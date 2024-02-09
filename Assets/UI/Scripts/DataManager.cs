using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FellowOakDicom;
using Dicom;
using openDicom.DataStructure.DataSet;
using UnityEngine;
using itk.simple;

public enum ScanOrder
{
    AxialIS = 0, AxialSI = 1,
    SagittalLR = 2, SagittalRL = 3,
    CoronalPA = 4, CoronalAP = 5
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
        //FindSeries();
    }

    public static void FindSeries()
    {
        //Crosstales.FB.FileBrowser.Instance.OpenFoldersAsync(Instance.FindDicomSeriesByPath,
        //    "Выберите папку с расположением dicom", "", false);
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

                CalculateOrder();
                
                OnFinishDataLoad?.Invoke();
            }
            catch (Exception e)
            {
                OnDataErrorLoad?.Invoke(e);
            }
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
