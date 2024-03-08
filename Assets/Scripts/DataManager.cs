using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FellowOakDicom;
using openDicom.DataStructure.DataSet;
using UnityEngine;
using itk.simple;

public enum ScanOrder
{
    Unknown = 0,
    AxialIS = 1, AxialSI = 2,
    SagittalLR = 3, SagittalRL = 4,
    CoronalPA = 5, CoronalAP = 6, 
}

public class DataManager : MonoBehaviour
{
    #region Properties
    
    public static DataManager Instance { get; private set; }
    public static ScanOrder Order { get; private set; }
    public static Image MainImage => Instance._mainImage;
    public static DicomDataset Dataset => Instance._fullMetadataDataset;
    public static List<DicomSliceMetadata> SlicesMetadata => Instance._slicesMetadata;
    public static List<SeriesInfo> FoundSeriesInfos => Instance._seriesInfos; 
    
    #endregion
    
    #region Private fields
    
    private List<SeriesInfo> _seriesInfos = new List<SeriesInfo>();
    private string _dicomDir;
    private Image _mainImage;
    private List<DicomSliceMetadata> _slicesMetadata;
    private DicomDataset _fullMetadataDataset;
    
    #endregion

    #region Delegates/Events
    
    public delegate void DataHandler();
    public static event DataHandler OnSeriesFound;
    public static event DataHandler OnSeriesNotFound; 
    public static event DataHandler OnStartDataLoad; 
    public static event DataHandler OnFinishDataLoad; 
    
    public delegate void DataErrorHandler(Exception e);
    public static event DataErrorHandler OnSeriesErrorLoad; 
    public static event DataErrorHandler OnDataErrorLoad;
    
    #endregion
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        OnSeriesFound += () => {
            foreach (var i in _seriesInfos)
            {
                Debug.Log(i.patientName + " " + i.seriesUID);
            }

            LoadDicomSeries(_seriesInfos[0].seriesUID);
        };
        OnSeriesNotFound += () => { Debug.Log("Series not found"); };
        OnStartDataLoad += () => { Debug.Log("Start load"); };
        OnFinishDataLoad += () =>
        {
            Debug.Log("Finish load"); 
        };
        OnSeriesErrorLoad += (e) => { Debug.Log("Series exteprion" + e); };
        OnDataErrorLoad += (e) => { Debug.Log("Data exteprion" + e); };
    }

    private void Start()
    {
        FindSeries();
    }

    public static void FindSeries()
    {
        Crosstales.FB.FileBrowser.Instance.OpenFoldersAsync(Instance.FindDicomSeriesByPath,
            "Выберите папку с расположением dicom", "", false);
    }

    private async void FindDicomSeriesByPath(string[] itemWithStreams)
    {
        if (itemWithStreams == null || itemWithStreams.Length == 0)
        {
            Debug.LogWarning("Не удается обнаружить директорию");
            return;
        }
        
        _seriesInfos.Clear();

        try
        {
            await FindDicomSeries(itemWithStreams[0]);
        }
        catch (Exception e)
        {
            OnSeriesErrorLoad?.Invoke(e);
        }

        if (_seriesInfos.Count > 0) 
            OnSeriesFound?.Invoke();
        else
            OnSeriesNotFound?.Invoke();
    }

    async Task FindDicomSeries(string dir)
    {
        foreach (var subDir in Directory.GetDirectories(dir))
        {
            await FindDicomSeries(subDir);
        }

        var seriesIDs = ImageSeriesReader.GetGDCMSeriesIDs(dir);
        foreach (var seriesID in seriesIDs)
        {
            var dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(dir, seriesID);
            if (dicomNames.Count > 0)
            {
                var dicomFile = DicomFile.Open(dicomNames[0]);

                var seriesInfo = new SeriesInfo
                {
                    seriesUID = seriesID,
                    patientName = dicomFile.Dataset.GetString(DicomTag.PatientName),
                    seriesDescription = dicomFile.Dataset.GetString(DicomTag.SeriesDescription),
                    dateStudy = dicomFile.Dataset.GetString(DicomTag.StudyDate),
                    dimX = dicomFile.Dataset.GetSingleValue<int>(DicomTag.Columns),
                    dimY = dicomFile.Dataset.GetSingleValue<int>(DicomTag.Rows),
                    dimZ = dicomNames.Count, // Предполагается, что количество файлов соответствует количеству срезов.
                    missingFiles = false,
                    dirPath = dir
                };

                if (dicomNames.Count > 1)
                {
                    int totalGaps = 0;

                    for (int i = 1; i < dicomNames.Count; i++)
                    {
                        int instanceNumber1 = DicomFile.Open(dicomNames[i - 1]).Dataset.GetSingleValue<int>(DicomTag.InstanceNumber);
                        int instanceNumber2 = DicomFile.Open(dicomNames[i]).Dataset.GetSingleValue<int>(DicomTag.InstanceNumber);

                        totalGaps += instanceNumber2 - instanceNumber1 - 1;
                    }

                    if (totalGaps > 0)
                    {
                        seriesInfo.missingFiles = true;
                        Debug.LogWarning($"Warning: {totalGaps} files missing in series {seriesID}. Please verify the loaded files.");
                    }
                }
                
                _seriesInfos.Add(seriesInfo);
            }
        }
    }
    
    public void LoadDicomSeries(string seriesID)
    {
        var seriesInfo = _seriesInfos.Find(info => info.seriesUID == seriesID);
        if (seriesInfo != null)
        {
            try
            {
                OnStartDataLoad?.Invoke();
                var dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(seriesInfo.dirPath, seriesID);
                var reader = new ImageSeriesReader();
                reader.SetFileNames(dicomNames);
                _mainImage = reader.Execute();
                
                _fullMetadataDataset = null;
                _slicesMetadata = new List<DicomSliceMetadata>();
                
                foreach (var dicomName in dicomNames)
                {
                    var dicomFile = DicomFile.Open(dicomName);

                    if (_fullMetadataDataset == null) _fullMetadataDataset = dicomFile.Dataset;
                    
                    // Extract DICOM tags to our metadata class
                    var dicomMetadata = new DicomSliceMetadata();

                    if (dicomFile.Dataset.Contains(DicomTag.ImagePositionPatient))
                        dicomMetadata.ImagePositionPatient = ParseDicomPosition(dicomFile.Dataset.GetString(DicomTag.ImagePositionPatient));
                    if (dicomFile.Dataset.Contains(DicomTag.SliceLocation))
                        dicomMetadata.SliceLocation = dicomFile.Dataset.GetSingleValue<float>(DicomTag.SliceLocation);
                    if (dicomFile.Dataset.Contains(DicomTag.InstanceNumber))
                        dicomMetadata.InstanceNumber = dicomFile.Dataset.GetSingleValue<int>(DicomTag.InstanceNumber);
                    if (dicomFile.Dataset.Contains(DicomTag.SOPInstanceUID))
                        dicomMetadata.SOPInstanceUID = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID);
                    if (dicomFile.Dataset.Contains(DicomTag.AcquisitionTime))
                        dicomMetadata.AcquisitionTime = dicomFile.Dataset.GetString(DicomTag.AcquisitionTime);

                    _slicesMetadata.Add(dicomMetadata);
                }

                /*
                CalculateOrder1();

                //var position = dataset.GetValues<double>(DicomTag.ImagePositionPatient);
                //var spacing = dataset.GetValues<double>(DicomTag.PixelSpacing);
                //var sliceThickness = dataset.GetValue<double>(DicomTag.SliceThickness, 0);

                Debug.Log("Image Orientation (Patient): " + string.Join(", ", orientation));
                Debug.Log("zOrientation: " + string.Join(", ", zOrientation));
                Debug.Log("Image Position (Patient): " + string.Join(", ", position));
                Debug.Log("Pixel Spacing: " + string.Join(", ", spacing));
                Debug.Log("Slice Thickness: " + string.Join(", ", sliceThickness));

                Matrix4x4 ijkToRAS = Matrix4x4.identity;

                ijkToRAS.SetRow(0, new Vector4(xOrientation.x * (float)spacing[0], xOrientation.y * (float)spacing[1], xOrientation.z * (float)sliceThickness, (float)position[0]));
                ijkToRAS.SetRow(1, new Vector4(yOrientation.x * (float)spacing[0], yOrientation.y * (float)spacing[1], yOrientation.z * (float)sliceThickness, (float)position[1]));
                ijkToRAS.SetRow(2, new Vector4(zOrientation.x * (float)spacing[0], zOrientation.y * (float)spacing[1], zOrientation.z * (float)sliceThickness, (float)position[2]));
                */

                var dataset = DicomFile.Open(dicomNames[0]).Dataset;
                
                var orientation = dataset.GetValues<double>(DicomTag.ImageOrientationPatient);
                
                Vector3 xOrientation = new Vector3((float)orientation[0], (float)orientation[1], (float)orientation[2]);
                Vector3 yOrientation = new Vector3((float)orientation[3], (float)orientation[4], (float)orientation[5]);
                Vector3 zOrientation = Vector3.Cross(xOrientation, yOrientation);
                
                Matrix4x4 ijkToRAS = Matrix4x4.identity;
                ijkToRAS.SetColumn(0, new Vector4(xOrientation.x, xOrientation.y, xOrientation.z, 0));
                ijkToRAS.SetColumn(1, new Vector4(yOrientation.x, yOrientation.y, yOrientation.z, 0));
                ijkToRAS.SetColumn(2, new Vector4(zOrientation.x, zOrientation.y, zOrientation.z, 0));

                var scanOrder = ComputeScanOrderFromIJKToRAS(ijkToRAS);
                Debug.Log("Scan Order: " + scanOrder);

                OnFinishDataLoad?.Invoke();
            }
            catch (Exception e)
            {
                OnDataErrorLoad?.Invoke(e);
            }
        }
    }
    
    private void CalculateOrder1()
    {
        var image = _mainImage; // _mainImage является объектом itk.simple.Image

        // Получаем ориентацию изображения
        var direction = image.GetDirection();
        // Получаем позицию изображения
        var origin = image.GetOrigin();
        // Получаем масштабирование пикселей
        var spacing = image.GetSpacing();

        // Создаем матрицу преобразования с учетом этих данных
        Matrix4x4 ijkToRAS = Matrix4x4.identity;

        /*
        // Установка ориентации и масштабирования
        ijkToRAS.SetColumn(0, new Vector4((float)direction[0] * (float)spacing[0], (float)direction[3] * (float)spacing[1], (float)direction[6] * (float)spacing[2], 0));
        ijkToRAS.SetColumn(1, new Vector4((float)direction[1] * (float)spacing[0], (float)direction[4] * (float)spacing[1], (float)direction[7] * (float)spacing[2], 0));
        ijkToRAS.SetColumn(2, new Vector4((float)direction[2] * (float)spacing[0], (float)direction[5] * (float)spacing[1], (float)direction[8] * (float)spacing[2], 0));
        // Установка позиции
        ijkToRAS.SetColumn(3, new Vector4((float)origin[0], (float)origin[1], (float)origin[2], 1));
        */
        
        // Установка ориентации и масштабирования
        ijkToRAS.SetRow(0, new Vector4((float)direction[0] * (float)spacing[0], (float)direction[1] * (float)spacing[1], (float)direction[2] * (float)spacing[2], 0));
        ijkToRAS.SetRow(1, new Vector4((float)direction[3] * (float)spacing[0], (float)direction[4] * (float)spacing[1], (float)direction[5] * (float)spacing[2], 0));
        ijkToRAS.SetRow(2, new Vector4((float)direction[6] * (float)spacing[0], (float)direction[7] * (float)spacing[1], (float)direction[8] * (float)spacing[2], 0));

        // Установка позиции
        ijkToRAS.SetRow(3, new Vector4((float)origin[0], (float)origin[1], (float)origin[2], 1));

        // Вывод полученных данных для демонстрации
        Debug.Log("Image Orientation (Patient): " + string.Join(", ", direction));
        Debug.Log("Image Position (Patient): " + string.Join(", ", origin));
        Debug.Log("Pixel Spacing: " + string.Join(", ", spacing));
        
        // Вычисление порядка сканирования из матрицы IJK to RAS
        var scanOrder = ComputeScanOrderFromIJKToRAS(ijkToRAS);
        Debug.Log("Scan Order: " + scanOrder);
    }
    
    public string ComputeScanOrderFromIJKToRAS(Matrix4x4 ijkToRAS)
    {
        Vector3 dir = new Vector3(0, 0, 1); // Аналог dir[4]={0,0,1,0};
        var kvec = ijkToRAS.MultiplyPoint(dir);

        int maxComp = 0;
        float max = Mathf.Abs(kvec[0]);

        for (int i = 1; i < 3; i++)
        {
            if (Mathf.Abs(kvec[i]) > max)
            {
                max = Mathf.Abs(kvec[i]);
                maxComp = i;
            }
        }
        
        Debug.Log($"maxComp: {maxComp.ToString()}");  
        Debug.Log($"max after: {max.ToString()}");  

        switch (maxComp)
        {
            case 0:
                return kvec[maxComp] > 0 ? ScanOrder.SagittalRL.ToString() : ScanOrder.SagittalLR.ToString();
            case 1:
                return kvec[maxComp] > 0 ? ScanOrder.CoronalAP.ToString() : ScanOrder.CoronalPA.ToString();
            case 2:
                return kvec[maxComp] > 0 ? ScanOrder.AxialIS.ToString() : ScanOrder.AxialSI.ToString();
            default:
                Debug.LogError("Max component not in valid range 0,1,2");
                return "";
        }
    }
    
    private void CalculateOrder()
    {
        Vector3 corner = GetOriginCornerDirection();
        
        // 0.0005f epsilon to eliminate floating point error
        corner = new Vector3(
            (Mathf.Abs(corner.x) < 0.0005f) ? 0 : corner.x,
            (Mathf.Abs(corner.y) < 0.0005f) ? 0 : corner.y,
            (Mathf.Abs(corner.z) < 0.0005f) ? 0 : corner.z);

        if (corner.x != 0)
            Order = (corner.x < 0) ? ScanOrder.SagittalLR : ScanOrder.SagittalRL; // Invert X
        else if (corner.y != 0)
            Order = (corner.y < 0) ? ScanOrder.AxialSI : ScanOrder.AxialIS;
        else if (corner.z != 0)
            Order = (corner.z < 0) ? ScanOrder.CoronalPA : ScanOrder.CoronalAP;
        else {
            Debug.LogError("Can't calculate order. Size is zero on all axies");
            Order = ScanOrder.AxialIS;
        }

        if (corner.x != 0 && corner.y != 0 || corner.x != 0 && corner.z != 0 || corner.y != 0 && corner.z != 0)
        {
            Debug.LogError("Orientation is not parallel axies");
        }
    }
    
    private Vector3 GetOriginCornerDirection()
    {
        // Преобразуем строку положения в вектор
        Vector3 firstSlicePosition = Quaternion.Euler(-90, 0, 0) * SlicesMetadata[0].ImagePositionPatient;
        firstSlicePosition.z = -firstSlicePosition.z;
        Vector3 lastSlicePosition = Quaternion.Euler(-90, 0, 0) * SlicesMetadata[SlicesMetadata.Count - 1].ImagePositionPatient;
        lastSlicePosition.z = -lastSlicePosition.z;

        return lastSlicePosition - firstSlicePosition;
    }
    
    // Функция для преобразования строки DICOM в вектор
    private Vector3 ParseDicomPosition(string dicomPositionString)
    {
        string[] parts = dicomPositionString.Split('\\');
        if (parts.Length != 3)
        {
            Debug.LogError("Invalid DICOM position string: " + dicomPositionString);
            return Vector3.zero;
        }

        float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[2], CultureInfo.InvariantCulture);

        return new Vector3(x, y, z);
    }
    
    // Класс для хранения информации о серии
    public class SeriesInfo
    {
        public string seriesUID;
        public string patientName;
        public string seriesDescription;
        public string dateStudy;
        public int dimX, dimY, dimZ;
        public bool missingFiles;
        public string dirPath;
    }
    
    // Класс для хранения метаданных среза
    public class DicomSliceMetadata
    {
        public Vector3 ImagePositionPatient { get; set; }
        public float SliceLocation { get; set; }
        public int InstanceNumber { get; set; }
        public string SOPInstanceUID { get; set; }
        public string AcquisitionTime { get; set; }
    }
}
