using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using UnityEngine;
using itk.simple;
using System.Text;
using System.Linq;
using FellowOakDicom.Imaging.Reconstruction;
using UnityEditor.SceneManagement;
using openDicom.File;

public class DicomDataHandler : MonoBehaviour
{
    public static DicomDataHandler Instance { get; private set; }

    public static List<DicomSeries> DicomSeriesList { get; private set; } = new List<DicomSeries>();

    public static itk.simple.Image MainImage => Instance._mainImage;
    public static List<SeriesInfo> SeriesInfos => Instance._seriesInfos;
    public static List<SelectedDicomSliceMetadata> SelectedSlicesMetadata => Instance._selectedSlicesMetadata;
    public static Matrix4x4 SlicesOrientationMatrix => Instance._slicesOrientationMatrix;
    public static DicomSliceOrder DicomSliceOrder => Instance._dicomSliceOrder;

    #region private fields
    private itk.simple.Image _mainImage;
    private List<SeriesInfo> _seriesInfos = new List<SeriesInfo>();
    private List<SelectedDicomSliceMetadata> _selectedSlicesMetadata;
    private Matrix4x4 _slicesOrientationMatrix;
    private DicomSliceOrder _dicomSliceOrder;
    #endregion

    public static EventHandler OnSeriesFound;
    public static EventHandler OnDataLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _slicesOrientationMatrix = Matrix4x4.identity;

        OnSeriesFound += (sender, args) =>
        {
            foreach (var i in _seriesInfos)
            {
                Debug.Log("The patient is: " + i.patientName + " " + i.seriesUID);
            }

            LoadDicomSeries(_seriesInfos[0].seriesUID); //the Dicom first series found
        };
        OnDataLoaded += (sender, args) =>
        {
            Debug.Log("Loading is finished!");
            DicomSeriesList.Add(new DicomSeries(_mainImage, _seriesInfos, _selectedSlicesMetadata, _slicesOrientationMatrix));
        };
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

        _seriesInfos.Clear(); //clearing the previous patient's data before uploading a new one

        try
        {
            await ExtractDicomSeriesInfo(itemWithStreams[0]); //the first folder 
        }
        catch (Exception e)
        {
            Debug.Log("Series exception: " + e);
        }

        if (_seriesInfos.Count > 0)
        {
            OnSeriesFound?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Debug.Log("Series are not found");
        }
    }

    private async Task ExtractDicomSeriesInfo(string dir)
    {
        foreach (var subDir in Directory.GetDirectories(dir))
        {
            await ExtractDicomSeriesInfo(subDir); //for opening sub-folders
        }

        var seriesIDs = ImageSeriesReader.GetGDCMSeriesIDs(dir);
        foreach (var seriesID in seriesIDs)
        {
            var dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(dir, seriesID);
            if (dicomNames.Count > 0)
            {
                var dicomFile = FellowOakDicom.DicomFile.Open(dicomNames[0]); //[0] because information on seriesID is the same for all files in a chosen dir

                var seriesInfo = new SeriesInfo // what series we have 
                {
                    seriesUID = seriesID,
                    patientName = dicomFile.Dataset.GetString(DicomTag.PatientName),
                    seriesDescription = dicomFile.Dataset.GetString(DicomTag.SeriesDescription),
                    dateStudy = dicomFile.Dataset.GetString(DicomTag.StudyDate),
                    dimX = dicomFile.Dataset.GetSingleValue<int>(DicomTag.Columns),
                    dimY = dicomFile.Dataset.GetSingleValue<int>(DicomTag.Rows),
                    dimZ = dicomNames.Count, // number of slices = number of files
                    missingFiles = false,
                    dirPath = dir
                };

                if (dicomNames.Count > 1)
                {
                    int totalGaps = 0;

                    for (int i = 1; i < dicomNames.Count; i++)
                    {
                        int instanceNumber1 = FellowOakDicom.DicomFile.Open(dicomNames[i - 1]).Dataset.GetSingleValue<int>(DicomTag.InstanceNumber);
                        int instanceNumber2 = FellowOakDicom.DicomFile.Open(dicomNames[i]).Dataset.GetSingleValue<int>(DicomTag.InstanceNumber);

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

    private void LoadDicomSeries(string seriesID)
    {
        var seriesInfo = _seriesInfos.Find(info => info.seriesUID == seriesID);
        if (seriesInfo != null)
        {
            try
            {
                Debug.Log("Start loading...");
                var reader = new ImageSeriesReader();
                var dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(seriesInfo.dirPath, seriesID);
                reader.SetFileNames(dicomNames);
                _mainImage = reader.Execute();

                _selectedSlicesMetadata = new List<SelectedDicomSliceMetadata>();

                foreach (var dicomName in dicomNames)
                {
                    var dicomFile = FellowOakDicom.DicomFile.Open(dicomName);

                    // Extract DICOM tags to our metadata class
                    var dicomMetadata = new SelectedDicomSliceMetadata();

                    if (dicomFile.Dataset.Contains(DicomTag.ImagePositionPatient))
                    {
                        dicomMetadata.ImagePositionPatient = ParseDicomPosition(dicomFile.Dataset.GetString(DicomTag.ImagePositionPatient));
                    }
                    if (dicomFile.Dataset.Contains(DicomTag.InstanceNumber))
                    {
                        dicomMetadata.InstanceNumber = dicomFile.Dataset.GetSingleValue<int>(DicomTag.InstanceNumber);
                    }
                    if (dicomFile.Dataset.Contains(DicomTag.SOPInstanceUID))
                    {
                        dicomMetadata.SOPInstanceUID = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID);
                    }
                    if (dicomFile.Dataset.Contains(DicomTag.ImageOrientationPatient)) 
                    {
                        dicomMetadata.ImageOrientationPatient = dicomFile.Dataset.GetValues<double>(DicomTag.ImageOrientationPatient);
                    }
                    _selectedSlicesMetadata.Add(dicomMetadata);
                }

                DefineMainImageOrientation();
                PrintSlicesOrientationMatrix();
                OnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Debug.Log("Data exception: " + e);
            }
        }
    }

    private void DefineMainImageOrientation()
    {
        var firstSlice = _selectedSlicesMetadata[0].ImagePositionPatient;
        var secondSlice = _selectedSlicesMetadata[1].ImagePositionPatient;
        var sliceDirectionVector = secondSlice - firstSlice; //(R,A,S)

        _dicomSliceOrder = GetDicomSliceOrder(sliceDirectionVector);

        _slicesOrientationMatrix.SetRow(0, new Vector4((float)_selectedSlicesMetadata[0].ImageOrientationPatient[0],
            (float)_selectedSlicesMetadata[0].ImageOrientationPatient[1], (float)_selectedSlicesMetadata[0].ImageOrientationPatient[2], 0));
        _slicesOrientationMatrix.SetRow(1, new Vector4((float)_selectedSlicesMetadata[0].ImageOrientationPatient[3],
            (float)_selectedSlicesMetadata[0].ImageOrientationPatient[4], (float)_selectedSlicesMetadata[0].ImageOrientationPatient[5], 0));
        _slicesOrientationMatrix.SetRow(2, new Vector4((float)sliceDirectionVector[0],
            sliceDirectionVector[1], sliceDirectionVector[2], 0));
    }

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

    private DicomSliceOrder GetDicomSliceOrder(Vector3 vector)
    {
        int nullValueCount = 0;

        if (vector.x != 0) 
        { 
            nullValueCount++;
            if (vector.y == 0 && vector.z == 0)
            {
                if (vector.x > 0) { return DicomSliceOrder.LR; }
                if (vector.x < 0) { return DicomSliceOrder.RL; }
            }
        }
        if (vector.y != 0)
        {
            nullValueCount++;
            if (vector.x == 0 && vector.z == 0)
            {
                if (vector.y > 0) { return DicomSliceOrder.AP; }
                if (vector.y < 0) { return DicomSliceOrder.PA; }
            }
        }
        if (vector.z != 0)
        {
            nullValueCount++;
            if(vector.x == 0 && vector.y == 0)
            {
                if (vector.z > 0) { return DicomSliceOrder.IS; }
                if (vector.z < 0) { return DicomSliceOrder.SI; }
            }
        }

        if(nullValueCount == 0) { return DicomSliceOrder.UnknownOrder; }

        return DicomSliceOrder.RotatedOrder;
    }

    private void PrintSlicesOrientationMatrix()
    {
        Debug.Log("The dicom slice order is: " + _dicomSliceOrder);
        Debug.Log("The orientation Matrix is: ");
        Debug.Log(String.Join(" ", _slicesOrientationMatrix));
    }
}

