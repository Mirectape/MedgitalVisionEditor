using itk.simple;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityVolumeRendering;

public class DicomVolumeBuilder : MonoBehaviour
{
    public DicomVolumeBuilder Instance { get; private set; }
    public static VolumeRenderedObject VolumeRenderedObject { get; private set; }
    public static Vector3 InitialBuildingPoint { get; private set; }
    private Texture3D _mainTexture;

    public static Action<UnityEngine.Transform> onVolumeBuilt;

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
        DicomDataHandler.OnDataLoaded += InitializeVolumeRendering;
    }

    private void InitializeVolumeRendering(object sender, EventArgs e)
    {
        try
        {
            Create3DTextureFromImage();
            CreateObject();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
        }
    }

    public void Create3DTextureFromImage()
    {
        DateTime t1 = DateTime.Now;

        // Image to floatImage transition
        CastImageFilter castFilter = new CastImageFilter();
        castFilter.SetOutputPixelType(PixelIDValueEnum.sitkFloat32);
        Image floatImage = castFilter.Execute(DicomDataHandler.MainImage);

        // Size of image in volume
        VectorUInt32 dims = floatImage.GetSize();
        int width = (int)dims[0];
        int height = (int)dims[1];
        int depth = (int)dims[2];
        int size = width * height * depth;

        float[] buffer = new float[size];
        Marshal.Copy(floatImage.GetBufferAsFloat(), buffer, 0, size); //???

        NativeArray<ushort> pixelBytes = new NativeArray<ushort>(size, Allocator.Persistent); //DOTS!!!

        StatisticsImageFilter statisticsFilter = new StatisticsImageFilter();
        statisticsFilter.Execute(floatImage);
        double minValue = statisticsFilter.GetMinimum();
        double rangeValue = statisticsFilter.GetMaximum() - minValue;

        ProcessBufferJob processBufferJob = new ProcessBufferJob
        {
            InputBuffer = new NativeArray<float>(buffer, Allocator.TempJob),
            PixelBytes = pixelBytes,
            MinValue = (float)minValue,
            RangeValue = (float)rangeValue
        };

        JobHandle handle = processBufferJob.Schedule(size, 256);
        handle.Complete();

        // Ñîçäàåì 3D òåêñòóðó
        _mainTexture = new Texture3D(width, height, depth, TextureFormat.RHalf, false);
        _mainTexture.SetPixelData(pixelBytes, 0);
        _mainTexture.Apply(false, true);

        processBufferJob.InputBuffer.Dispose();  // Îñâîáîäèì ïàìÿòü âðåìåííîãî áóôåðà
        pixelBytes.Dispose();

        DateTime t2 = DateTime.Now;
        Debug.Log("Uploaded in: " + (t2 - t1).TotalSeconds + "seconds");
    }

    private void CreateObject()
    {
        GameObject outerObject = new GameObject("VolumeRenderedObject");
        VolumeRenderedObject volObj = outerObject.AddComponent<VolumeRenderedObject>();

        GameObject meshContainer = GameObject.Instantiate((GameObject)Resources.Load("VolumeContainer"));
        volObj.volumeContainerObject = meshContainer;
        MeshRenderer meshRenderer = meshContainer.GetComponent<MeshRenderer>();

        CreateObjectInternal(meshContainer, meshRenderer, volObj, outerObject);
    }

    private void ApplyTexturing(VolumeRenderedObject volObj, MeshRenderer meshRenderer)
    {
        // Ïðèìåíÿåì òåêñòóðèðîâàíèå ê îáúåêòó
        meshRenderer.sharedMaterial = new Material(meshRenderer.sharedMaterial);
        volObj.meshRenderer = meshRenderer;

        const int noiseDimX = 512;
        const int noiseDimY = 512;
        Texture2D noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(noiseDimX, noiseDimY);

        UnityVolumeRendering.TransferFunction tf = TransferFunctionDatabase.CreateTransferFunction();
        Texture2D tfTexture = tf.GetTexture();
        volObj.transferFunction = tf;

        TransferFunction2D tf2D = TransferFunctionDatabase.CreateTransferFunction2D();
        volObj.transferFunction2D = tf2D;

        StatisticsImageFilter statisticsFilter = new StatisticsImageFilter();
        statisticsFilter.Execute(DicomDataHandler.MainImage);
        double minValue = statisticsFilter.GetMinimum();
        double maxValue = statisticsFilter.GetMaximum();

        meshRenderer.sharedMaterial.SetTexture("_DataTex", _mainTexture);
        meshRenderer.sharedMaterial.SetTexture("_GradientTex", null);
        meshRenderer.sharedMaterial.SetTexture("_NoiseTex", noiseTexture);
        meshRenderer.sharedMaterial.SetTexture("_TFTex", tfTexture);
        meshRenderer.sharedMaterial.SetFloat("_MinDensity", (float)minValue);
        meshRenderer.sharedMaterial.SetFloat("_MaxDensity", (float)maxValue);

        meshRenderer.sharedMaterial.EnableKeyword("MODE_DVR");
        meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
        meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");
    }

    private void CreateObjectInternal(GameObject meshContainer, MeshRenderer meshRenderer, VolumeRenderedObject volObj, GameObject outerObject)
    {
        // Ïðîâåðÿåì íàëè÷èå ìåòàäàííûõ î ñðåçàõ
        if (DicomDataHandler.SelectedSlicesMetadata == null || DicomDataHandler.SelectedSlicesMetadata.Count == 0)
        {
            Debug.LogError("No slice metadata available.");
            return;
        }

        // Èíèöèàëèçàöèÿ è óñòàíîâêà ïàðàìåòðîâ êîíòåéíåðà ñåòêè
        UnityEngine.Transform meshContainerTransform = meshContainer.transform;
        UnityEngine.Transform outerObjectTransform = outerObject.transform;

        ApplyTexturing(volObj, meshRenderer);
        VolumeRenderedObject = volObj;

        var initialBuildingPoint = new GameObject("Initial Building Point").transform;
        initialBuildingPoint.transform.localPosition = new Vector3(-0.5f, -0.5f, -0.5f);
        initialBuildingPoint.SetParent(outerObjectTransform); //Apply all the matrices to him along with image Volume 

        CreateVolumeDataSet();

        onVolumeBuilt?.Invoke(outerObjectTransform);
        volObj.transferFunction = TransferFunctionSetter.GetRecalibratedTransferFunction(volObj);

        initialBuildingPoint.SetParent(null); //Remove him from there to get Image origin at (0,0,0)
        InitialBuildingPoint = new Vector3(initialBuildingPoint.localPosition.x,
                                            initialBuildingPoint.localPosition.y,
                                            initialBuildingPoint.localPosition.z);

        VolumeRenderedObject.volumeContainerObject.transform.SetParent(VolumeRenderedObject.transform);
        ResetTransform(VolumeRenderedObject.volumeContainerObject.transform);
    }

    private void CreateVolumeDataSet()
    {
        // Get all files in DICOM directory
        List<string> filePaths = Directory.GetFiles(DicomDataHandler.SeriesInfos[0].dirPath).ToList<string>();
        // Create importer
        IImageSequenceImporter importer = ImporterFactory.CreateImageSequenceImporter(ImageSequenceFormat.DICOM);
        // Load list of DICOM series (normally just one series)
        IEnumerable<IImageSequenceSeries> seriesList = importer.LoadSeries(filePaths);
        // There will usually just be one series
        foreach (IImageSequenceSeries series in seriesList)
        {
            // Import single DICOm series
            VolumeDataset dataset = importer.ImportSeries(series);
            VolumeRenderedObject.dataset = dataset;
        }
    }

    private void ResetTransform(UnityEngine.Transform objectToReset)
    {
        // Reset position to origin
        objectToReset.localPosition = Vector3.zero;

        // Reset rotation to identity (no rotation)
        objectToReset.localRotation = Quaternion.identity;

        // Reset scale to default (1, 1, 1)
        objectToReset.localScale = Vector3.one;
    }
}
