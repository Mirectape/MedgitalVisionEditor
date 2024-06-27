using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using FMSolution.FMZip;

namespace FMSolution.FMETP
{
    [Serializable]
    public enum MicDeviceMode { Default, TargetDevice }
    [AddComponentMenu("FMETP/Mapper/MicEncoder")]
    public class MicEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowCapture = true;
        public bool EditorShowAudioInfo = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public AudioOutputFormat OutputFormat = AudioOutputFormat.FMPCM16;
        public bool OutputAsChunks = true;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawPCM16ReadyEvent = new UnityEventByteArray();


        private Queue<byte[]> AppendQueueSendByteFMPCM16 = new Queue<byte[]>();
        private Queue<byte[]> AppendQueueSendBytePCM16 = new Queue<byte[]>();

        //[Header("[Capture In-Game Sound]")]
        public bool StreamGameSound = true;
        public int OutputSampleRate = 11025;
        [Range(1, 8)]
        public int OutputChannels = 1;

        //----------------------------------------------
        private AudioSource AudioMic;
        private ConcurrentQueue<byte> AudioBytes = new ConcurrentQueue<byte>();

        public MicDeviceMode DeviceMode = MicDeviceMode.Default;
        public string TargetDeviceName = "MacBook Pro FMMicrophone";
        string CurrentDeviceName = null;

        [TextArea]
        public string DetectedDevices;

        ////[Header("[Capture In-Game Sound]")]
        //public bool StreamGameSound = true;
        //public int OutputSampleRate = 11025;
        //[Range(1, 8)]
        //public int OutputChannels = 1;

        private int CurrentAudioTimeSample = 0;
        private int LastAudioTimeSample = 0;
        //----------------------------------------------

        [Range(1f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        public bool UseHalf = true;
        public FMGZipEncodeMode GZipMode = FMGZipEncodeMode.None;

        //[Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 2001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int maximumChunkSize = Int32.MaxValue - 1024;
        public int OutputChunkSize = 1436;//8096; //32768
        public int GetChunkSize() { return OutputAsChunks ? OutputChunkSize : maximumChunkSize; }

        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;

        public int dataLength;

#if !UNITY_WEBGL || UNITY_EDITOR

        // Use this for initialization
        private void Start()
        {
            StartAll();
        }

        private async void CaptureMicAsync()
        {
            if (AudioMic == null) AudioMic = GetComponent<AudioSource>();
            if (AudioMic == null) AudioMic = gameObject.AddComponent<AudioSource>();

            //Check Target Device
            DetectedDevices = "";
            string[] MicNames = FMMicrophone.devices;
            foreach (string _name in MicNames) DetectedDevices += _name + "\n";
            if (DeviceMode == MicDeviceMode.TargetDevice)
            {
                bool IsCorrectName = false;
                for (int i = 0; i < MicNames.Length; i++)
                {
                    if (MicNames[i] == TargetDeviceName)
                    {
                        IsCorrectName = true;
                        break;
                    }
                }
                if (!IsCorrectName) TargetDeviceName = null;
            }
            //Check Target Device

            CurrentDeviceName = DeviceMode == MicDeviceMode.Default ? MicNames[0] : TargetDeviceName;
            AudioMic.clip = FMMicrophone.Start(CurrentDeviceName, true, 1, OutputSampleRate);
            AudioMic.loop = true;
            while (FMMicrophone.GetPosition(CurrentDeviceName) <= 0) await FMCoreTools.AsyncTask.Yield();
            Debug.Log(CurrentDeviceName + " Start Mic(pos): " + FMMicrophone.GetPosition(CurrentDeviceName));
            AudioMic.Play();

            AudioMic.volume = 0f;

            OutputChannels = AudioMic.clip.channels;

            while (!stoppedOrCancelled())
            {
                AddMicData();
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();
            }
        }

        private void AddMicData()
        {
            LastAudioTimeSample = CurrentAudioTimeSample;
            //CurrentAudioTimeSample = AudioMic.timeSamples;
            CurrentAudioTimeSample = FMMicrophone.GetPosition(CurrentDeviceName);

            if (CurrentAudioTimeSample != LastAudioTimeSample)
            {
                float[] samples = new float[AudioMic.clip.samples];
                AudioMic.clip.GetData(samples, 0);

                if (CurrentAudioTimeSample > LastAudioTimeSample)
                {
                    for (int i = LastAudioTimeSample; i < CurrentAudioTimeSample; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                }
                else if (CurrentAudioTimeSample < LastAudioTimeSample)
                {
                    for (int i = LastAudioTimeSample; i < samples.Length; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                    for (int i = 0; i < CurrentAudioTimeSample; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                }
            }
        }

#else

#endif
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

        public void OnReceivedRawPCM16Data(byte[] byteData)
        {
            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
        }

        int webMicID = -1;
        private bool initialised = false;
        private void StartAll()
        {
            cancellationTokenSource_global = new CancellationTokenSource();

            if (initialised) return;
            initialised = true;

            stop = false;
            SenderAsync();

#if !UNITY_WEBGL || UNITY_EDITOR
            CaptureMicAsync();
#else
            webMicID = FMMicrophone.StartFMMicrophoneWebGL(OutputFormat, OutputSampleRate, OutputChannels, OnReceivedRawPCM16Data);
#endif
        }

        private void StopAll()
        {
            initialised = false;
            stop = true;
            StopAllAsync();


#if !UNITY_WEBGL || UNITY_EDITOR
            if (AudioMic != null)
            {
                AudioMic.Stop();
                FMMicrophone.End(CurrentDeviceName);
            }
#else
            FMMicrophone.StopWebGL(webMicID);
#endif
            AppendQueueSendByteFMPCM16.Clear();
            AppendQueueSendBytePCM16.Clear();
        }

        private async void InvokeEventsCheckerAsync()
        {
            while (!stoppedOrCancelled())
            {
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();
                while (AppendQueueSendByteFMPCM16.Count > 0) OnDataByteReadyEvent.Invoke(AppendQueueSendByteFMPCM16.Dequeue());
                while (AppendQueueSendBytePCM16.Count > 0) OnRawPCM16ReadyEvent.Invoke(AppendQueueSendBytePCM16.Dequeue());
            }
        }
        private async void SenderAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GZipMode = FMGZipEncodeMode.None;//WebGL doesn't support GZip
#endif
            InvokeEventsCheckerAsync();
            while (!stoppedOrCancelled())
            {
                if (Time.realtimeSinceStartup > next)
                {
                    interval = 1f / StreamFPS;
                    next = Time.realtimeSinceStartup + interval;

                    //==================getting byte data==================
                    if (OutputFormat == AudioOutputFormat.FMPCM16)
                    {
                        byte[] _samplerateByte = BitConverter.GetBytes(OutputSampleRate);
                        byte[] _channelsByte = BitConverter.GetBytes(OutputChannels);

                        dataByte = new byte[AudioBytes.Count + _samplerateByte.Length + _channelsByte.Length];

                        Buffer.BlockCopy(_samplerateByte, 0, dataByte, 0, _samplerateByte.Length);
                        Buffer.BlockCopy(_channelsByte, 0, dataByte, 4, _channelsByte.Length);
                        Buffer.BlockCopy(AudioBytes.ToArray(), 0, dataByte, 8, AudioBytes.Count);
                    }
                    else
                    {
                        dataByte = new byte[AudioBytes.Count];
                        Buffer.BlockCopy(AudioBytes.ToArray(), 0, dataByte, 0, AudioBytes.Count);
                        GZipMode = FMGZipEncodeMode.None;
                    }

                    AudioBytes = new ConcurrentQueue<byte>();
                    if (GZipMode != FMGZipEncodeMode.None) dataByte = await FMZipHelper.FMZippedByteAsync(dataByte, cancellationTokenSource_global.Token, GZipMode);

                    dataByteTemp = dataByte.ToArray();
                    //==================getting byte data==================
                    int _length = dataByteTemp.Length;
                    dataLength = _length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    int _metaByteLength = 14;
                    int _chunkSize = GetChunkSize();
                    if (OutputFormat == AudioOutputFormat.FMPCM16) _chunkSize -= _metaByteLength;
                    int _chunkCount = Mathf.CeilToInt((float)_length / (float)_chunkSize);
                    for (int i = 1; i <= _chunkCount; i++)
                    {
                        int dataByteLength = (i == _chunkCount) ? (_length % _chunkSize) : (_chunkSize);
                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                        {
                            byte[] _meta_offset = BitConverter.GetBytes(_offset);
                            byte[] SendByte = new byte[dataByteLength + _metaByteLength];

                            Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                            Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                            Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);
                            Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                            SendByte[12] = (byte)(GZipMode != FMGZipEncodeMode.None ? 1 : 0);
                            SendByte[13] = (byte)0;//not used, but just keep one empty byte for standard

                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 14, dataByteLength);
                            AppendQueueSendByteFMPCM16.Enqueue(SendByte);
                        }
                        else
                        {
                            byte[] SendByte = new byte[dataByteLength];
                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 0, dataByteLength);

                            if (!BitConverter.IsLittleEndian) Array.Reverse(SendByte);
                            AppendQueueSendBytePCM16.Enqueue(SendByte);
                        }
                        _offset += _chunkSize;
                    }

                    dataID++;
                    if (dataID > maxID) dataID = 0;
                }
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();
            }
        }
    }
}