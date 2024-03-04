using System;
using System.Runtime.InteropServices;
using UnityEngine;
using itk.simple;
using Unity.Collections;
using UnityVolumeRendering;
using Unity.Burst;
using Unity.Jobs;

[BurstCompile]
struct ProcessBufferJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> InputBuffer;
    public NativeArray<ushort> PixelBytes;
    public float MinValue;
    public float RangeValue;

    public void Execute(int index)
    {
        PixelBytes[index] = Mathf.FloatToHalf((InputBuffer[index] - MinValue) / RangeValue);
    }
}

public class DataVisualizationManager : MonoBehaviour
{
    #region Properties
    
    public static DataVisualizationManager Instance { get; private set; }
    public static VolumeRenderedObject VolumeRenderedObject { get; private set; }
    
    #endregion
    
    #region Private fields

    public Texture3D _mainTexture;
    
    #endregion
    
    #region Delegates/Events
    
    public delegate void VolumeHandler();
    public static event VolumeHandler OnVolumeCreate;

    public delegate void VolumeErrorHandler(Exception e);
    public static event VolumeErrorHandler OnVolumeErrorCreate;

    #endregion
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DataManager.OnFinishDataLoad += InitializeVolumeRendering;
    }

    private void InitializeVolumeRendering()
    {
        try
        {
            Create3DTextureFromImage();
            CreateObject();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnVolumeErrorCreate?.Invoke(e);
        }
    }
    
    public void Create3DTextureFromImage()
    {
        DateTime t1 = DateTime.Now;
        // Преобразуем тип данных изображения в float
        CastImageFilter castFilter = new CastImageFilter();
        castFilter.SetOutputPixelType(PixelIDValueEnum.sitkFloat32);
        Image floatImage = castFilter.Execute(DataManager.MainImage);

        // Получаем размеры изображения
        VectorUInt32 dims = floatImage.GetSize();
        int width = (int)dims[0];
        int height = (int)dims[1];
        int depth = (int)dims[2];
        int size = width * height * depth;

        float[] buffer = new float[size];
        Marshal.Copy(floatImage.GetBufferAsFloat(), buffer, 0, size);

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
        
        // Создаем 3D текстуру
        _mainTexture = new Texture3D(width, height, depth, TextureFormat.RHalf, false);
        _mainTexture.SetPixelData(pixelBytes, 0);
        _mainTexture.Apply(false, true);
    
        processBufferJob.InputBuffer.Dispose();  // Освободим память временного буфера
        pixelBytes.Dispose();
        
        DateTime t2 = DateTime.Now;
        Debug.Log((t2-t1).TotalSeconds);
    }

    private void CreateObject()
    {
        GameObject outerObject = new GameObject("VolumeRenderedObject");
        VolumeRenderedObject volObj = outerObject.AddComponent<VolumeRenderedObject>();

        GameObject meshContainer = GameObject.Instantiate((GameObject)Resources.Load("VolumeContainer"));
        volObj.volumeContainerObject = meshContainer;
        MeshRenderer meshRenderer = meshContainer.GetComponent<MeshRenderer>();

        CreateObjectInternal(meshContainer, meshRenderer,volObj, outerObject) ;
    }

   private void InitializeMeshContainer(UnityEngine.Transform meshContainer, UnityEngine.Transform outerObject)
   {
       // Устанавливаем начальные параметры для контейнера сетки
       meshContainer.localScale = Vector3.one;
       meshContainer.localPosition = Vector3.zero;
       meshContainer.parent = outerObject;
   } 

   private void SetMeshScale(UnityEngine.Transform meshContainer)
   {
       // Устанавливаем масштаб сетки на основе размеров изображения
       VectorUInt32 size = DataManager.MainImage.GetSize();
       VectorDouble spacing = DataManager.MainImage.GetSpacing();
       Vector3 realSize = new Vector3(
           Mathf.Abs((float)size[0] * (float)spacing[0]),
           Mathf.Abs((float)size[1] * (float)spacing[1]),
           Mathf.Abs((float)size[2] * (float)spacing[2])) * 0.001f;
       meshContainer.localScale = realSize;
   }

   private void SetMeshRotation(UnityEngine.Transform outerObject)
   {
       // Устанавливаем поворот сетки на основе направления изображения
       VectorDouble direction = DataManager.MainImage.GetDirection();
       Matrix4x4 directionMatrix = new Matrix4x4(
           new Vector4((float)direction[0], (float)direction[3], (float)direction[6], 0),
           new Vector4((float)direction[1], (float)direction[4], (float)direction[7], 0),
           new Vector4((float)direction[2], (float)direction[5], (float)direction[8], 0),
           new Vector4(0, 0, 0, 1)
       );
       Quaternion rotation = directionMatrix.rotation;

       outerObject.localRotation = rotation;
       outerObject.Rotate(Vector3.right, -90, Space.World);
   }

   private void MirrorMeshContainer(UnityEngine.Transform meshContainer)
   {
       // Применяем отражение к сетке, если это необходимо
       float xDot = Mathf.Abs(Vector3.Dot(meshContainer.right, Vector3.right));
       float yDot = Mathf.Abs(Vector3.Dot(meshContainer.up, Vector3.right));
       float zDot = Mathf.Abs(Vector3.Dot(meshContainer.forward, Vector3.right));

       Vector3 mirror = meshContainer.localScale;
       if (xDot > yDot && xDot > zDot) mirror.x = -mirror.x;
       else if (yDot > xDot && yDot > zDot) mirror.y = -mirror.y;
       else if (zDot > xDot && zDot > yDot) mirror.z = -mirror.z;

       meshContainer.localScale = mirror;
   }

   private void OffsetMeshContainer(UnityEngine.Transform meshContainer, UnityEngine.Transform outerObject)
   {
       // Смещаем контейнер сетки на основе его начальной точки
       UnityEngine.Transform originPoint = new GameObject().transform;
       originPoint.SetParent(meshContainer);
       originPoint.localPosition = -Vector3.one * 0.5f;

       Vector3 relative = outerObject.position - originPoint.position;
       meshContainer.position += relative;
   }

   private void SetMeshPosition(UnityEngine.Transform outerObject)
   {
       // Устанавливаем позицию контейнера сетки на основе начала изображения
       VectorDouble origin = DataManager.MainImage.GetOrigin();
       outerObject.position = new Vector3(-(float)origin[0], (float)origin[2], -(float)origin[1]) * 0.001f;
   }

   private void ApplyTexturing(VolumeRenderedObject volObj, MeshRenderer meshRenderer)
   {
       // Применяем текстурирование к объекту
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
       statisticsFilter.Execute(DataManager.MainImage);
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
       // Проверяем наличие метаданных о срезах
       if (DataManager.SlicesMetadata == null || DataManager.SlicesMetadata.Count == 0)
       {
           Debug.LogError("No slice metadata available.");
           return;
       }
       
       // Инициализация и установка параметров контейнера сетки
       UnityEngine.Transform meshContainerTransform = meshContainer.transform;
       UnityEngine.Transform outerObjectTransform = outerObject.transform;

       InitializeMeshContainer(meshContainerTransform, outerObjectTransform);
       SetMeshScale(meshContainerTransform);
       SetMeshRotation(outerObjectTransform);
       MirrorMeshContainer(meshContainerTransform);
       OffsetMeshContainer(meshContainerTransform, outerObjectTransform);
       SetMeshPosition(outerObjectTransform);
       ApplyTexturing(volObj, meshRenderer);

       VolumeRenderedObject = volObj;
       
       OnVolumeCreate?.Invoke();
       
       Debug.Log(DataManager.Order.ToString());
   }
}