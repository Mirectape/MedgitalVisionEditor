using System.Collections;
using UnityEngine;
using System;

using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Unity.Collections;
using FMSolution.FMZip;

namespace FMSolution.FMETP
{
    [RequireComponent(typeof(Camera)), AddComponentMenu("FMETP/Mapper/FMPCStreamEncoder")]
    public class FMPCStreamEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowSettings = true;
        public bool EditorShowNetworking = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public Camera TargetCamera;

        private RenderTextureDescriptor renderTextureDescriptor;
        private RenderTexture rt;
        private RenderTexture rt_fx;

        [Range(1, 8192)] public int TargetWidth = 256;
        [Range(1, 8192)] public int TargetHeight = 256;

        private int streamWidth = 256;
        private int streamHeight = 256;

        [Range(10, 100)]
        public int Quality = 80;
        public FMChromaSubsamplingOption ChromaSubsampling = FMChromaSubsamplingOption.Subsampling420;

        [Range(0f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        [HideInInspector] public Material MatFMPCStream;

        private void Reset()
        {
            MatFMPCStream = new Material(Shader.Find("Hidden/FMPCStreamEncoder"));
            TargetCamera = GetComponent<Camera>();
            TargetCamera.depthTextureMode = DepthTextureMode.DepthNormals;
        }

        private void CheckResolution()
        {
            streamWidth = TargetWidth;
            streamHeight = TargetHeight;
            streamWidth = Mathf.Clamp(streamWidth, 1, 8192);
            streamHeight = Mathf.Clamp(streamHeight, 1, 8192);

            if (rt == null)
            {
                rt = new RenderTexture(streamWidth, streamHeight, 32, RenderTextureFormat.ARGB32);
                rt.Create();
                rt.filterMode = FilterMode.Point;
                TargetCamera.targetTexture = rt;
            }
            else
            {
                if (rt.width != streamWidth || rt.height != streamHeight)
                {
                    TargetCamera.targetTexture = null;
                    DestroyImmediate(rt);
                    rt = new RenderTexture(streamWidth, streamHeight, 32, RenderTextureFormat.ARGB32);
                    rt.Create();
                    rt.filterMode = FilterMode.Point;
                    TargetCamera.targetTexture = rt;
                }
            }

            if (CapturedTexture == null) { CapturedTexture = new Texture2D(streamWidth, streamHeight, TextureFormat.RGB24, false, false); }
            else
            {
                if (CapturedTexture.width != streamWidth || CapturedTexture.height != streamHeight)
                {
                    DestroyImmediate(CapturedTexture);
                    CapturedTexture = new Texture2D(streamWidth, streamHeight, TextureFormat.RGB24, false, false);
                    CapturedTexture.filterMode = FilterMode.Point;
                }
            }

            if (TargetCamera.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                TargetCamera.depthTextureMode = DepthTextureMode.DepthNormals;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (NeedUpdateTexture && !EncodingTexture)
            {
                NeedUpdateTexture = false;

                if (rt_fx == null)
                {
                    renderTextureDescriptor = source.descriptor;
                    renderTextureDescriptor.sRGB = false;
                    //renderTextureDescriptor.width *= 2;
                    rt_fx = RenderTexture.GetTemporary(renderTextureDescriptor);
                }
                else if (rt_fx.width != source.descriptor.width * 2)
                {
                    rt_fx.Release();
                    renderTextureDescriptor = source.descriptor;
                    //renderTextureDescriptor.width *= 2;
                    rt_fx = RenderTexture.GetTemporary(renderTextureDescriptor);
                }
                Graphics.Blit(source, rt_fx, MatFMPCStream);

                //RenderTexture to Texture2D
                ProcessCapturedTexture();
            }
            Graphics.Blit(source, destination);
        }

        private void RequestTextureUpdate()
        {
            if (EncodingTexture) return;
            NeedUpdateTexture = true;

            CheckResolution();
            TargetCamera.Render();
        }

        public bool FastMode = true;
        public bool AsyncMode = true;

        public FMGZipEncodeMode GZipMode = FMGZipEncodeMode.None;
        private bool NeedUpdateTexture = false;
        private bool EncodingTexture = false;

        public bool ignoreSimilarTexture = true;
        private int lastRawDataByte = 0;
        [Tooltip("Compare previous image data size(byte)")]
        public int similarByteSizeThreshold = 8;

        //experimental feature: check if your GPU supports AsyncReadback
        private bool supportsAsyncGPUReadback = false;
        public bool EnableAsyncGPUReadback = true;
        public bool SupportsAsyncGPUReadback { get { return supportsAsyncGPUReadback; } }

        public Texture2D CapturedTexture;
        public Texture GetStreamTexture
        {
            get
            {
                if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) return rt;
                return CapturedTexture;
            }
        }

        public bool OutputAsChunks = true;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 4001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int maximumChunkSize = Int32.MaxValue - 1024;
        public int OutputChunkSize = 1436;//8096; //32768
        public int GetChunkSize() { return OutputAsChunks ? OutputChunkSize : maximumChunkSize; }

        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByte = new Queue<byte[]>();

        public int dataLength;

        private void ProcessCapturedTexture()
        {
            streamWidth = rt_fx.width;
            streamHeight = rt_fx.height;

            if (!FastMode) EnableAsyncGPUReadback = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { ProcessCapturedTextureGPUReadbackAsync(); }
            else { ProcessCapturedTextureAsync(); }
        }

        private async void ProcessCapturedTextureAsync()
        {
            //render texture to texture2d
            //Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt_fx;
            CapturedTexture.ReadPixels(new Rect(0, 0, rt_fx.width, rt_fx.height), 0, 0);
            CapturedTexture.Apply();
            RenderTexture.active = previous;

            //encode to byte for streaming
            EncodeBytesAsync();
            await FMCoreTools.AsyncTask.Yield();
        }

        byte[] RawTextureData = new byte[0];
        private NativeArray<byte> asyncGPUReadbackNativeArray;
        private async void ProcessCapturedTextureGPUReadbackAsync()
        {
#if UNITY_2023_1_OR_NEWER
            await Awaitable.EndOfFrameAsync();
#else
            await WaitForEndOfFrameAsync();
#endif
#if UNITY_2018_2_OR_NEWER
            if (rt_fx != null)
            {
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt_fx, 0, TextureFormat.RGB24);
                while (!request.done)
                {
                    await FMCoreTools.AsyncTask.Yield();
                    if (!request.done) await FMCoreTools.AsyncTask.Delay(1);
                }
                if (!request.hasError)
                {
                    asyncGPUReadbackNativeArray = request.GetData<byte>();
                    FMCoreTools.NativeArrayCopyTo(asyncGPUReadbackNativeArray, ref RawTextureData);
                    EncodeBytesAsync(true);
                }
                else { EncodingTexture = false; }
            }
            else { EncodingTexture = false; }
#endif
        }

        private async void InvokeEventsCheckerAsync()
        {
            while (!stoppedOrCancelled())
            {
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();
                while (AppendQueueSendByte.Count > 0) OnDataByteReadyEvent.Invoke(AppendQueueSendByte.Dequeue());
            }
        }

        private async void SenderAsync()
        {
            InvokeEventsCheckerAsync();
            while (!stoppedOrCancelled())
            {
                if (Time.realtimeSinceStartup > next)
                {
                    if (StreamFPS > 0)
                    {
                        interval = 1f / StreamFPS;
                        next = Time.realtimeSinceStartup + interval;

                        RequestTextureUpdate();
                    }
                }
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();
            }
        }

        private async void EncodeBytesAsync(bool assignedRawTexture = false)
        {
            if (!assignedRawTexture)
            {
                FMCoreTools.NativeArrayCopyTo(CapturedTexture.GetRawTextureData<byte>(), ref RawTextureData);
                streamWidth = CapturedTexture.width;
                streamHeight = CapturedTexture.height;
            }

            if (CapturedTexture != null || RawTextureData != null)
            {
                bool detectedSimilarTexture = false;
                if (FastMode)
                {
                    //try AsyncMode, on supported platform
                    if (AsyncMode)
                    {
                        //new method via async
                        await Task.Run(() => { try { dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling); } catch { } });
                    }
                    else
                    {
                        dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);
                    }
                }
                else { dataByte = CapturedTexture.EncodeToJPG(Quality); }

                if (ignoreSimilarTexture) detectedSimilarTexture = FMCoreTools.CheckSimilarSize(dataByte.Length, lastRawDataByte, similarByteSizeThreshold);
                lastRawDataByte = dataByte.Length;

                if (GZipMode != FMGZipEncodeMode.None && !detectedSimilarTexture) dataByte = await FMZipHelper.FMZippedByteAsync(dataByte, cancellationTokenSource_global.Token, GZipMode);
                if (!detectedSimilarTexture && dataByte != null)
                {
                    //add camera meta data
                    byte[] _camNearClipPlane = BitConverter.GetBytes(TargetCamera.nearClipPlane);
                    byte[] _camFarClipPlane = BitConverter.GetBytes(TargetCamera.farClipPlane);

                    byte[] _camFOV = BitConverter.GetBytes(TargetCamera.fieldOfView);
                    byte[] _camAspect = BitConverter.GetBytes(TargetCamera.aspect);

                    byte[] _camOrthographicProjection = BitConverter.GetBytes((TargetCamera.orthographic ? 1f : 0f));
                    byte[] _camOrthographicSize = BitConverter.GetBytes(TargetCamera.orthographicSize);

                    byte[] _dataByteTmp = new byte[dataByte.Length + 24];

                    Buffer.BlockCopy(_camNearClipPlane, 0, _dataByteTmp, 0, 4);
                    Buffer.BlockCopy(_camFarClipPlane, 0, _dataByteTmp, 4, 4);
                    Buffer.BlockCopy(_camFOV, 0, _dataByteTmp, 8, 4);
                    Buffer.BlockCopy(_camAspect, 0, _dataByteTmp, 12, 4);
                    Buffer.BlockCopy(_camOrthographicProjection, 0, _dataByteTmp, 16, 4);
                    Buffer.BlockCopy(_camOrthographicSize, 0, _dataByteTmp, 20, 4);
                    Buffer.BlockCopy(dataByte, 0, _dataByteTmp, 24, dataByte.Length);
                    dataByte = _dataByteTmp;

                    dataByteTemp = dataByte.ToArray();
                    EncodingTexture = false;
                    //==================getting byte data==================
                    int _length = dataByteTemp.Length;
                    dataLength = _length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    int _metaByteLength = 14;
                    int _chunkSize = GetChunkSize();
                    _chunkSize -= _metaByteLength;
                    int _chunkCount = Mathf.CeilToInt((float)_length / (float)_chunkSize);
                    for (int i = 1; i <= _chunkCount; i++)
                    {
                        int _dataByteLength = (i == _chunkCount) ? (_length % _chunkSize) : (_chunkSize);
                        byte[] _meta_offset = BitConverter.GetBytes(_offset);
                        byte[] SendByte = new byte[_dataByteLength + _metaByteLength];

                        Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                        Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                        Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                        Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                        SendByte[12] = (byte)(GZipMode != FMGZipEncodeMode.None ? 1 : 0);
                        SendByte[13] = (byte)0;//not used, but just keep one empty byte for standard

                        Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 14, _dataByteLength);
                        AppendQueueSendByte.Enqueue(SendByte);

                        _offset += _chunkSize;
                    }

                    dataID++;
                    if (dataID > maxID) dataID = 0;
                }
            }

            EncodingTexture = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback)
            {
                //dispose and release memory from AsyncGPUReadback
                if (asyncGPUReadbackNativeArray.IsCreated) asyncGPUReadbackNativeArray.Dispose();
            }
        }

        private void Start()
        {
#if UNITY_2018_2_OR_NEWER
            try { supportsAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback; }
            catch { supportsAsyncGPUReadback = false; }
#else
            supportsAsyncGPUReadback = false;
#endif

            StartAll();
        }
        private void OnEnable() { StartAll(); }
        private void OnDisable() { StopAll(); }
        private void OnApplicationQuit() { StopAll(); }
        private void OnDestroy() { StopAll(); }

        private CancellationTokenSource cancellationTokenSource_global;
        private bool stoppedOrCancelled() { return stop || cancellationTokenSource_global.IsCancellationRequested; }
        private void StopAllAsync()
        {
            if (cancellationTokenSource_global != null)
            {
                if (!cancellationTokenSource_global.IsCancellationRequested) cancellationTokenSource_global.Cancel();
            }
        }
        public Task<bool> WaitForEndOfFrameAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(FMCoreTools.AsyncTask.WaitForEndOfFrameCOR(tcs));
            return tcs.Task;
        }

        private void StopAll()
        {
            initialised = false;
            stop = true;
            StopAllAsync();

            lastRawDataByte = 0;
            AppendQueueSendByte.Clear();
        }

        private bool initialised = false;
        private void StartAll()
        {
            cancellationTokenSource_global = new CancellationTokenSource();

            if (initialised) return;
            initialised = true;

            CheckFastModeSupport();

            stop = false;
            SenderAsync();

            NeedUpdateTexture = false;
            EncodingTexture = false;
        }

        private void CheckFastModeSupport()
        {
#if UNITY_IOS && !UNITY_EDITOR
            FastMode = true;
#endif


#if !UNITY_WEBGL && (UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN || UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
            //supported fast mode
#else
            //not supported fast mode
            FastMode = false;
#endif
        }
    }
}