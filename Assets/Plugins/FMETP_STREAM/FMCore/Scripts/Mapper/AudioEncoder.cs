using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FMSolution.FMZip;

namespace FMSolution.FMETP
{
    public enum AudioOutputFormat
    {
        FMPCM16 = (byte)0,
        PCM16 = (byte)1,
        //FMADPCM = (byte)2,
        //ADPCM = (byte)3
    }
    public enum AudioReadMethod { OnAudioFilterRead, AudioListenerGetOutputData, DSPTimeGetOutputData }

    [AddComponentMenu("FMETP/Mapper/AudioEncoder")]
    public class AudioEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowCapture = true;
        public bool EditorShowAudioInfo = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        //----------------------------------------------
        private AudioListener[] AudioListenerObject;
        private bool isReadingOrEncodingAudioBuffer = false;

        private ConcurrentQueue<byte> AudioBytes = new ConcurrentQueue<byte>();
        private ConcurrentQueue<float> AudioBuffer = new ConcurrentQueue<float>();

        //[Header("[Capture In-Game Sound]")]
        public bool StreamGameSound = true;
        public int SystemSampleRate = 48000;
        [Range(1, 8)]
        public int SystemChannels = 2;
        public bool ForceMono = true;

        [Tooltip("Mute the local audio playback when the In-Game audio is streaming")]
        public bool MuteLocalAudioPlayback = false;
        public AudioReadMethod AudioReadMode = AudioReadMethod.OnAudioFilterRead;
        //----------------------------------------------

        [Range(1f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        public FMGZipEncodeMode GZipMode = FMGZipEncodeMode.HighPerformance;

        public AudioOutputFormat OutputFormat = AudioOutputFormat.FMPCM16;
        public bool OutputAsChunks = true;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawPCM16ReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 2001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int maximumChunkSize = Int32.MaxValue - 1024;
        public int OutputChunkSize = 1436; //32768;
        public int GetChunkSize() { return OutputAsChunks ? OutputChunkSize : maximumChunkSize; }
        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByteFMPCM16 = new Queue<byte[]>();
        private Queue<byte[]> AppendQueueSendBytePCM16 = new Queue<byte[]>();

        public int dataLength;

        // Use this for initialization
        private void Start()
        {
            Application.runInBackground = true;

            SystemSampleRate = AudioSettings.GetConfiguration().sampleRate;

            if (GetComponent<AudioListener>() == null) this.gameObject.AddComponent<AudioListener>();
            AudioListenerObject = FindObjectsOfType<AudioListener>();
            for (int i = 0; i < AudioListenerObject.Length; i++)
            {
                if (AudioListenerObject[i] != null) AudioListenerObject[i].enabled = (AudioListenerObject[i].gameObject == this.gameObject);
            }

            StartAll();
        }

        public int OutputSampleRate = 24000;
        public int OutputChannels = 2;
        public bool MatchSystemSampleRate = true;
        public int TargetSampleRate = 24000;
        private float sampleRateScalar = 0f;
        private float scaledStep = 0f;
        private float lastAudioBuffer = 0f;

        private long _readCount = 0;
        public int ReadCount
        {
            get { return Convert.ToInt32(Interlocked.Read(ref _readCount)); }
            set { Interlocked.Exchange(ref _readCount, (long)value); }
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

        private void OnAudioFilterRead(float[] data, int channels)
        {
            OutputSampleRate = MatchSystemSampleRate ? SystemSampleRate : TargetSampleRate;
            OutputChannels = ForceMono ? 1 : SystemChannels;
            SystemChannels = channels;

            if (StreamGameSound)
            {
                if (AudioReadMode == AudioReadMethod.OnAudioFilterRead)
                {
                    ReadCount = 0;

                    int _step = ForceMono ? channels : 1;
                    int _audioDataLength = data.Length;
                    if (MatchSystemSampleRate)
                    {
                        for (int i = 0; i < _audioDataLength; i += _step) AudioBuffer.Enqueue(data[i]);
                    }
                    else
                    {
                        sampleRateScalar = (float)SystemSampleRate / (float)TargetSampleRate;
                        scaledStep %= 1f;
                        do
                        {
                            float currentAudioBuffer = data[(int)scaledStep];
                            AudioBuffer.Enqueue(Mathf.Lerp(lastAudioBuffer, currentAudioBuffer, scaledStep % 1f));
                            lastAudioBuffer = currentAudioBuffer;
                            scaledStep += _step * sampleRateScalar;
                        } while (scaledStep < _audioDataLength);
                    }

                    if (MuteLocalAudioPlayback) Array.Copy(new float[_audioDataLength], data, _audioDataLength);
                }
                else if (AudioReadMode == AudioReadMethod.AudioListenerGetOutputData)
                {
                    ReadCount += data.Length / channels;
                }
            }
        }

        private async void EncodeAudioBufferAsync()
        {
            while (!stoppedOrCancelled())
            {
                if (AudioReadMode == AudioReadMethod.OnAudioFilterRead)
                {
                    //Dequeue buffered audio data from OnAudioFilterRead(), and Encode Bytes in main thread
                    int audioBufferCount = AudioBuffer.Count;
                    if (audioBufferCount > 0)
                    {
                        //skip extra queued data, make sure there is no accumulated audio buffer
                        while (audioBufferCount > OutputSampleRate)
                        {
                            if (AudioBuffer.TryDequeue(out float removedAudioBuffer)) audioBufferCount--;
                        }

                        isReadingOrEncodingAudioBuffer = true;
                        do
                        {
                            if (AudioBuffer.TryDequeue(out float _data))
                            {
                                if (OutputFormat == AudioOutputFormat.FMPCM16)
                                {
                                    byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(_data));
                                    foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                }
                                else
                                {
                                    //byte[] byteData = BitConverter.GetBytes(_data);
                                    byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(_data));
                                    foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                }

                                audioBufferCount--;
                            }
                        } while (audioBufferCount > 0);
                        isReadingOrEncodingAudioBuffer = false;
                    }
                }
                else if (AudioReadMode == AudioReadMethod.AudioListenerGetOutputData)
                {
                    //Read audio buffer from AudioListener.GetOutputData() directly and Encode Bytes in the main thread
                    if (ReadCount > 0)
                    {
                        /*
                        //for unknown reason, this may create some lag in playback unexpected
                        int _readCount = 1;
                        while (ReadCount >= _readCount) _readCount *= 2;
                        _readCount /= 2;
                        ReadCount -= _readCount;
                        */

                        int _readCount = ReadCount;
                        ReadCount = 0;

                        if (!StreamGameSound) _readCount = 0; //skip data if not streaming
                        if (_readCount > OutputSampleRate) _readCount = OutputSampleRate; //skip overloaded buffer...

                        if (_readCount > 0)
                        {
                            isReadingOrEncodingAudioBuffer = true;
                            List<float[]> data = new List<float[]>();
                            for (int i = 0; i < OutputChannels; i++)
                            {
                                data.Add(new float[_readCount]);
                                AudioListener.GetOutputData(data[i], i);
                            }

                            if (MatchSystemSampleRate)
                            {
                                for (int i = 0; i < _readCount; i++)
                                {
                                    for (int targetChannel = 0; targetChannel < OutputChannels; targetChannel++)
                                    {
                                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(data[targetChannel][i]));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                        else
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(data[targetChannel][i]));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                sampleRateScalar = (float)SystemSampleRate / (float)TargetSampleRate;
                                scaledStep %= 1f;

                                do
                                {
                                    for (int targetChannel = 0; targetChannel < OutputChannels; targetChannel++)
                                    {
                                        float currentAudioBuffer = data[targetChannel][(int)scaledStep];

                                        lastAudioBuffer = currentAudioBuffer;
                                        scaledStep += sampleRateScalar;

                                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(Mathf.Lerp(lastAudioBuffer, currentAudioBuffer, scaledStep % 1f)));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                        else
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(Mathf.Lerp(lastAudioBuffer, currentAudioBuffer, scaledStep % 1f)));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                    }
                                } while (scaledStep < _readCount);
                            }
                            isReadingOrEncodingAudioBuffer = false;
                        }
                    }
                }
                await Task.Yield();
            }
        }

        private async void SenderAsync()
        {
            InvokeEventsCheckerAsync();
            await FMCoreTools.AsyncTask.Delay(1);
            await FMCoreTools.AsyncTask.Yield(); //just skip one frame, in case that you want to change audio read mode

            EncodeAudioBufferAsync();
            while (!stoppedOrCancelled())
            {
                if (Time.realtimeSinceStartup > next)
                {
                    interval = 1f / StreamFPS;
                    next = Time.realtimeSinceStartup + interval;

                    //check if reading buffer? and check if there is any audio bytes?
                    while (isReadingOrEncodingAudioBuffer || AudioBytes.Count <= 0)
                    {
                        await FMCoreTools.AsyncTask.Delay(1);
                        await FMCoreTools.AsyncTask.Yield();
                    }

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
                        int _dataByteLength = (i == _chunkCount) ? (_length % _chunkSize) : (_chunkSize);
                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                        {
                            byte[] _meta_offset = BitConverter.GetBytes(_offset);
                            byte[] SendByte = new byte[_dataByteLength + _metaByteLength];

                            Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                            Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                            Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                            Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                            SendByte[12] = (byte)(GZipMode != FMGZipEncodeMode.None ? 1 : 0);
                            SendByte[13] = (byte)OutputFormat;

                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 14, _dataByteLength);
                            AppendQueueSendByteFMPCM16.Enqueue(SendByte);
                        }
                        else if (OutputFormat == AudioOutputFormat.PCM16)
                        {
                            byte[] SendByte = new byte[_dataByteLength];
                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 0, _dataByteLength);

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

        private void OnEnable() { StartAll(); }
        private void OnDisable() { StopAll(); }
        private void OnApplicationQuit() { StopAll(); }
        private void OnDestroy() { StopAll(); }

        private CancellationTokenSource cancellationTokenSource_global;
        //private void OnEnable() { InitAsyncTokenSource(); }
        //private void OnDisable() { StopAllAsync(); }
        private bool stoppedOrCancelled() { return cancellationTokenSource_global.IsCancellationRequested; }
        private void InitAsyncTokenSource() { cancellationTokenSource_global = new CancellationTokenSource(); }
        private void StopAllAsync()
        {
            if (cancellationTokenSource_global != null)
            {
                if (!cancellationTokenSource_global.IsCancellationRequested) cancellationTokenSource_global.Cancel();
            }
        }

        private bool initialised = false;
        private void StartAll()
        {
            if (initialised) return;
            initialised = true;

            stop = false;
            InitAsyncTokenSource();
            SenderAsync();

            if (AudioListenerObject != null)
            {
                for (int i = 0; i < AudioListenerObject.Length; i++)
                {
                    if (AudioListenerObject[i] != null) AudioListenerObject[i].enabled = (AudioListenerObject[i].gameObject == this.gameObject);
                }
            }
        }
        private void StopAll()
        {
            initialised = false;
            stop = true;
            StopAllAsync();

            AppendQueueSendByteFMPCM16.Clear();
            AppendQueueSendBytePCM16.Clear();

            //reset listener
            if (AudioListenerObject != null)
            {
                for (int i = 0; i < AudioListenerObject.Length; i++)
                {
                    if (AudioListenerObject[i] != null) AudioListenerObject[i].enabled = (AudioListenerObject[i].gameObject != this.gameObject);
                }
            }
        }
    }
}