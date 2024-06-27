using System.Collections;
using UnityEngine;
using System;

using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;

using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;
using Unity.Collections;
using FMSolution.FMZip;

namespace FMSolution.FMETP
{
    public enum GameViewCaptureMode { RenderCam, MainCam, FullScreen, Desktop }
    public enum GameViewCubemapSample
    {
        VeryHigh = 4096,
        High = 2048,
        Medium = 1024,
        Low = 512,
        Minimum = 256
    }
    public enum FMDesktopSystemOS
    {
        NULL = 0,
        WIN = 1,
        OSX = 2,
        LINUX = 3
    }
    public enum FMDesktopDisplayID
    {
        Display1 = 0,
        Display2 = 1,
        Display3 = 2,
        Display4 = 3,
        Display5 = 4,
        Display6 = 5,
        Display7 = 6,
        Display8 = 7
    }
    public enum FMDesktopRotationAngle
    {
        Degree0 = 0,
        Degree90 = 90,
        Degree180 = 180,
        Degree270 = 270
    }
    public enum GameViewOutputFormat { FMMJPEG, MJPEG, RAWRGB24 }
    public enum GameViewPreviewType { None, RawImage, MeshRenderer }

    [AddComponentMenu("FMETP/Mapper/GameViewEncoder")]
    public class GameViewEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowMode = true;
        public bool EditorShowSettings = true;
        public bool EditorShowNetworking = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public static GameViewEncoder InitAndCreateFMLive(GameObject go,
            GameViewCaptureMode inputGameViewCaptureMode,
            GameViewOutputFormat inputGameViewOutputFormat,

            Vector2 inputResolution,
            float inputStreamFPS,

            bool inputFastMode,
            bool inputAsyncMode,
            bool inputEnableAsyncGPUReadbacak

            )
        {
            GameViewEncoder _encoder = go.AddComponent<GameViewEncoder>();
            _encoder.RenderCam = go.AddComponent<Camera>();

            _encoder.CaptureMode = inputGameViewCaptureMode;
            _encoder.OutputFormat = inputGameViewOutputFormat;
            _encoder.Resolution = inputResolution;
            _encoder.renderResolution = inputResolution;
            _encoder.StreamFPS = inputStreamFPS;

            _encoder.FastMode = inputFastMode;
            _encoder.AsyncMode = inputAsyncMode;
            _encoder.EnableAsyncGPUReadback = inputEnableAsyncGPUReadbacak;

            return _encoder;
        }
        public static GameViewEncoder InitAndCreateDesktopCapture(
            GameObject go,
            Material inputMatFMDesktop,
            Material inputMatColorAdjustment,

            Material inputMatPano,
            Material inputMatMixedReality,

            GameViewCaptureMode inputCaptureMode,
            FMDesktopDisplayID inputFMDesktopTargetDisplay,
            bool inputFMDesktopShowCursor,
            bool inputFMDesktopFlipX,
            bool inputFMDesktopFlipY,
            float inputFMDesktopRangeX,
            float inputFMDesktopRangeY,
            float inputFMDesktopOffsetX,
            float inputFMDesktopOffsetY,
            FMDesktopRotationAngle inputFMDesktopRotationAngle,

            Vector2 inputResolution,
            bool inputMatchScreenAspect,
            int inputQuality,
            float inputStreamFPS,
            bool inputFastMode,
            bool inputAsyncMode,
            FMGZipEncodeMode inputGZipMode,
            FMChromaSubsamplingOption inputChromaSubsampling,
            int inputColorReductionLevel,

            bool inputIgnoreSimilarTexture,
            int inputSimilarByteSizeThreshold,
            GameViewPreviewType inputPreviewType,
            RawImage inputPreviewRawImage,
            MeshRenderer inputPreviewMeshRenderer,
            GameViewOutputFormat inputOutputFormat,
            bool inputOutputAsChunks,
            UInt16 inputLabel
            )
        {
            GameViewEncoder _encoder = go.AddComponent<GameViewEncoder>();

            _encoder.MatFMDesktop = inputMatFMDesktop;
            _encoder.MatColorAdjustment = inputMatColorAdjustment;

            _encoder.MatPano = inputMatPano;
            _encoder.MatMixedReality = inputMatMixedReality;

            _encoder.CaptureMode = inputCaptureMode;
            _encoder.FMDesktopTargetDisplay = inputFMDesktopTargetDisplay;
            _encoder.FMDesktopShowCursor = inputFMDesktopShowCursor;

            _encoder.FMDesktopFlipX = inputFMDesktopFlipX;
            _encoder.FMDesktopFlipY = inputFMDesktopFlipY;
            _encoder.FMDesktopRangeX = inputFMDesktopRangeX;
            _encoder.FMDesktopRangeY = inputFMDesktopRangeY;
            _encoder.FMDesktopOffsetX = inputFMDesktopOffsetX;
            _encoder.FMDesktopOffsetY = inputFMDesktopOffsetY;

            _encoder.FMDesktopRotationAngle = inputFMDesktopRotationAngle;

            _encoder.Resolution = inputResolution;
            _encoder.MatchScreenAspect = inputMatchScreenAspect;

            _encoder.Quality = inputQuality;
            _encoder.StreamFPS = inputStreamFPS;
            _encoder.FastMode = inputFastMode;
            _encoder.AsyncMode = inputAsyncMode;
            _encoder.GZipMode = inputGZipMode;
            _encoder.ChromaSubsampling = inputChromaSubsampling;
            _encoder.ColorReductionLevel = inputColorReductionLevel;

            _encoder.ignoreSimilarTexture = inputIgnoreSimilarTexture;
            _encoder.similarByteSizeThreshold = inputSimilarByteSizeThreshold;

            _encoder.PreviewType = inputPreviewType;
            _encoder.PreviewRawImage = inputPreviewRawImage;
            _encoder.PreviewMeshRenderer = inputPreviewMeshRenderer;
            _encoder.OutputFormat = inputOutputFormat;
            _encoder.OutputAsChunks = inputOutputAsChunks;

            _encoder.label = inputLabel;
            return _encoder;
        }

        public GameViewCaptureMode CaptureMode = GameViewCaptureMode.RenderCam;
        private GameViewCaptureMode _CaptureMode = GameViewCaptureMode.RenderCam;
        [Range(0.05f, 1f)] public float ResolutionScaling = 0.25f;

        public Camera MainCam;
        public Camera RenderCam;

        public Vector2 Resolution = new Vector2(512, 512);
        private Vector2 renderResolution = new Vector2(512, 512);
        private void CheckRenderResolution()
        {
            Resolution.x = Mathf.RoundToInt(Resolution.x);
            Resolution.y = Mathf.RoundToInt(Resolution.y);
            if (Resolution.x < 2) Resolution.x = 2;
            if (Resolution.y < 2) Resolution.y = 2;
            renderResolution = Resolution;
        }

        public bool MatchScreenAspect = true;

        public bool FastMode = true;
        public bool AsyncMode = true;

        ///<summary>
        /// None: Not Apply GZip
        /// LowLatency: Apply GZip in current Thread(Sync)
        /// Balance: Apply GZip in current Thread(Async)
        /// HighPerformance: Apply GZip in other Thread
        ///</summary>
        public FMGZipEncodeMode GZipMode = FMGZipEncodeMode.None;
        public bool PanoramaMode = false;

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

        private int streamWidth;
        private int streamHeight;

        public GameViewPreviewType PreviewType = GameViewPreviewType.None;
        public RawImage PreviewRawImage;
        public MeshRenderer PreviewMeshRenderer;
        public Texture2D CapturedTexture;
        public Texture GetStreamTexture
        {
            get
            {
#if !FMETP_URP
                if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) return rt;
#endif
                return CapturedTexture;
            }
        }
        private RenderTextureDescriptor sourceDescriptor;
        public RenderTextureDescriptor OutputRenderTextureDescriptor { get { return sourceDescriptor; } }
        private RenderTexture rt_reserved;//try to reserved existing render texture in RenderCamera Mode
        private bool reservedExistingRenderTexture = false;

        private RenderTexture rt;
        private RenderTexture rt_cube;

        #if !FM_WEBCAM_DISABLED
        private WebCamTexture webcamTexture;
        public WebCamTexture WebcamTexture { get { return webcamTexture; } set { webcamTexture = value; } }
        public void Action_SetWebcamTexture(WebCamTexture inputWebcamTexture) { webcamTexture = inputWebcamTexture; }
        #endif

        [HideInInspector] public Material MatPano; //has to be public, otherwise the shader will be  missing
        [HideInInspector] public Material MatFMDesktop;
        [HideInInspector] public Material MatColorAdjustment;
        [HideInInspector] public Material MatMixedReality;

        public bool EnableMixedReality = false;
        #if !FM_WEBCAM_DISABLED
        private WebcamManager webcamManager;

        public int MixedRealityTargetCamID = 0;
        public bool MixedRealityUseFrontCam = false;
        public Vector2 MixedRealityRequestResolution = new Vector2(1280, 720);

        public bool MixedRealityFlipX = false;
        public bool MixedRealityFlipY = false;
        [Range(0.01f, 2f)] public float MixedRealityScaleX = 1f;
        [Range(0.01f, 2f)] public float MixedRealityScaleY = 1f;
        [Range(-0.5f, 0.5f)] public float MixedRealityOffsetX = 0f;
        [Range(-0.5f, 0.5f)] public float MixedRealityOffsetY = 0f;
        #endif

        [Range(0, 2)]
        public int ColorReductionLevel = 0;
        private float brightness { get { return 1f / Mathf.Pow(2, ColorReductionLevel); } }

        //for URP only
        [HideInInspector] public Material mat_source;

        public GameViewCubemapSample CubemapResolution = GameViewCubemapSample.Medium;

        private Texture2D Screenshot;
        private ColorSpace ColorSpace;

        public bool FMDesktopShowAdvancedOptions = false;
        private FMDesktopManager fmDesktopManager;
        private float fmDesktopMonitorAspectRatio = 1f;

        private FMDesktopSystemOS fmDesktopSystemOS = FMDesktopSystemOS.NULL;
        private Int16 fmDesktopMonitorOffsetX = 0;
        private Int16 fmDesktopMonitorOffsetY = 0;
        private Int16 fmDesktopMonitorScaling = 1;
        private Int16 fmDesktopFrameWidth = 0;
        private Int16 fmDesktopFrameHeight = 0;
        private Int16 fmDesktopFrameStride = 0;
        public Int16 FMDesktopMonitorOffsetX { get { return fmDesktopMonitorOffsetX; } }
        public Int16 FMDesktopMonitorOffsetY { get { return fmDesktopMonitorOffsetY; } }
        public Int16 FMDesktopMonitorScaling { get { return fmDesktopMonitorScaling; } }
        public Int16 FMDesktopFrameWidth { get { return fmDesktopFrameWidth; } }
        public Int16 FMDesktopFrameHeight { get { return fmDesktopFrameHeight; } }
        public Int16 FMDesktopFrameStride { get { return fmDesktopFrameStride; } }

        //public Int16 FMDesktopStreamMonitorOffsetX { get { return (Int16)((float)fmDesktopMonitorOffsetX + (((float)fmDesktopFrameWidth - ((float)fmDesktopFrameWidth * FMDesktopRangeX)) * 0.5f) + ((float)fmDesktopFrameWidth * FMDesktopOffsetX)); } }
        //public Int16 FMDesktopStreamMonitorOffsetY { get { return (Int16)((float)fmDesktopMonitorOffsetY + (((float)fmDesktopFrameHeight - ((float)fmDesktopFrameHeight * FMDesktopRangeY)) * 0.5f) + ((float)fmDesktopFrameHeight * FMDesktopOffsetY)); } }
        //simplified result..
        //Calculation for cropped streaming result
        public Int16 FMDesktopStreamMonitorOffsetX { get { return (Int16)((float)fmDesktopMonitorOffsetX + ((float)fmDesktopFrameWidth * (((1f - FMDesktopRangeX) * 0.5f) + FMDesktopOffsetX))); } }
        public Int16 FMDesktopStreamMonitorOffsetY { get { return (Int16)((float)fmDesktopMonitorOffsetY + ((float)fmDesktopFrameHeight * (((1f - FMDesktopRangeY) * 0.5f) + FMDesktopOffsetY))); } }

        public Int16 FMDesktopStreamMonitorScaling { get { return fmDesktopMonitorScaling; } }
        public Int16 FMDesktopStreamFrameWidth { get { return (Int16)((float)fmDesktopFrameWidth * FMDesktopRangeX); } }
        public Int16 FMDesktopStreamFrameHeight { get { return (Int16)((float)fmDesktopFrameHeight * FMDesktopRangeY); } }

        public float FMDesktopRotation = 0f;

        public Vector2 FMDesktopResolution = Vector2.zero;
        public bool FMDesktopFlipX = true;
        public bool FMDesktopFlipY = false;
        public bool FMDesktopShowCursor = true;

        [Range(0.001f, 1f)] public float FMDesktopRangeX = 1f;
        [Range(0.001f, 1f)] public float FMDesktopRangeY = 1f;
        [Range(-0.5f, 0.5f)] public float FMDesktopOffsetX = 0f;
        [Range(-0.5f, 0.5f)] public float FMDesktopOffsetY = 0f;
        public FMDesktopRotationAngle FMDesktopRotationAngle = FMDesktopRotationAngle.Degree0;
        public TextureFormat FMDesktopTextureFormat = TextureFormat.RGBA32;

        public FMDesktopDisplayID FMDesktopTargetDisplay = FMDesktopDisplayID.Display1;
        public int FMDesktopMonitorID { get { return (int)FMDesktopTargetDisplay; } }
        [Range(0, 8)] public int FMDesktopMonitorCount = 0;

        public GameViewOutputFormat OutputFormat = GameViewOutputFormat.FMMJPEG;
        public bool OutputAsChunks = true;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawMJPEGReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawRGB24ReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        [Range(1000,UInt16.MaxValue)] public UInt16 label = 1001;
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

        private void CaptureModeUpdate()
        {
#if !UNITY_EDITOR && !UNITY_STANDALONE
            if (CaptureMode == GameViewCaptureMode.Desktop) CaptureMode = GameViewCaptureMode.FullScreen;
#endif
            if (_CaptureMode != CaptureMode)
            {
                _CaptureMode = CaptureMode;
                if (rt != null) Destroy(rt);
                if (CapturedTexture != null) Destroy(CapturedTexture);

                if (fmDesktopManager != null) Destroy(fmDesktopManager);

                AssignMaterials();
            }
        }

        private void AssignMaterials(bool _override = false)
        {
            if (_override)
            {
                try { MatPano = new Material(Shader.Find("Hidden/FMCubemapToEquirect")); } catch (Exception e) { Debug.LogWarning(e); }
                try { MatFMDesktop = new Material(Shader.Find("Hidden/FMDesktopMask")); } catch (Exception e) { Debug.LogWarning(e); }
                try { MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment")); } catch (Exception e) { Debug.LogWarning(e); }
                try { MatMixedReality = new Material(Shader.Find("Hidden/FMETPMixedReality")); } catch (Exception e) { Debug.LogWarning(e); }
#if FMETP_URP
                //for URP only
                try { mat_source = new Material(Shader.Find("Hidden/FMETPMainCamURP")); } catch (Exception e) { Debug.LogWarning(e); }
#endif
            }
            else
            {
                if (MatPano == null) try { MatPano = new Material(Shader.Find("Hidden/FMCubemapToEquirect")); } catch (Exception e) { Debug.LogWarning(e); }
                if (MatFMDesktop == null) try { MatFMDesktop = new Material(Shader.Find("Hidden/FMDesktopMask")); } catch (Exception e) { Debug.LogWarning(e); }
                if (MatColorAdjustment == null) try { MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment")); } catch (Exception e) { Debug.LogWarning(e); }
                if (MatMixedReality == null) try { MatMixedReality = new Material(Shader.Find("Hidden/FMETPMixedReality")); } catch (Exception e) { Debug.LogWarning(e); }
#if FMETP_URP
                //for URP only
                if(mat_source == null) try { mat_source = new Material(Shader.Find("Hidden/FMETPMainCamURP")); } catch (Exception e) { Debug.LogWarning(e); }
#endif
            }
        }

        //init when added component, or reset component
        private void Reset()
        {
            AssignMaterials(true);
        }

        private void Start()
        {
            Application.runInBackground = true;
            ColorSpace = QualitySettings.activeColorSpace;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            fmDesktopSystemOS = FMDesktopSystemOS.WIN;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            fmDesktopSystemOS = FMDesktopSystemOS.OSX;
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            fmDesktopSystemOS = FMDesktopSystemOS.LINUX;
#endif

#if UNITY_2018_2_OR_NEWER
            try { supportsAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback; }
            catch { supportsAsyncGPUReadback = false; }
#else
            supportsAsyncGPUReadback = false;
#endif

            sourceDescriptor = (UnityEngine.XR.XRSettings.enabled) ? UnityEngine.XR.XRSettings.eyeTextureDesc : new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32);
            sourceDescriptor.depthBufferBits = 24;

#if WINDOWS_UWP
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback && FastMode) sourceDescriptor.colorFormat = RenderTextureFormat.ARGB32;
#endif

            if (RenderCam != null)
            {
                if (RenderCam.targetTexture != null)
                {
                    rt_reserved = RenderCam.targetTexture;
                    reservedExistingRenderTexture = true;
                }
                else
                {
                    reservedExistingRenderTexture = false;
                }
            }

            CaptureModeUpdate();
            StartAll();
        }

        private void Update()
        {
            CheckRenderResolution();
            CaptureModeUpdate();

            switch (_CaptureMode)
            {
                case GameViewCaptureMode.MainCam:
                    if (MainCam == null) MainCam = this.GetComponent<Camera>();
                    if (!EncodingTexture) renderResolution = new Vector2(Screen.width, Screen.height) * ResolutionScaling;
                    if (sourceDescriptor.vrUsage == VRTextureUsage.TwoEyes) renderResolution.x /= 2f;
#if !FM_WEBCAM_DISABLED
                    if (EnableMixedReality)
                    {
                        if (webcamManager == null)
                        {
                            webcamManager = this.gameObject.AddComponent<WebcamManager>();
                            webcamManager.hideFlags = HideFlags.HideInInspector;

                            webcamManager.TargetCamID = MixedRealityTargetCamID;
                            webcamManager.useFrontCam = MixedRealityUseFrontCam;
                            webcamManager.requestResolution = MixedRealityRequestResolution;

                            webcamManager.useFrontCam = false;
                            webcamManager.OnWebcamTextureReady.AddListener(Action_SetWebcamTexture);

                            MainCam.clearFlags = CameraClearFlags.SolidColor;
                            MainCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                        }
                    }
                    else
                    {
                        if (webcamManager != null)
                        {
                            Destroy(webcamManager);
                            webcamManager = null;
                        }
                    }
#else
                    EnableMixedReality = false;
#endif
                    break;
                case GameViewCaptureMode.RenderCam:
                    if (MatchScreenAspect)
                    {
                        if (Screen.width > Screen.height) renderResolution.y = renderResolution.x / (float)(Screen.width) * (float)(Screen.height);
                        if (Screen.width < Screen.height) renderResolution.x = renderResolution.y / (float)(Screen.height) * (float)(Screen.width);
                        if (sourceDescriptor.vrUsage == VRTextureUsage.TwoEyes) renderResolution.x /= 2f;
                    }
                    break;
                case GameViewCaptureMode.FullScreen:
                    if (!EncodingTexture) renderResolution = new Vector2(Screen.width, Screen.height) * ResolutionScaling;
                    break;
                case GameViewCaptureMode.Desktop:
                    if (MatchScreenAspect)
                    {
                        if (fmDesktopMonitorAspectRatio > 1f) renderResolution.y = renderResolution.x / fmDesktopMonitorAspectRatio;
                        if (fmDesktopMonitorAspectRatio < 1f) renderResolution.x = renderResolution.y * fmDesktopMonitorAspectRatio;
                    }
                    break;
            }

            if (_CaptureMode != GameViewCaptureMode.RenderCam)
            {
                if (RenderCam != null)
                {
                    if (RenderCam.targetTexture != null) RenderCam.targetTexture = null;
                }
            }
        }

        private void CheckResolution()
        {
            if (renderResolution.x < 2) renderResolution.x = 2;
            if (renderResolution.y < 2) renderResolution.y = 2;

            bool IsLinear = (ColorSpace == ColorSpace.Linear) && (CaptureMode == GameViewCaptureMode.FullScreen);

            sourceDescriptor.width = Mathf.RoundToInt(renderResolution.x);
            sourceDescriptor.height = Mathf.RoundToInt(renderResolution.y);
            if (sourceDescriptor.width % 2 != 0) sourceDescriptor.width += 1;
            if (sourceDescriptor.height % 2 != 0) sourceDescriptor.height += 1;
            sourceDescriptor.sRGB = !IsLinear;

            if (PanoramaMode && CaptureMode == GameViewCaptureMode.RenderCam)
            {
                if (rt_cube == null)
                {
                    rt_cube = new RenderTexture((int)CubemapResolution, (int)CubemapResolution, 0, RenderTextureFormat.ARGB32, IsLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
                    //rt_cube.Create();
                }
                else
                {
                    if (rt_cube.width != (int)CubemapResolution || rt_cube.height != (int)CubemapResolution || rt_cube.sRGB != IsLinear)
                    {
                        if (MainCam != null) { if (MainCam.targetTexture == rt_cube) MainCam.targetTexture = null; }
                        if (RenderCam != null) { if (RenderCam.targetTexture == rt_cube) RenderCam.targetTexture = null; }
                        Destroy(rt_cube);
                        rt_cube = new RenderTexture((int)CubemapResolution, (int)CubemapResolution, 0, RenderTextureFormat.ARGB32, IsLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
                        //rt_cube.Create();
                    }
                }

                rt_cube.antiAliasing = 1;
                rt_cube.filterMode = FilterMode.Bilinear;
                rt_cube.anisoLevel = 0;
                rt_cube.dimension = TextureDimension.Cube;
                rt_cube.autoGenerateMips = false;
            }


            if (rt == null)
            {
                //may have unsupport graphic format bug on Unity2019/2018, fallback to not using descriptor
                try { rt = new RenderTexture(sourceDescriptor); }
                catch
                {
                    DestroyImmediate(rt);
                    rt = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                }
                rt.Create();
            }
            else
            {
                if (rt.width != sourceDescriptor.width || rt.height != sourceDescriptor.height || rt.sRGB != IsLinear)
                {
                    if (MainCam != null) { if (MainCam.targetTexture == rt) MainCam.targetTexture = null; }
                    if (RenderCam != null) { if (RenderCam.targetTexture == rt) RenderCam.targetTexture = null; }
                    DestroyImmediate(rt);
                    //may have unsupport graphic format bug on Unity2019/2018, fallback to not using descriptor
                    try { rt = new RenderTexture(sourceDescriptor); }
                    catch
                    {
                        DestroyImmediate(rt);
                        rt = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                    }
                    rt.Create();
                }
            }

            if (CapturedTexture == null) { CapturedTexture = new Texture2D(sourceDescriptor.width, sourceDescriptor.height, TextureFormat.RGB24, false, IsLinear); }
            else
            {
                if (CapturedTexture.width != sourceDescriptor.width || CapturedTexture.height != sourceDescriptor.height)
                {
                    DestroyImmediate(CapturedTexture);
                    CapturedTexture = new Texture2D(sourceDescriptor.width, sourceDescriptor.height, TextureFormat.RGB24, false, IsLinear);
                }
            }
        }

        private void ProcessCapturedTexture()
        {
            if (stoppedOrCancelled())
            {
                EncodingTexture = false;
                return;
            }
            streamWidth = rt.width;
            streamHeight = rt.height;

            if (!FastMode) EnableAsyncGPUReadback = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { ProcessCapturedTextureGPUReadbackAsync(); }
            else { ProcessCapturedTextureAsync(); }
        }

        private async void ProcessCapturedTextureAsync()
        {
            //render texture to texture2d
            RenderTexture.active = rt;
            CapturedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            CapturedTexture.Apply();
            RenderTexture.active = null;

            //encode to byte for streaming
            EncodeBytesAsync();
            await FMCoreTools.AsyncTask.Yield();
        }

        byte[] RawTextureData = new byte[0];
        private NativeArray<byte> asyncGPUReadbackNativeArray;
        private async void ProcessCapturedTextureGPUReadbackAsync()
        {
#if UNITY_2018_2_OR_NEWER
            if (rt != null)
            {
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24);
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

#if FMETP_URP
        private RenderTexture rt_source;
        private RenderTexture currentRenderTexture; // In URP Editor Camera, we have to backup its current render texture, otherwise it will be black screen in scene's view(runtime)
        private async void DelayAddRenderPipelineListenersAsync(int inputDelayMS)
        {
            await FMCoreTools.AsyncTask.Delay(inputDelayMS);
            AddRenderPipelineListeners();
        }

        private void AddRenderPipelineListeners()
        {
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
        }

        private void RemoveRenderPipelineListeners()
        {
            RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_beginCameraRendering;
        }

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            //OnPreRender();
#if UNITY_EDITOR
            //Fixed URP Scene view Editor Camera Bug: try reserving Unity Editor GameView RenderTexture
            currentRenderTexture = RenderTexture.active;
#endif
            if (_CaptureMode != GameViewCaptureMode.MainCam) return;
            if (rt_source == null)
            {
                try { rt_source = new RenderTexture(sourceDescriptor); }
                catch
                {
                    DestroyImmediate(rt_source);
                    rt_source = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                }
                rt_source.Create();
            }
            else
            {
                if (rt_source.width != Screen.width || rt_source.height != Screen.height)
                {
                    if (MainCam != null) { if (MainCam.targetTexture == rt_source) MainCam.targetTexture = null; }
                    DestroyImmediate(rt_source);
                    sourceDescriptor.width = Screen.width;
                    sourceDescriptor.height = Screen.height;
                    renderResolution = new Vector2(Screen.width, Screen.height) * ResolutionScaling;

                    try { rt_source = new RenderTexture(sourceDescriptor); }
                    catch
                    {
                        DestroyImmediate(rt_source);
                        rt_source = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                    }
                    rt_source.Create();
                }
            }

            MainCam.targetTexture = rt_source;
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            //OnPostRender();
            if (_CaptureMode != GameViewCaptureMode.MainCam) return;
            MainCam.targetTexture = null;
            OnRenderImageURP();

#if UNITY_EDITOR
            //Fixed URP Scene view Editor Camera Bug: try setting Unity Editor GameView RenderTexture back
            RenderTexture.active = currentRenderTexture;
#endif
        }

        private void OnRenderImageURP()
        {
            //Graphics.Blit(rt_source, null as RenderTexture);
            if(NeedUpdateTexture && !EncodingTexture)
            {
                NeedUpdateTexture = false;
                CheckResolution();

                if (EnableMixedReality) SetMixedRealityMaterial();
                if (ColorReductionLevel > 0)
                {
                    MatColorAdjustment.SetFloat("_Brightness", brightness);
                    Graphics.Blit(rt_source, rt, MatColorAdjustment);
                }
                else
                {
                    if (EnableMixedReality)
                    {
                        Graphics.Blit(rt_source, rt, MatMixedReality);
                    }
                    else
                    {
                        Graphics.Blit(rt_source, rt);
                    }
                }

                //RenderTexture to Texture2D
                ProcessCapturedTexture();
            }

            Graphics.Blit(rt_source, null as RenderTexture, mat_source);
        }
#else
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_CaptureMode == GameViewCaptureMode.MainCam)
            {
                if (NeedUpdateTexture && !EncodingTexture)
                {
                    NeedUpdateTexture = false;
                    CheckResolution();

                    if (EnableMixedReality) SetMixedRealityMaterial();
                    if (ColorReductionLevel > 0)
                    {
                        MatColorAdjustment.SetFloat("_Brightness", brightness);
                        Graphics.Blit(source, rt, MatColorAdjustment);
                    }
                    else
                    {
                        if (EnableMixedReality)
                        {
                            Graphics.Blit(source, rt, MatMixedReality);
                        }
                        else
                        {
                            Graphics.Blit(source, rt);
                        }
                    }

                    //RenderTexture to Texture2D
                    ProcessCapturedTexture();
                }
            }

            Graphics.Blit(source, destination);
        }
#endif

        private void SetMixedRealityMaterial()
        {
#if !FM_WEBCAM_DISABLED
            if (ColorReductionLevel > 0)
            {
                MatColorAdjustment.SetFloat("_FlipX", MixedRealityFlipX ? 1f : 0f);
                MatColorAdjustment.SetFloat("_FlipY", MixedRealityFlipY ? 1f : 0f);
                MatColorAdjustment.SetFloat("_ScaleX", MixedRealityScaleX);
                MatColorAdjustment.SetFloat("_ScaleY", MixedRealityScaleY);
                MatColorAdjustment.SetFloat("_OffsetX", MixedRealityOffsetX);
                MatColorAdjustment.SetFloat("_OffsetY", MixedRealityOffsetY);
                if (webcamTexture != null) MatColorAdjustment.SetTexture("_WebcamTex", (Texture)webcamTexture);
            }
            else
            {
                MatMixedReality.SetFloat("_FlipX", MixedRealityFlipX ? 1f : 0f);
                MatMixedReality.SetFloat("_FlipY", MixedRealityFlipY ? 1f : 0f);
                MatMixedReality.SetFloat("_ScaleX", MixedRealityScaleX);
                MatMixedReality.SetFloat("_ScaleY", MixedRealityScaleY);
                MatMixedReality.SetFloat("_OffsetX", MixedRealityOffsetX);
                MatMixedReality.SetFloat("_OffsetY", MixedRealityOffsetY);
                if (webcamTexture != null) MatMixedReality.SetTexture("_WebcamTex", (Texture)webcamTexture);
            }
#endif
        }

        private async void RenderTextureRefreshAsync()
        {
            if (NeedUpdateTexture && !EncodingTexture)
            {
                NeedUpdateTexture = false;
                EncodingTexture = true;

#if UNITY_2023_1_OR_NEWER
                await Awaitable.EndOfFrameAsync();
#else
                await WaitForEndOfFrameAsync();
#endif
                CheckResolution();

                if (_CaptureMode == GameViewCaptureMode.RenderCam)
                {
                    if (RenderCam != null)
                    {
                        if (RenderCam.enabled == false) RenderCam.enabled = true;
                        if (PanoramaMode)
                        {
                            RenderCam.targetTexture = rt_cube;
                            RenderCam.RenderToCubemap(rt_cube);

                            Shader.SetGlobalFloat("FORWARD", RenderCam.transform.eulerAngles.y * 0.01745f);
                            MatPano.SetFloat("_Brightness", brightness);
                            Graphics.Blit(rt_cube, rt, MatPano);
                        }
                        else
                        {
                            if (reservedExistingRenderTexture)
                            {
                                RenderCam.targetTexture = rt_reserved;

                                //apply color adjustment for bandwidth
                                if (ColorReductionLevel > 0)
                                {
                                    MatColorAdjustment.SetFloat("_Brightness", brightness);
                                    Graphics.Blit(rt_reserved, rt, MatColorAdjustment);
                                }
                                else { Graphics.Blit(rt_reserved, rt); }
                            }
                            else
                            {
                                RenderCam.targetTexture = rt;
                                RenderCam.Render();
                                RenderCam.targetTexture = null;

                                //apply color adjustment for bandwidth
                                if (ColorReductionLevel > 0)
                                {
                                    MatColorAdjustment.SetFloat("_Brightness", brightness);
                                    Graphics.Blit(rt, rt, MatColorAdjustment);
                                }
                            }

                        }

                        // RenderTexture to Texture2D
                        ProcessCapturedTexture();
                    }
                    else { EncodingTexture = false; }
                }

                if (_CaptureMode == GameViewCaptureMode.FullScreen)
                {
                    if (ResolutionScaling == 1f)
                    {
                        // cleanup
                        if (CapturedTexture != null) Destroy(CapturedTexture);
                        CapturedTexture = ScreenCapture.CaptureScreenshotAsTexture();
                        if (ColorReductionLevel > 0)
                        {
                            MatColorAdjustment.SetFloat("_Brightness", brightness);
                            Graphics.Blit(CapturedTexture, rt, MatColorAdjustment);

                            // RenderTexture to Texture2D
                            ProcessCapturedTexture();
                        }
                        else { EncodeBytesAsync(); }
                    }
                    else
                    {
                        // cleanup
                        if (Screenshot != null) Destroy(Screenshot);
                        Screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                        if (ColorReductionLevel > 0)
                        {
                            MatColorAdjustment.SetFloat("_Brightness", brightness);
                            Graphics.Blit(Screenshot, rt, MatColorAdjustment);
                        }
                        else { Graphics.Blit(Screenshot, rt); }

                        // RenderTexture to Texture2D
                        ProcessCapturedTexture();
                    }
                }

                if (_CaptureMode == GameViewCaptureMode.Desktop)
                {
#if UNITY_EDITOR || UNITY_STANDALONE
                    if (fmDesktopManager == null)
                    {
                        if (FMDesktopManager.instance == null)
                        {
                            fmDesktopManager = this.gameObject.AddComponent<FMDesktopManager>();
                            fmDesktopManager.hideFlags = HideFlags.HideInInspector;
                        }
                        else
                        {
                            fmDesktopManager = FMDesktopManager.instance;
                        }
                        while (!fmDesktopManager.IsCapturing()) await FMCoreTools.AsyncTask.Delay(1);
                    }

                    FMDesktopMonitorCount = fmDesktopManager.MonitorCount;
                    if (FMDesktopMonitorID >= (FMDesktopMonitorCount - 1)) FMDesktopTargetDisplay = (FMDesktopDisplayID)(FMDesktopMonitorCount - 1);

                    int _monitorID = FMDesktopMonitorID;
                    if (fmDesktopManager.AvailableMonitor(_monitorID))
                    {
                        FMMonitor _fmmonitor = fmDesktopManager.FMMonitors[_monitorID];
                        if (!_fmmonitor.FrameTextureReady)
                        {
                            _fmmonitor.RequestUpdate(TextureFormat.RGBA32);
                            fmDesktopManager.TargetTextureFormat = TextureFormat.RGBA32;
                            await FMCoreTools.AsyncTask.Delay(1);
                            //await FMCoreTools.AsyncTask.Yield();
                        }

                        bool _textureReady = false;
                        while (!_textureReady && !stoppedOrCancelled())
                        {
                            if (_monitorID != FMDesktopMonitorID) return;
                            if (fmDesktopManager == null) return;
                            if (!fmDesktopManager.IsCapturing()) return;

                            if (FMDesktopShowCursor)
                            {
                                _textureReady = fmDesktopManager.FMMonitors[_monitorID].FrameTextureReady && fmDesktopManager.CursorTextureReady;
                            }
                            else
                            {
                                _textureReady = fmDesktopManager.FMMonitors[_monitorID].FrameTextureReady;
                            }
                            await FMCoreTools.AsyncTask.Delay(1);
                            //if (!_textureReady && !stoppedOrCancelled()) await FMCoreTools.AsyncTask.Yield();
                        }

                        if (_textureReady)
                        {
                            fmDesktopMonitorAspectRatio = (float)_fmmonitor.FrameWidth / (float)_fmmonitor.FrameHeight;
                            MatFMDesktop.SetFloat("_ShowCursor", FMDesktopShowCursor ? 1f: 0f);
                            if (FMDesktopShowCursor)
                            {
                                MatFMDesktop.SetFloat("_FrameWidth", _fmmonitor.FrameWidth);
                                MatFMDesktop.SetFloat("_FrameHeight", _fmmonitor.FrameHeight);
                                
                                float cursor_ratio = (float)fmDesktopManager.CursorWidth / (float)fmDesktopManager.CursorHeight;
                                float screen_cursor_scaling = 80 / _fmmonitor.Scaling;
                                bool landscapeMode = _fmmonitor.FrameWidth > _fmmonitor.FrameHeight;
                                MatFMDesktop.SetFloat("_CursorWidth", (float)(landscapeMode ? _fmmonitor.FrameWidth : _fmmonitor.FrameHeight) / screen_cursor_scaling);
                                MatFMDesktop.SetFloat("_CursorHeight", ((float)(landscapeMode ? _fmmonitor.FrameWidth : _fmmonitor.FrameHeight) / screen_cursor_scaling) / cursor_ratio);

                                MatFMDesktop.SetFloat("_MonitorScaling", _fmmonitor.Scaling);

                                float cursorPointX = (fmDesktopManager.CursorPoint.Position.x - _fmmonitor.MonitorOffsetX) * _fmmonitor.Scaling;
                                float cursorPointY = (fmDesktopManager.CursorPoint.Position.y - _fmmonitor.MonitorOffsetY) * _fmmonitor.Scaling;
                                cursorPointX -= fmDesktopManager.CursorPoint.HotSpot.x * _fmmonitor.Scaling;
                                cursorPointY -= fmDesktopManager.CursorPoint.HotSpot.y * _fmmonitor.Scaling;
                                MatFMDesktop.SetFloat("_CursorPointX", cursorPointX);
                                MatFMDesktop.SetFloat("_CursorPointY", cursorPointY);

                                MatFMDesktop.SetTexture("_CursorTex", fmDesktopManager.TextureCursor);
                            }

                            MatFMDesktop.SetFloat("_FlipX", FMDesktopFlipX ? 0f : 1f);
                            MatFMDesktop.SetFloat("_FlipY", FMDesktopFlipY ? 0f : 1f);
                            MatFMDesktop.SetFloat("_RangeX", FMDesktopRangeX);
                            MatFMDesktop.SetFloat("_RangeY", FMDesktopRangeY);
                            MatFMDesktop.SetFloat("_OffsetX", FMDesktopOffsetX);
                            MatFMDesktop.SetFloat("_OffsetY", FMDesktopOffsetY);
                            MatFMDesktop.SetFloat("_RotationAngle", (float)FMDesktopRotationAngle);

                            MatFMDesktop.SetTexture("_MainTex", _fmmonitor.TextureFrame);

                            MatFMDesktop.SetFloat("_Brightness", brightness);
                            Graphics.Blit(_fmmonitor.TextureFrame, rt, MatFMDesktop);

                            //fmDesktopFrameWidth = (Int16)(_fmmonitor.FrameWidth / _fmmonitor.Scaling);
                            //fmDesktopFrameHeight = (Int16)(_fmmonitor.FrameHeight / _fmmonitor.Scaling);
                            fmDesktopFrameWidth = (Int16)_fmmonitor.FrameWidth;
                            fmDesktopFrameHeight = (Int16)_fmmonitor.FrameHeight;
                            fmDesktopFrameStride = (Int16)_fmmonitor.FrameStride;
                            fmDesktopMonitorOffsetX = (Int16)_fmmonitor.MonitorOffsetX;
                            fmDesktopMonitorOffsetY = (Int16)_fmmonitor.MonitorOffsetY;
                            fmDesktopMonitorScaling = (Int16)_fmmonitor.Scaling;

                            //request for next frame
                            _fmmonitor.RequestUpdate(TextureFormat.RGBA32);
                            //fmDesktopManager.TargetTextureFormat = TextureFormat.RGBA32;
                            fmDesktopManager.TargetTextureFormat = FMDesktopTextureFormat;

                            //RenderTexture to Texture2D
                            ProcessCapturedTexture();
                        }
                        else { EncodingTexture = false; }
                    }
                    else { EncodingTexture = false; }
#else
                    EncodingTexture = false;
#endif
                }
            }
        }

        public void Action_UpdateTexture() { RequestTextureUpdate(); }

        private void RequestTextureUpdate()
        {
            if (EncodingTexture) return;
            NeedUpdateTexture = true;
            if (_CaptureMode != GameViewCaptureMode.MainCam) RenderTextureRefreshAsync();
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

        private byte[][] outputSplitBytes(byte[] rawPixels, int width, int height, int splitChunks = 2)
        {
            if (width % splitChunks != 0) return null;

            int splitWidth = width / splitChunks;
            int imageSize = width * height * 3; // Assuming RGB format

            byte[][] splitPixels = new byte[splitChunks][];
            for (int i = 0; i < splitPixels.Length; i++) splitPixels[i] = new byte[imageSize / splitChunks];
            

            // Copy pixels to left and right arrays
            for (int y = 0; y < height; y++)
            {
                for (int i = 0; i < splitPixels.Length; i++)
                {
                    Buffer.BlockCopy(rawPixels, y * width * 3 + (i * splitWidth * 3), splitPixels[i], y * splitWidth * 3, splitWidth * 3);
                }
            }

            return splitPixels;
        }

        private async void EncodeBytesAsync(bool assignedRawTexture = false)
        {
            bool _canRefreshPreviewImage = false;
            if (!assignedRawTexture)
            {
                FMCoreTools.NativeArrayCopyTo(CapturedTexture.GetRawTextureData<byte>(), ref RawTextureData);
                streamWidth = CapturedTexture.width;
                streamHeight = CapturedTexture.height;
            }

            if (OutputFormat == GameViewOutputFormat.RAWRGB24)
            {
                OnRawRGB24ReadyEvent.Invoke(RawTextureData);
            }
            else if (CapturedTexture != null || RawTextureData != null)
            {
                //==================getting byte data==================
                if (OutputFormat != GameViewOutputFormat.FMMJPEG) GZipMode = FMGZipEncodeMode.None;

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
                    dataByteTemp = dataByte.ToArray();
                    EncodingTexture = false;
                    //==================getting byte data==================
                    int _length = dataByteTemp.Length;
                    dataLength = _length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    byte[] _meta_monitorOffsetX = BitConverter.GetBytes(FMDesktopStreamMonitorOffsetX);
                    byte[] _meta_monitorOffsetY = BitConverter.GetBytes(FMDesktopStreamMonitorOffsetY);
                    byte[] _meta_frameWidth = BitConverter.GetBytes(FMDesktopStreamFrameWidth);
                    byte[] _meta_frameHeight = BitConverter.GetBytes(FMDesktopStreamFrameHeight);

                    //for mac retina x2, otherwise the default value should be 1 on other platforms
                    byte _meta_monitorScaling = (byte)FMDesktopStreamMonitorScaling;
                    byte _meta_OS = (byte)fmDesktopSystemOS;

                    int _metaByteLength = CaptureMode == GameViewCaptureMode.Desktop ? 24 : 15;
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
                            SendByte[13] = (byte)ColorReductionLevel;

                            //for desktop, if it's desktop capture mode, use monitor scaling byte(usually 1, or 2 for retina display)
                            SendByte[14] = (byte)(CaptureMode == GameViewCaptureMode.Desktop ? _meta_monitorScaling : 0);
                            if (CaptureMode == GameViewCaptureMode.Desktop)
                            {
                                SendByte[15] = _meta_OS;
                                Buffer.BlockCopy(_meta_monitorOffsetX, 0, SendByte, 16, 2);
                                Buffer.BlockCopy(_meta_monitorOffsetY, 0, SendByte, 18, 2);
                                Buffer.BlockCopy(_meta_frameWidth, 0, SendByte, 20, 2);
                                Buffer.BlockCopy(_meta_frameHeight, 0, SendByte, 22, 2);
                            }

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
                    _canRefreshPreviewImage = true;
                }
            }

            EncodingTexture = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback)
            {
                //dispose and release memory from AsyncGPUReadback
                if (asyncGPUReadbackNativeArray.IsCreated) asyncGPUReadbackNativeArray.Dispose();
            }

            if (_canRefreshPreviewImage)
            {
                //refresh preview image on URP
#if FMETP_URP
                if (supportsAsyncGPUReadback && EnableAsyncGPUReadback && PreviewType != GameViewPreviewType.None)
                {
                    CapturedTexture.LoadRawTextureData(RawTextureData);
                    CapturedTexture.Apply();
                }
#endif
                switch (PreviewType)
                {
                    case GameViewPreviewType.None: break;
                    case GameViewPreviewType.RawImage: PreviewRawImage.texture = GetStreamTexture; break;
                    case GameViewPreviewType.MeshRenderer: PreviewMeshRenderer.material.mainTexture = GetStreamTexture; break;
                }
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

#if FMETP_URP
            RemoveRenderPipelineListeners();
#endif
        }

        private bool initialised = false;
        private void StartAll()
        {
            cancellationTokenSource_global = new CancellationTokenSource();

            if (initialised) return;
            initialised = true;

            CheckRenderResolution();
            CheckFastModeSupport();
#if FMETP_URP
            DelayAddRenderPipelineListenersAsync(2000);
#endif
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