using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using FMSolution.FMZip;
using System.Runtime.InteropServices;

namespace FMSolution.FMETP
{
    public enum AudioDecoderSourceFormat { Auto, Manual }
    [RequireComponent(typeof(AudioSource)), AddComponentMenu("FMETP/Mapper/AudioDecoder")]
    public class AudioDecoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowSettings = true;
        public bool EditorShowPlayback = true;
        public bool EditorShowAudioInfo = true;
        public bool EditorShowDecoded = true;
        public bool EditorShowPairing = true;
        #endregion

        // Use this for initialization
        private void Start()
        {
            Application.runInBackground = true;
            DeviceSampleRate = AudioSettings.GetConfiguration().sampleRate;

            if (Audio == null) Audio = GetComponent<AudioSource>();
            Audio.volume = volume;
        }

        private bool ReadyToGetFrame = true;
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 2001;
        private UInt16 dataID = 0;
        private int dataLength = 0;
        private int receivedLength = 0;

        private byte[] dataByte;
        private bool isGZipped = false;
        public FMGZipDecodeMode GZipMode = FMGZipDecodeMode.HighPerformance;
        private AudioOutputFormat inputFormat = AudioOutputFormat.FMPCM16;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;
        public float Volume
        {
            get { return volume; }
            set
            {
                volume = Mathf.Clamp(value, 0f, 1f);
                if (Audio == null) Audio = GetComponent<AudioSource>();
                Audio.volume = volume;
            }
        }

        public AudioDecoderSourceFormat SourceFormatDection = AudioDecoderSourceFormat.Auto;
        [Range(1,8)]
        public int SourceChannels = 1;
        public int SourceSampleRate = 48000;
        public int DeviceSampleRate = 48000;

        private ConcurrentQueue<float> ABufferQueue = new ConcurrentQueue<float>();
        public UnityEventFloatArray OnPCMFloatReadyEvent = new UnityEventFloatArray();

        /// <summary>
        /// By default, this function equals to Action_ProcessFMPCM16Data(byte[]);
        /// </summary>
        public void Action_ProcessData(byte[] _byteData) { Action_ProcessFMPCM16Data(_byteData); }

        /// <summary>
        /// Require input byte[] of FMPCM16 streams, which includes all meta data of sample rate, channels
        /// </summary>
        public void Action_ProcessFMPCM16Data(byte[] _byteData)
        {
            if (!enabled) return;
            if (_byteData.Length <= 14) return;

            UInt16 _label = BitConverter.ToUInt16(_byteData, 0);
            if (_label != label) return;
            UInt16 _dataID = BitConverter.ToUInt16(_byteData, 2);

            if (_dataID != dataID) receivedLength = 0;
            dataID = _dataID;
            dataLength = BitConverter.ToInt32(_byteData, 4);
            int _offset = BitConverter.ToInt32(_byteData, 8);

            isGZipped = _byteData[12] == 1;
            inputFormat = (AudioOutputFormat)_byteData[13];

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
                    if (this.isActiveAndEnabled) ProcessAudioDataFMPCM16Async(dataByte);
                }
            }
        }

        /// <summary>
        /// Require input byte[] of Raw PCM16 streams, but you have to set the Playback Samplate Rate, Channels manually
        /// </summary>
        public void Action_ProcessPCM16Data(byte[] _byteData)
        {
            if (!enabled) return;
            if (_byteData.Length <= 18) return;

            if (ReadyToGetFrame)
            {
                if (this.isActiveAndEnabled) ProcessAudioDataPCM16Async(_byteData);
            }
        }

        private async Task ProcessAudioDataPCM16Task(byte[] inputByteData)
        {
            float[] ABuffer = FMCoreTools.ToFloatArray(inputByteData);

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
            for (int i = 0; i < ABuffer.Length; i++) ABufferQueue.Enqueue(ABuffer[i]);
            CreateClip();
#else
            CreateClipWeb(inputByteData);
#endif
            await Task.Yield();
            OnPCMFloatReadyEvent.Invoke(ABuffer);
        }

        private async void ProcessAudioDataPCM16Async(byte[] inputByteData)
        {
            //if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                ReadyToGetFrame = false;
                await ProcessAudioDataPCM16Task(inputByteData);
                ReadyToGetFrame = true;
            }
        }

        private async void ProcessAudioDataFMPCM16Async(byte[] inputByteData)
        {
            //if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                ReadyToGetFrame = false;
                if (isGZipped)
                {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
                    inputByteData = await FMZipHelper.FMUnzippedByteAsync(inputByteData, cancellationTokenSource_global.Token, GZipMode);
#else

                    inputByteData = await FMZipHelper.FMUnzippedByteAsyncWeb(inputByteData, cancellationTokenSource_global.Token);
#endif
                    if (inputByteData == null)
                    {
                        ReadyToGetFrame = true;
                        return;
                    }
                }

                if (inputByteData.Length >= 8 + 1024)
                {
                    byte[] _sampleRateByte = new byte[4];
                    byte[] _channelsByte = new byte[4];
                    byte[] _audioByte = new byte[1];

                    _audioByte = new byte[inputByteData.Length - 8];
                    Buffer.BlockCopy(inputByteData, 0, _sampleRateByte, 0, _sampleRateByte.Length);
                    Buffer.BlockCopy(inputByteData, 4, _channelsByte, 0, _channelsByte.Length);
                    Buffer.BlockCopy(inputByteData, 8, _audioByte, 0, _audioByte.Length);

                    SourceSampleRate = BitConverter.ToInt32(_sampleRateByte, 0);
                    SourceChannels = BitConverter.ToInt32(_channelsByte, 0);

                    //simplify this function...
                    await ProcessAudioDataPCM16Task(_audioByte);
                }
                ReadyToGetFrame = true;
            }
        }

        private int position = 0;
        private int samplerate = 44100;
        private int channel = 2;

        private AudioClip audioClip;
        private AudioSource Audio;
        private void CreateClip()
        {
            if (samplerate != SourceSampleRate || channel != SourceChannels)
            {
                samplerate = SourceSampleRate;
                channel = SourceChannels;

                if (Audio != null) Audio.Stop();
                if (audioClip != null) DestroyImmediate(audioClip);

                audioClip = AudioClip.Create("StreamingAudio", samplerate * SourceChannels, SourceChannels, samplerate, true, OnAudioRead, OnAudioSetPosition);
                Audio = GetComponent<AudioSource>();
                Audio.clip = audioClip;
                Audio.loop = true;
                Audio.Play();
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
        private void CreateClipWeb(byte[] inputByteData)
        {
            if (samplerate != SourceSampleRate || channel != SourceChannels)
            {
                samplerate = SourceSampleRate;
                channel = SourceChannels;
            }

            initialiseMicrophoneJavascript();
            if (SourceSampleRate != 0 && SourceSampleRate != 0)
            {
                StreamAudio(SourceChannels, inputByteData.Length / 2, SourceSampleRate, inputByteData, inputByteData.Length);
            }
        }

        [DllImport("__Internal")] private static extern void StreamAudio(int NUM_CHANNELS, int NUM_SAMPLES, int SAMPLE_RATE, byte[] PCM16_BYTES, int PCM16_BYTES_SIZE);
        [DllImport("__Internal")] private static extern void FMCoreTools_WebGLAddScript_2021_2(string _innerHTML, string _src);
        private static string initialisedJavascript = null;
        private static void initialiseMicrophoneJavascript()
        {
            if (string.IsNullOrEmpty(initialisedJavascript))
            {
                string javascript = ""
                    + $@"var startTime = 0;" + "\n"
                    + $@"var audioCtx = new AudioContext();" + "\n"
                    ;

                initialisedJavascript = javascript;
                FMCoreTools_WebGLAddScript_2021_2(javascript, "");
                Debug.Log(javascript);
            }
        }
#endif

        private void OnAudioRead(float[] data)
        {
            int count = 0;
            while (count < data.Length)
            {
                if (ABufferQueue.Count > 0) { ABufferQueue.TryDequeue(out data[count]); }
                else { data[count] = 0f; }

                position++;
                count++;
            }
        }

        private void OnAudioSetPosition(int newPosition) { position = newPosition; }

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
    }
}