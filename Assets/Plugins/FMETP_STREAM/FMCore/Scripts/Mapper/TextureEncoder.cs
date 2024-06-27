using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Unity.Collections;
using FMSolution.FMZip;

namespace FMSolution.FMETP
{
    [Serializable]
    public enum FMTextureType { Texture2D, RenderTexture, WebcamTexture }
    [AddComponentMenu("FMETP/Mapper/TextureEncoder")]
    public class TextureEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowMode = true;
        public bool EditorShowSettings = true;
        public bool EditorShowNetworking = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public FMTextureType TextureType = FMTextureType.Texture2D;
        public Texture2D StreamTexture;
        public RenderTexture StreamRenderTexture;

        #if !FM_WEBCAM_DISABLED
        public WebCamTexture StreamWebCamTexture;
        public RenderTexture StreamWebCamRenderTexture;
        public WebCamTexture SetStreamWebCamTexture { set { StreamWebCamTexture = value; } }
        #endif

        [Range(0.05f, 1f)]
        public float ResolutionScaling = 0.5f;

        public Texture GetStreamTexture
        {
            get
            {
                if (TextureType == FMTextureType.Texture2D) return StreamTexture;
                #if !FM_WEBCAM_DISABLED
                if (TextureType == FMTextureType.WebcamTexture) return StreamWebCamTexture;
                #endif
                return StreamRenderTexture;
            }
        }
        public Texture GetPreviewTexture
        {
            get
            {
                if (TextureType == FMTextureType.Texture2D) return StreamTexture;
                #if !FM_WEBCAM_DISABLED
                if (TextureType == FMTextureType.WebcamTexture) return StreamWebCamRenderTexture;
                #endif
                return StreamRenderTexture;
            }
        }

        public bool FastMode = true;
        public bool AsyncMode = true;
        public FMGZipEncodeMode GZipMode = FMGZipEncodeMode.None;

        [Range(10, 100)]
        public int Quality = 40;
        public FMChromaSubsamplingOption ChromaSubsampling = FMChromaSubsamplingOption.Subsampling420;

        [Range(0f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        public bool ignoreSimilarTexture = true;
        private int lastRawDataByte = 0;
        [Tooltip("Compare previous image data size(byte)")]
        public int similarByteSizeThreshold = 8;

        private bool NeedUpdateTexture = false;
        private bool EncodingTexture = false;

        //experimental feature: check if your GPU supports AsyncReadback
        private bool supportsAsyncGPUReadback = false;
        public bool EnableAsyncGPUReadback = true;
        public bool SupportsAsyncGPUReadback { get { return supportsAsyncGPUReadback; } }

        public GameViewOutputFormat OutputFormat = GameViewOutputFormat.FMMJPEG;
        public bool OutputAsChunks = true;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawMJPEGReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawRGB24ReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 1001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int maximumChunkSize = Int32.MaxValue - 1024;
        public int OutputChunkSize = 1436;//8096; //32768
        public int GetChunkSize() { return OutputAsChunks ? OutputChunkSize : maximumChunkSize; }
        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByteFMMJPEG = new Queue<byte[]>();
        private Queue<byte[]> AppendQueueSendByteMJPEG = new Queue<byte[]>();

        public int dataLength;

        //texture settings
        private byte[] RawTextureData = new byte[0];
        private int streamWidth = 8;
        private int streamHeight = 8;

        public int StreamWidth { get { return streamWidth; } }
        public int StreamHeight { get { return streamHeight; } }

        private void Start()
        {
            Application.runInBackground = true;

#if UNITY_2018_2_OR_NEWER
            supportsAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
#else
            supportsAsyncGPUReadback = false;
#endif

            StartAll();
        }

        public void Action_StreamTexture(byte[] inputRawTextureData, int inputWidth, int inputHeight)
        {
            int stride = inputRawTextureData.Length / (inputWidth * inputHeight);
            if (stride != 3 && stride != 4)
            {
                Debug.LogError("unsupported stride count: " + stride + ", only RGB24(stride: 3) and RGBA32(stride: 4) are supported");
                return;
            }

            if (RawTextureData.Length != inputRawTextureData.Length) RawTextureData = new byte[inputRawTextureData.Length];
            Buffer.BlockCopy(inputRawTextureData, 0, RawTextureData, 0, inputRawTextureData.Length);

            streamWidth = inputWidth;
            streamHeight = inputHeight;
            NeedUpdateTexture = true;
        }

        public void Action_StreamTexture(Texture2D inputTexture2D)
        {
            if (inputTexture2D == null) return;
            if (!inputTexture2D.isReadable) return;
            if (inputTexture2D.format != TextureFormat.RGB24 && inputTexture2D.format != TextureFormat.RGBA32)
            {
                Debug.LogError("unsupported formmat: " + inputTexture2D.format + ", only RGB24 and RGBA32 are supported");
                return;
            }

            FMCoreTools.NativeArrayCopyTo(inputTexture2D.GetRawTextureData<byte>(), ref RawTextureData);
            Action_StreamTexture(RawTextureData, inputTexture2D.width, inputTexture2D.height);
        }

        #if !FM_WEBCAM_DISABLED
        public void Action_StreamWebcamTexture(WebCamTexture inputWebcamTexture)
        {
            if (inputWebcamTexture == null) return;

            streamWidth = Mathf.RoundToInt(inputWebcamTexture.width * ResolutionScaling);
            streamHeight = Mathf.RoundToInt(inputWebcamTexture.height * ResolutionScaling);

            if (streamWidth < 1) streamWidth = 1;
            if (streamHeight < 1) streamHeight = 1;

            if (StreamWebCamRenderTexture == null)
            {
                StreamWebCamRenderTexture = new RenderTexture(streamWidth, streamHeight, 0, RenderTextureFormat.ARGB32);
                StreamWebCamRenderTexture.Create();
            }
            else
            {
                if (StreamWebCamRenderTexture.width != streamWidth || StreamWebCamRenderTexture.height != streamHeight)
                {
                    Destroy(StreamWebCamRenderTexture);
                    StreamWebCamRenderTexture = new RenderTexture(streamWidth, streamHeight, 0, RenderTextureFormat.ARGB32);
                    StreamWebCamRenderTexture.Create();
                }
            }

            Graphics.Blit(inputWebcamTexture, StreamWebCamRenderTexture);
            Action_StreamRenderTexture(StreamWebCamRenderTexture);
        }
        #endif

        public void Action_StreamRenderTexture(RenderTexture inputRenderTexture)
        {
            if (inputRenderTexture == null) return;

            streamWidth = inputRenderTexture.width;
            streamHeight = inputRenderTexture.height;

            //RenderTexture to Texture2D
            if (!FastMode) EnableAsyncGPUReadback = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { ProcessCapturedTextureGPUReadbackAsync(inputRenderTexture); }
            else
            {
                if (StreamTexture == null) { StreamTexture = new Texture2D(inputRenderTexture.width, inputRenderTexture.height, TextureFormat.RGB24, false); }
                else
                {
                    if (StreamTexture.width != inputRenderTexture.width || StreamTexture.height != inputRenderTexture.height)
                    {
                        DestroyImmediate(StreamTexture);
                        StreamTexture = new Texture2D(inputRenderTexture.width, inputRenderTexture.height, TextureFormat.RGB24, false);
                    }
                }

                RenderTexture.active = inputRenderTexture;
                StreamTexture.ReadPixels(new Rect(0, 0, inputRenderTexture.width, inputRenderTexture.height), 0, 0);
                StreamTexture.Apply();
                RenderTexture.active = null;

                Action_StreamTexture(StreamTexture);
            }
        }

        private NativeArray<byte> asyncGPUReadbackNativeArray;
        private async void ProcessCapturedTextureGPUReadbackAsync(RenderTexture inputRenderTexture)
        {
#if UNITY_2023_1_OR_NEWER
            await Awaitable.EndOfFrameAsync();
#else
            await WaitForEndOfFrameAsync();
#endif
#if UNITY_2018_2_OR_NEWER
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(inputRenderTexture, 0, TextureFormat.RGB24);
            while (!request.done)
            {
                await FMCoreTools.AsyncTask.Yield();
                if (!request.done) await FMCoreTools.AsyncTask.Delay(1);
            }
            if (!request.hasError)
            {
                asyncGPUReadbackNativeArray = request.GetData<byte>();
                RawTextureData = asyncGPUReadbackNativeArray.ToArray();
            }
            NeedUpdateTexture = true;
#endif
        }

        private void RequestTextureUpdate()
        {
            switch (TextureType)
            {
                case FMTextureType.Texture2D:
                    if (StreamTexture != null) Action_StreamTexture(StreamTexture);
                    break;
                case FMTextureType.RenderTexture:
                    if (StreamRenderTexture != null) Action_StreamRenderTexture(StreamRenderTexture);
                    break;
                case FMTextureType.WebcamTexture:
                    #if !FM_WEBCAM_DISABLED
                    if (StreamWebCamTexture != null) Action_StreamWebcamTexture(StreamWebCamTexture);
                    #endif
                    break;
            }


            if (!NeedUpdateTexture) return;
            if (!EncodingTexture)
            {
                //update it now
                EncodingTexture = true;
                EncodeBytesAsync();

                NeedUpdateTexture = false;
            }
        }

        private async void InvokeEventsCheckerAsync()
        {
            while (!stoppedOrCancelled())
            {
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();
                while (AppendQueueSendByteFMMJPEG.Count > 0) OnDataByteReadyEvent.Invoke(AppendQueueSendByteFMMJPEG.Dequeue());
                while (AppendQueueSendByteMJPEG.Count > 0) OnRawMJPEGReadyEvent.Invoke(AppendQueueSendByteMJPEG.Dequeue());
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

        private async void EncodeBytesAsync()
        {
            if (OutputFormat == GameViewOutputFormat.RAWRGB24)
            {
                if (RawTextureData == null) OnRawRGB24ReadyEvent.Invoke(RawTextureData);
            }
            else
            {
                //==================getting byte data==================
#if UNITY_IOS && !UNITY_EDITOR
                FastMode = true;
#endif

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN || UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                //supported fast mode
#else
                //not supported fast mode
                FastMode = false;
#endif
                if (OutputFormat != GameViewOutputFormat.FMMJPEG)
                {
                    GZipMode = FMGZipEncodeMode.None;
                }

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
                else
                {
                    dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);
                }

                if (ignoreSimilarTexture) detectedSimilarTexture = FMCoreTools.CheckSimilarSize(dataByte.Length, lastRawDataByte, similarByteSizeThreshold);
                lastRawDataByte = dataByte.Length;

                if (GZipMode != FMGZipEncodeMode.None && !detectedSimilarTexture) dataByte = await FMZipHelper.FMZippedByteAsync(dataByte, cancellationTokenSource_global.Token, GZipMode);
                if (!detectedSimilarTexture && dataByte != null)
                {
                    dataByteTemp = dataByte.ToArray();
                    EncodingTexture = false;
                    //==================getting byte data==================
                    int _length = dataByteTemp.Length;
                    dataLength = _length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    int _metaByteLength = 15;
                    int _chunkSize = GetChunkSize();
                    if (OutputFormat == GameViewOutputFormat.FMMJPEG) _chunkSize -= _metaByteLength;
                    int _chunkCount = Mathf.CeilToInt((float)_length / (float)_chunkSize);
                    for (int i = 1; i <= _chunkCount; i++)
                    {
                        int _dataByteLength = (i == _chunkCount) ? (_length % _chunkSize) : (_chunkSize);
                        if (OutputFormat == GameViewOutputFormat.FMMJPEG)
                        {
                            byte[] _meta_offset = BitConverter.GetBytes(_offset);
                            byte[] SendByte = new byte[_dataByteLength + _metaByteLength];

                            Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                            Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                            Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                            Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                            SendByte[12] = (byte)(GZipMode != FMGZipEncodeMode.None ? 1 : 0);
                            SendByte[13] = (byte)0;
                            SendByte[14] = (byte)0;

                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, _metaByteLength, _dataByteLength);
                            AppendQueueSendByteFMMJPEG.Enqueue(SendByte);
                        }
                        else if (OutputFormat == GameViewOutputFormat.MJPEG)
                        {
                            //==================output raw mjpeg data==================
                            byte[] SendByte = new byte[_dataByteLength];
                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 0, _dataByteLength);
                            AppendQueueSendByteMJPEG.Enqueue(SendByte);
                        }

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
            AppendQueueSendByteFMMJPEG.Clear();
            AppendQueueSendByteMJPEG.Clear();
        }

        private bool initialised = false;
        private void StartAll()
        {
            cancellationTokenSource_global = new CancellationTokenSource();

            if (initialised) return;
            initialised = true;

            stop = false;
            SenderAsync();

            NeedUpdateTexture = false;
            EncodingTexture = false;
        }
    }
}