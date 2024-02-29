using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.FB;
using UnityVolumeRendering;
using System.Linq;
using System;

public class DicomRuntimeUpload : MonoBehaviour
{
    private string path;

    private void Awake()
    {
        OnOpenDICOMDatasetResultAsync();
    }

    public void OpenSingleFolder()
    {
        path = FileBrowser.Instance.OpenSingleFolder();
        Debug.Log("Selected folder: " + path);
    }

    private async void OnOpenDICOMDatasetResultAsync()
    {
        // We'll only allow one dataset at a time in the runtime GUI (for simplicity)
        DespawnAllDatasets();
        path = FileBrowser.Instance.OpenSingleFolder();
        bool recursive = true;

        // Read all files
        IEnumerable<string> fileCandidates = Directory.EnumerateFiles(path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));

        // Import the dataset
        IImageSequenceImporter importer = ImporterFactory.CreateImageSequenceImporter(ImageSequenceFormat.DICOM);
        IEnumerable<IImageSequenceSeries> seriesList = await importer.LoadSeriesAsync(fileCandidates);
        float numVolumesCreated = 0;
        foreach (IImageSequenceSeries series in seriesList)
        {
            VolumeDataset dataset = await importer.ImportSeriesAsync(series);
            // Spawn the object
            if (dataset != null)
            {
                VolumeRenderedObject obj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                obj.transform.position = new Vector3(numVolumesCreated, 0, 0);
                numVolumesCreated++;
            }
        }
    }

    private void DespawnAllDatasets()
    {
        VolumeRenderedObject[] volobjs = GameObject.FindObjectsOfType<VolumeRenderedObject>();
        foreach (VolumeRenderedObject volobj in volobjs)
        {
            GameObject.Destroy(volobj.gameObject);
        }
    }
}

