using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using UnityEngine;
using itk.simple;
using UnityEngine.UI;

public class DicomDataHandler : MonoBehaviour
{
    public static DicomDataHandler Instance {  get; private set; }

    public static itk.simple.Image MainImage => Instance._mainImage;
    public static List<SeriesInfo> SeriesInfos => Instance._seriesInfos;
    public static DicomDataset FullMetadataset => Instance._fullMetadataDataset;
    public static List<SelectedDicomSliceMetadata> SelectedSlicesMetadata => Instance._selectedSlicesMetadata;

    #region private fields
    private itk.simple.Image _mainImage;
    private List<SeriesInfo> _seriesInfos = new List<SeriesInfo>();
    private DicomDataset _fullMetadataDataset;
    private List<SelectedDicomSliceMetadata> _selectedSlicesMetadata;
    #endregion

    public static EventHandler OnSeriesFound;
    public static EventHandler OnDataLoaded;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        OnSeriesFound += (sender, args) => 
        {
            foreach (var i in _seriesInfos)
            {
                Debug.Log(i.patientName + " " + i.seriesUID);
            }

            LoadDicomSeries(_seriesInfos[0].seriesUID); //the Dicom first series found
        };
        OnDataLoaded += (sender, args) =>
        {
            Debug.Log("Loading is finished!");
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
            await FindDicomSeries(itemWithStreams[0]); //the first folder 
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

    private async Task FindDicomSeries(string dir)
    {
        foreach (var subDir in Directory.GetDirectories(dir))
        {
            await FindDicomSeries(subDir); //for opening sub-folders
        }

        var seriesIDs = ImageSeriesReader.GetGDCMSeriesIDs(dir);
        foreach (var seriesID in seriesIDs)
        {
            var dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(dir, seriesID);
            if (dicomNames.Count > 0)
            {
                var dicomFile = DicomFile.Open(dicomNames[0]); //[0] because information on seriesID is the same for all files in a chosen dir

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

                _fullMetadataDataset = null;
                _selectedSlicesMetadata = new List<SelectedDicomSliceMetadata>();

                foreach (var dicomName in dicomNames)
                {
                    var dicomFile = DicomFile.Open(dicomName);

                    if (_fullMetadataDataset == null)
                    {
                        _fullMetadataDataset = dicomFile.Dataset;
                    }

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

                    _selectedSlicesMetadata.Add(dicomMetadata);
                }

                OnDataLoaded?.Invoke(this, new EventArgs());
            }
            catch (Exception e)
            {
                Debug.Log("Data exception: " + e);
            }
        }
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
}

