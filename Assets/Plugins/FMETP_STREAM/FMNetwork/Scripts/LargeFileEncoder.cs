using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

namespace FMSolution.FMNetwork
{
    [AddComponentMenu("FMETP/Mapper/LargeFileEncoder")]
    public class LargeFileEncoder : MonoBehaviour
    {
        [Tooltip("target streaming byte per milliseconds")]
        [Range(1, 200000)]
        public int TargetBytePerMS = 10000;
        private int targetWaitMS
        {
            get
            {
                return Mathf.CeilToInt(((float)streamedLength) / ((float)TargetBytePerMS));
            }
        }

        private bool stop = false;
        private int streamedLength = 0;
        private Queue<byte[]> _appendQueueSendData = new Queue<byte[]>();
        private Queue<byte[]> _appendQueueChunksBytes = new Queue<byte[]>();

        public bool OutputAsChunks = true;
        private int maximumChunkSize = Int32.MaxValue - 1024;
        public int OutputChunkSize = 1436;//8096; //32768
        public int GetChunkSize() { return OutputAsChunks ? OutputChunkSize : maximumChunkSize; }
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

        [Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 8001;

        private UInt16 offsetSequence = 0;
        private UInt16 localSequence = 0;
        private UInt16 dataID
        {
            get
            {
                localSequence++;
                if (localSequence > maxSequence) localSequence = 0;
                return (UInt16)(offsetSequence + localSequence);
            }
        }

        private UInt16 maxSequence = 1024;

        private void Start()
        {
            Application.runInBackground = true;
            StartAll();
        }

        public void Action_SendLargeByte(byte[] _data) { _appendQueueSendData.Enqueue(_data); }
        private async void CheckQueuedDataAsync()
        {
            while (!stoppedOrCancelled())
            {
                await FMCoreTools.AsyncTask.Delay(1);
                await FMCoreTools.AsyncTask.Yield();

                while (_appendQueueSendData.Count > 0 && !stoppedOrCancelled())
                {
                    await FMCoreTools.AsyncTask.Yield();
                    byte[] _dataByte = _appendQueueSendData.Dequeue();
                    int _length = _dataByte.Length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    int _metaByteLength = 12;
                    int _chunkSize = GetChunkSize();
                    _chunkSize -= _metaByteLength;
                    int _chunkCount = Mathf.CeilToInt((float)_length / (float)_chunkSize);
                    for (int i = 1; i <= _chunkCount; i++)
                    {
                        int dataByteLength = (i == _chunkCount) ? (_length % _chunkSize) : (_chunkSize);
                        byte[] _meta_offset = BitConverter.GetBytes(_offset);
                        byte[] SendByte = new byte[dataByteLength + _metaByteLength];

                        Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                        Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                        Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                        Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);

                        Buffer.BlockCopy(_dataByte, _offset, SendByte, 12, dataByteLength);

                        _appendQueueChunksBytes.Enqueue(SendByte);
                        _offset += _chunkSize;
                    }
                }

                while (_appendQueueChunksBytes.Count > 0 && !stoppedOrCancelled())
                {
                    byte[] _streamByte = _appendQueueChunksBytes.Dequeue();

                    await FMCoreTools.AsyncTask.Yield();
                    OnDataByteReadyEvent.Invoke(_streamByte);

                    streamedLength += _streamByte.Length;

                    if (streamedLength > TargetBytePerMS)
                    {
                        await FMCoreTools.AsyncTask.Delay(targetWaitMS);
                        streamedLength %= TargetBytePerMS;
                    }
                }
            }
        }

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

        private void OnEnable() { StartAll(); }

        private bool initialised = false;
        private void StartAll()
        {
            cancellationTokenSource_global = new CancellationTokenSource();

            if (initialised) return;
            initialised = true;

            stop = false;
            offsetSequence = (UInt16)(2048 + Mathf.RoundToInt(UnityEngine.Random.Range(0f, 1024f)) * 2048);
            CheckQueuedDataAsync();
        }

        private void StopAll()
        {
            initialised = false;
            stop = true;
            StopAllAsync();

            _appendQueueSendData.Clear();
            _appendQueueChunksBytes.Clear();
            _appendQueueSendData = new Queue<byte[]>();
            _appendQueueChunksBytes = new Queue<byte[]>();
        }
    }
}