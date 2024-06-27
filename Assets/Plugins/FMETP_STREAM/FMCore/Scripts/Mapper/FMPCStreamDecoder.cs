using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

using System;
using System.Threading.Tasks;
using System.Threading;
using FMSolution.FMZip;
using Unity.Collections;

namespace FMSolution.FMETP
{
    [AddComponentMenu("FMETP/Mapper/FMPCStreamDecoder")]
    public class FMPCStreamDecoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowSettings = true;
        public bool EditorShowDecoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public int PCWidth = 0;
        public int PCHeight = 0;
        public int PCCount = 0;

        public Color MainColor = Color.white;
        [Range(0.000001f, 100f)]
        public float PointSize = 0.04f;
        public bool ApplyDistance = true;

        public Mesh PMesh;
        public Material MatFMPCStreamDecoder;

        //init when added component, or reset component
        void Reset() { MatFMPCStreamDecoder = new Material(Shader.Find("Hidden/FMPCStreamDecoder")); }

        public bool FastMode = true;
        public bool AsyncMode = true;

        public Texture2D ReceivedTexture2D;

        private void Start()
        {
            Application.runInBackground = true;
            this.gameObject.AddComponent<MeshRenderer>().hideFlags = HideFlags.HideInInspector;
            this.gameObject.AddComponent<MeshFilter>().hideFlags = HideFlags.HideInInspector;
        }

        private void Update()
        {
            if (PCCount > 0)
            {
                MatFMPCStreamDecoder.color = MainColor;
                MatFMPCStreamDecoder.SetFloat("_PointSize", PointSize);
                MatFMPCStreamDecoder.SetFloat("_ApplyDistance", ApplyDistance ? 1f : 0f);
            }
        }

        private bool ReadyToGetFrame = true;

        //[Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 4001;
        private UInt16 dataID = 0;
        //UInt16 maxID = 1024;
        private int dataLength = 0;
        private int receivedLength = 0;

        private byte[] dataByte;
        private bool isGZipped = false;
        public FMGZipDecodeMode GZipMode = FMGZipDecodeMode.HighPerformance;

        public void Action_ProcessPointCloudData(byte[] _byteData)
        {
            if (!enabled) return;
            if (_byteData.Length <= 8) return;

            UInt16 _label = BitConverter.ToUInt16(_byteData, 0);
            if (_label != label) return;
            UInt16 _dataID = BitConverter.ToUInt16(_byteData, 2);

            if (_dataID != dataID) receivedLength = 0;
            dataID = _dataID;
            dataLength = BitConverter.ToInt32(_byteData, 4);
            int _offset = BitConverter.ToInt32(_byteData, 8);

            isGZipped = _byteData[12] == 1;

            if (receivedLength == 0) dataByte = new byte[dataLength];
            int chunkLength = _byteData.Length - 14;
            if (_offset + chunkLength <= dataByte.Length)
            {
                receivedLength += chunkLength;
                Buffer.BlockCopy(_byteData, 14, dataByte, _offset, chunkLength);
            }

            if (ReadyToGetFrame)
            {
                if (receivedLength == dataLength)
                {
                    if (this.isActiveAndEnabled) ProcessImageDataAsync(dataByte);
                }
            }
        }


        private float camNearClipPlane = 0f;
        private float camFarClipPlane = 0f;
        private float camFOV = 60f;
        private float camAspect = 1f;
        private float camOrthographicProjection = 0f;
        private float camOrthographicSize = 1f;

        private async void ProcessImageDataAsync(byte[] _byteData)
        {
            ReadyToGetFrame = false;

            //read camera meta data
            camNearClipPlane = BitConverter.ToSingle(_byteData, 0);
            camFarClipPlane = BitConverter.ToSingle(_byteData, 4);
            camFOV = BitConverter.ToSingle(_byteData, 8);
            camAspect = BitConverter.ToSingle(_byteData, 12);
            camOrthographicProjection = BitConverter.ToSingle(_byteData, 16);
            camOrthographicSize = BitConverter.ToSingle(_byteData, 20);

            byte[] inputByteData = new byte[_byteData.Length - 24];
            Buffer.BlockCopy(_byteData, 24, inputByteData, 0, inputByteData.Length);

            if (ReceivedTexture2D == null) { ReceivedTexture2D = new Texture2D(1, 1, TextureFormat.RGB24, false); }
            else
            {
                if (ReceivedTexture2D.format != TextureFormat.RGB24)
                {
                    DestroyAsyncObject(ReceivedTexture2D);
                    ReceivedTexture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                }
            }

#if UNITY_IOS && !UNITY_EDITOR
            FastMode = true;
#endif

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN || UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            //supported fast mode
#else
            //not supported fast mode
            FastMode = false;
#endif
            if (isGZipped)
            {
                inputByteData = await FMZipHelper.FMUnzippedByteAsync(inputByteData, cancellationTokenSource_global.Token, GZipMode);
                if (inputByteData == null)
                {
                    ReadyToGetFrame = true;
                    return;
                }
            }

            if (FastMode)
            {
                //try AsyncMode, on supported platform
                if (AsyncMode)
                {
                    //has spare thread
                    byte[] RawTextureData = new byte[1];
                    int _width = 0;
                    int _height = 0;

                    //need to clone a buffer for multi-threading
                    byte[] _bufferByte = new byte[inputByteData.Length];
                    Buffer.BlockCopy(inputByteData, 0, _bufferByte, 0, inputByteData.Length);

                    //new method via async
                    await Task.Run(() => { try { inputByteData.FMJPGToRawTextureData(ref RawTextureData, ref _width, ref _height, TextureFormat.RGB24); } catch { } });

                    if (RawTextureData.Length <= 8)
                    {
                        ReadyToGetFrame = true;
                        return;
                    }

                    try
                    {
                        //check resolution
                        ReceivedTexture2D.FMMatchResolution(ref ReceivedTexture2D, _width, _height);
                        ReceivedTexture2D.LoadRawTextureData(RawTextureData);
                        ReceivedTexture2D.Apply();
                    }
                    catch
                    {
                        DestroyAsyncObject(ReceivedTexture2D);

                        ReadyToGetFrame = true;
                        return;
                    }
                }
                else
                {
                    //no spare thread, run in main thread
                    try { ReceivedTexture2D.FMLoadJPG(ref ReceivedTexture2D, inputByteData); }
                    catch
                    {
                        DestroyAsyncObject(ReceivedTexture2D);

                        ReadyToGetFrame = true;
                        return;
                    }
                }
            }
            else
            {
                try { ReceivedTexture2D.LoadImage(inputByteData); }
                catch
                {
                    DestroyAsyncObject(ReceivedTexture2D);

                    ReadyToGetFrame = true;
                    return;
                }
            }

            if (ReceivedTexture2D.width <= 8)
            {
                //throw new Exception("texture is smaller than 8 x 8, wrong data");
                Debug.LogError("texture is smaller than 8 x 8, wrong data");
                ReadyToGetFrame = true;
                return;
            }

            ReceivedTexture2D.filterMode = FilterMode.Point;
            Action_ProcessImage(ReceivedTexture2D);

            ReadyToGetFrame = true;
        }

        private CancellationTokenSource cancellationTokenSource_global;
        private void OnEnable() { InitAsyncTokenSource(); }
        private void OnDisable() { StopAllAsync(); }
        private bool stoppedOrCancelled() { return cancellationTokenSource_global.IsCancellationRequested; }
        private void InitAsyncTokenSource() { cancellationTokenSource_global = new CancellationTokenSource(); }
        private void StopAllAsync()
        {
            if (cancellationTokenSource_global != null)
            {
                if (!cancellationTokenSource_global.IsCancellationRequested) cancellationTokenSource_global.Cancel();
            }
        }
        private void DestroyAsyncObject(UnityEngine.Object inputObject)
        {
            if (cancellationTokenSource_global != null)
            {
                if (cancellationTokenSource_global.IsCancellationRequested)
                {
                    DestroyImmediate(inputObject);
                    GC.Collect();
                    return;
                }
            }

            Destroy(inputObject);
            GC.Collect();
        }

        private Vector3[] pcVertices = new Vector3[0];
        private int[] pcIndices = new int[0];
        public void Action_ProcessImage(Texture2D inputTexture)
        {
            if (inputTexture.width != PCWidth || inputTexture.height != PCHeight)
            {
                PCWidth = inputTexture.width / 2;
                PCHeight = inputTexture.height;
                PCCount = PCWidth * PCHeight;

                //if (PMesh != null) DestroyImmediate(PMesh);
                if (PMesh == null) PMesh = new Mesh();
                PMesh.name = "PMesh_" + PCWidth + "x" + PCHeight;

                PMesh.indexFormat = PCCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

                if (pcVertices.Length != PCCount)
                {
                    pcVertices = new Vector3[PCCount];
                    for (int j = 0; j < PCHeight; j++)
                    {
                        for (int i = 0; i < PCWidth; i++)
                        {
                            int index = (j * PCWidth) + i;
                            pcVertices[index].x = ((float)i / (float)PCWidth);
                            pcVertices[index].y = ((float)j / (float)PCHeight);
                            pcVertices[index].z = 0f;
                        }
                    }

                    PMesh.vertices = pcVertices;

                    if (pcIndices.Length != pcVertices.Length) pcIndices = Enumerable.Range(0, PMesh.vertices.Length).ToArray();
                    PMesh.SetIndices(pcIndices, MeshTopology.Points, 0);
                    PMesh.UploadMeshData(false);

                    PMesh.bounds = new Bounds(Vector3.zero, new Vector3(2, 2, 2) * camFarClipPlane);
                    GetComponent<MeshFilter>().sharedMesh = PMesh;
                    GetComponent<MeshRenderer>().sharedMaterial = MatFMPCStreamDecoder;
                }
            }

            if (MatFMPCStreamDecoder != null) MatFMPCStreamDecoder.mainTexture = inputTexture;

            MatFMPCStreamDecoder.SetFloat("_NearClipPlane", camNearClipPlane);
            MatFMPCStreamDecoder.SetFloat("_FarClipPlane", camFarClipPlane);

            MatFMPCStreamDecoder.SetFloat("_VerticalFOV", camFOV);
            MatFMPCStreamDecoder.SetFloat("_Aspect", camAspect);

            MatFMPCStreamDecoder.SetFloat("_OrthographicProjection", camOrthographicProjection);
            MatFMPCStreamDecoder.SetFloat("_OrthographicSize", camOrthographicSize);
        }
    }
}