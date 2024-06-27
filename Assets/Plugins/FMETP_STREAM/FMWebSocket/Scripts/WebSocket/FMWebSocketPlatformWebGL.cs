using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace FMSolution.FMWebSocket
{
    public class FMWebSocketPlatformWebGL
    {
        public class Component : FMWebSocketCore
        {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
#if UNITY_2021_2_OR_NEWER
            [DllImport("__Internal")] private static extern string FMWebSocket_IsWebSocketConnected_2021_2();
        [DllImport("__Internal")] private static extern void FMWebSocket_AddEventListeners_2021_2(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendByte_2021_2(byte[] array, int size);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendString_2021_2(string _src);
        [DllImport("__Internal")] private static extern void FMWebSocket_Close_2021_2();
#else
        [DllImport("__Internal")] private static extern string FMWebSocket_IsWebSocketConnected_2021_2_before();
        [DllImport("__Internal")] private static extern void FMWebSocket_AddEventListeners_2021_2_before(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendByte_2021_2_before(byte[] array, int size);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendString_2021_2_before(string _src);
        [DllImport("__Internal")] private static extern void FMWebSocket_Close_2021_2_before();
#endif
#endif
            internal override int LastPingTimeMS
            {
                get { return (int)_lastPingTimeMS; }
                set { _lastPingTimeMS = (long)value; }
            }
            internal override int LastPongTimeMS
            {
                get { return (int)_lastPongTimeMS; }
                set { _lastPongTimeMS = (long)value; }
            }

            private bool stop = false;
            private bool stoppedOrCancelled() { return stop; }
            internal override void StopAll()
            {
                FMWebSocket_Close();

                //skip, if stopped already
                if (stop)
                {
                    StopAllCoroutines();//stop all coroutines, just in case
                    return;
                }

                stop = true;
                wsConnected = false;

                StopAllCoroutines();
                ResetConcurrentQueues();
            }

            internal override void StartAll()
            {
                connectionStatus = FMWebSocketConnectionStatus.Disconnected;

                if (initialised) return;
                initialised = true;

                stop = false;
                StartCoroutine(ConnectAndAddEventListenersCOR(0.2f));
                WebSocketStartLoop();
            }

            public void RegOnOpen() { RegisterNetworkType(); DebugLog(">>> UNITY: ON OPEN"); }
            public void RegOnClose() { DebugLog(">>> UNITY: ON Close"); wsConnected = false; }
            public void RegOnError(string _msg) { DebugLog(">>> UNITY: (Error) " + _msg); wsConnected = false; }

            public void RegOnMessageString(string _msg)
            {
                _appendQueueReceivedStringData.Enqueue(_msg);
            }
            public void RegOnMessageRawData(string _stringData)
            {
                //Binary
                _appendQueueReceivedData.Enqueue(System.Convert.FromBase64String(_stringData));
            }
            public void RegOnMessageRawData(byte[] byteData)
            {
                _appendQueueReceivedData.Enqueue(byteData);
            }

            private void ResetWebSocket()
            {
                LastPingTimeMS = 0;
                LastPongTimeMS = 0;
                wsJoinedRoom = false;

                url = "ws" + (sslEnabled ? "s" : "") + "://" + IP;
                if (portRequired) url += (port != 0 ? ":" + port.ToString() : "");
            }

            private IEnumerator ConnectAndAddEventListenersCOR(float _delaySeconds = 0.5f)
            {
                yield return new WaitForSecondsRealtime(_delaySeconds);
                yield return null;

                ResetWebSocket();
                FMWebSocket_AddEventListeners(url, gameObject.name);
                DebugLog(">>> ConnectAndAddEventListeners");

                float _timer_connecting = 0f;
                while (!stoppedOrCancelled() && !IsWebSocketConnected() && _timer_connecting < 2000)
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    _timer_connecting += 100;

                    if (!IsWebSocketConnected()) DebugLog("connection: try reaching server");
                }

                //Connection Checker
                StartCoroutine(ConnectionCheckerCOR(0.5f));
            }

            private void FMWebSocket_AddEventListeners(string _src, string _gameobject)
            {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
#if UNITY_2021_2_OR_NEWER
                FMWebSocket_AddEventListeners_2021_2(_src, _gameobject);
#else
                FMWebSocket_AddEventListeners_2021_2_before(_src, _gameobject);
#endif
#endif
            }

            private IEnumerator ConnectionCheckerCOR(float _delaySeconds = 0.5f)
            {
                while (!stoppedOrCancelled())
                {
                    yield return new WaitForSecondsRealtime(_delaySeconds);
                    if (!GetWSConnectionStatus())
                    {
                        if (autoReconnect)
                        {
                            FMWebSocket_Close();
                            ResetWebSocket();
                            yield return new WaitForSecondsRealtime(_delaySeconds);

                            try
                            {
                                ResetConcurrentQueues();
                                initialised = true;

                                FMWebSocket_AddEventListeners(url, gameObject.name);
                                DebugLog(">>> ConnectAndAddEventListeners");
                            }
                            catch (Exception e)
                            {
                                DebugLog("Connection Execption: " + e.Message);
                            }

                            float _timer_connecting = 0f;
                            while (!stoppedOrCancelled() && !IsWebSocketConnected() && _timer_connecting < 2000)
                            {
                                yield return new WaitForSecondsRealtime(0.1f);
                                _timer_connecting += 100;
                                DebugLog("reconnecting");
                            }
                            DebugLog("reconnecting: " + !IsWebSocketConnected());
                        }
                        else
                        {
                            Close();
                        }
                    }
                    else
                    {
                        FMPing();
                    }
                }
                FMWebSocket_Close();
            }

            internal override bool IsWebSocketConnected()
            {
                string wsReadyState = "-1";
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
#if UNITY_2021_2_OR_NEWER
                wsReadyState = FMWebSocket_IsWebSocketConnected_2021_2();
#else
                wsReadyState = FMWebSocket_IsWebSocketConnected_2021_2_before();
#endif
#endif
                /*
                 * wsReadyState -1: ws is null...
                 * wsReadyState 0: Connecting
                 * wsReadyState 1: Open
                 * wsReadyState 2: Closing
                 * wsReadyState 3: Closed
                 */

                //DebugLog("return value: -> > > " + wsReadyState);
                return (wsReadyState == "1" || wsReadyState == "2") ? true : false;
            }

            internal override void WebSocketSenderLoop() { StartCoroutine(MainThreadSenderCOR()); }
            private IEnumerator MainThreadSenderCOR()
            {
                while (!stop)
                {
                    yield return null;
                    Sender();
                }
            }

            internal override void WebSocketStartLoop() { StartCoroutine(WebSocketStartCOR()); }
            private IEnumerator WebSocketStartCOR()
            {
                stop = false;
                yield return new WaitForSeconds(0.5f);

                //WebGL is using main thread only
                WebSocketSenderLoop();

                while (!stop)
                {
                    while (_appendQueueReceivedData.Count > 0)
                    {
                        if (_appendQueueReceivedData.TryDequeue(out byte[] ReceivedData))
                        {
                            if (ReceivedData.Length > 4)
                            {
                                byte[] _meta = new byte[] { ReceivedData[0], ReceivedData[1], ReceivedData[2], ReceivedData[3] };
                                byte[] _data = new byte[ReceivedData.Length - 4];

                                if (_meta[1] == 3)
                                {
                                    //remove target wsid meta
                                    int _wsidByteLength = (int)BitConverter.ToUInt16(ReceivedData, 4);
                                    _data = new byte[ReceivedData.Length - 6 - _wsidByteLength];
                                    Buffer.BlockCopy(ReceivedData, 6 + _wsidByteLength, _data, 0, _data.Length);
                                }
                                else
                                {
                                    Buffer.BlockCopy(ReceivedData, 4, _data, 0, _data.Length);
                                }

                                switch (_meta[0])
                                {
                                    case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                    case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                }
                            }
                            Manager.GetRawReceivedByteDataEvent.Invoke(ReceivedData);
                        }
                    }
                    while (_appendQueueReceivedStringData.Count > 0)
                    {
                        if (_appendQueueReceivedStringData.TryDequeue(out string ReceivedData))
                        {
                            OnMessageCheck(ReceivedData);
                            Manager.GetRawReceivedStringDataEvent.Invoke(ReceivedData);
                        }
                    }
                    yield return null;
                }
                yield break;
            }

            internal override void FMWebSocket_Close()
            {
                initialised = false;
                wsJoinedRoom = false;
                wsRoomMaster = false;

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
#if UNITY_2021_2_OR_NEWER
                FMWebSocket_Close_2021_2();
#else
                FMWebSocket_Close_2021_2_before();
#endif
#endif
                wsConnected = false;
                connectionStatus = FMWebSocketConnectionStatus.Disconnected;
            }

            internal override void FMWebSocket_Send(byte[] _byteData, bool sendAsync = false)
            {
                WebSocket_Send(_byteData, sendAsync);
            }
            internal override void FMWebSocket_Send(string _stringData, bool sendAsync = false)
            {
                WebSocket_Send(_stringData, sendAsync);
            }
            internal override void WebSocket_Send(byte[] _byteData, bool sendAsync = false)
            {
                try
                {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
#if UNITY_2021_2_OR_NEWER
                    FMWebSocket_SendByte_2021_2(_byteData, _byteData.Length);
#else
                    FMWebSocket_SendByte_2021_2_before(_byteData, _byteData.Length);
#endif
#endif
                }
                catch { }
            }
            internal override void WebSocket_Send(string _stringData, bool sendAsync = false)
            {
                try
                {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
#if UNITY_2021_2_OR_NEWER
                    FMWebSocket_SendString_2021_2(_stringData);
#else
                    FMWebSocket_SendString_2021_2_before(_stringData);
#endif
#endif
                }
                catch { }
            }
        }
    }
}