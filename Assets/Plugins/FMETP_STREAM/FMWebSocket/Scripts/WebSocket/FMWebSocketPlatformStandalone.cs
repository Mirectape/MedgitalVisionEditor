using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;

namespace FMSolution.FMWebSocket
{
    public class FMWebSocketPlatformStandalone
    {
        public class Component : FMWebSocketCore
        {
            private CancellationTokenSource cancellationTokenSource_global;
            private bool stoppedOrCancelled() { return stop || cancellationTokenSource_global.IsCancellationRequested; }
            private void StopAllAsync() { if (cancellationTokenSource_global != null) cancellationTokenSource_global.Cancel(); }

            internal override int LastPingTimeMS
            {
                get { return (int)Interlocked.Read(ref _lastPingTimeMS); }
                set { Interlocked.Exchange(ref _lastPingTimeMS, (long)value); }
            }
            internal override int LastPongTimeMS
            {
                get { return (int)Interlocked.Read(ref _lastPongTimeMS); }
                set { Interlocked.Exchange(ref _lastPongTimeMS, (long)value); }
            }

            private long _stop = 0;
            private bool stop
            {
                get { return Interlocked.Read(ref _stop) == 1; }
                set { Interlocked.Exchange(ref _stop, Convert.ToInt64(value)); }
            }


            private WebSocket ws;
            internal override void StopAll()
            {
                FMWebSocket_Close();

                //skip, if stopped already
                if (stop)
                {
                    StopAllAsync(); //stop all Async again, just in case
                    return;
                }

                stop = true;
                wsConnected = false;

                StopAllAsync();
                ResetConcurrentQueues();
            }

            internal override void StartAll()
            {
                connectionStatus = FMWebSocketConnectionStatus.Disconnected;
                cancellationTokenSource_global = new CancellationTokenSource();

                if (initialised) return;
                initialised = true;

                stop = false;
                Task.Run(() => ConnectAndAddEventListenersAsync(200));
                WebSocketStartLoop();
            }

            public void OnOpen(object sender, EventArgs e) { RegisterNetworkType(); }
            public void OnClose(object sender, EventArgs e) { DebugLog("onClose"); wsConnected = false; }
            public void OnError(object sender, EventArgs e) { DebugLog("onError"); wsConnected = false; }
            public void OnMessage(object sender, MessageEventArgs e)
            {
                if (e.IsBinary) { _appendQueueReceivedData.Enqueue(e.RawData); }
                else { _appendQueueReceivedStringData.Enqueue(e.Data); }
            }

            private bool useAsync = true;
            //private bool useAsync = false;

            private void ResetWebSocket()
            {
                LastPingTimeMS = 0;
                LastPongTimeMS = 0;
                wsJoinedRoom = false;

                url = "ws" + (sslEnabled ? "s" : "") + "://" + IP;
                if (portRequired) url += ":" + port;
                ws = new WebSocket(url);
                ws.WaitTime = TimeSpan.FromMilliseconds(2000);//timeout
                ws.OnMessage += (sender, e) =>
                {
                    //DebugLog(e.IsBinary);
                    //DebugLog(e.RawData.Length);
                };

                ws.OnOpen += OnOpen;
                ws.OnMessage += OnMessage;
                ws.OnError += OnError;
                ws.OnClose += OnClose;

                if (sslEnabled)
                {
                    switch (sslProtocols)
                    {
                        case FMSslProtocols.None: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None; break;
                        case FMSslProtocols.Tls: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls; break;
#if UNITY_2019_1_OR_NEWER
                        case FMSslProtocols.Tls11: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls11; break;
                        case FMSslProtocols.Tls12: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12; break;
#else
                        case FMSslProtocols.Default: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Default; break;
                        case FMSslProtocols.Ssl2: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Ssl2; break;
                        case FMSslProtocols.Ssl3: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Ssl3; break;
                        default: ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Default; break;
#endif
                    }
                }
            }
            private async void ConnectAndAddEventListenersAsync(int inputDelayMS = 20)
            {
                await FMCoreTools.AsyncTask.Delay(inputDelayMS);
                await FMCoreTools.AsyncTask.Yield(); //have to wait for next frame after "OnEnable()" in first frame of Game Start, which cause connection bug

                ResetWebSocket();
                ws.ConnectAsync();

                float _timer_connecting = 0f;
                while (!stoppedOrCancelled() && !IsWebSocketConnected() && _timer_connecting < 2000)
                {
                    await FMCoreTools.AsyncTask.Delay(100, cancellationTokenSource_global.Token);
                    _timer_connecting += 100;

                    if (!IsWebSocketConnected()) DebugLog("connection: try reaching server");
                }

                //Connection Checker
                ConnectionCheckerAsync(500);
            }

            private async void ConnectionCheckerAsync(int inputDelayMS = 500)
            {
                while (!stoppedOrCancelled())
                {
                    await FMCoreTools.AsyncTask.Delay(inputDelayMS);
                    if (!GetWSConnectionStatus())
                    {
                        if (autoReconnect && !stoppedOrCancelled())
                        {
                            FMWebSocket_Close();
                            ResetWebSocket();
                            await FMCoreTools.AsyncTask.Delay(inputDelayMS);

                            try
                            {
                                ResetConcurrentQueues();
                                initialised = true;

                                ws.ConnectAsync();
                            }
                            catch (Exception e)
                            {
                                DebugLog("Connection Execption: " + e.Message);
                            }

                            float _timer_connecting = 0f;
                            while (!stoppedOrCancelled() && !IsWebSocketConnected() && _timer_connecting < 2000)
                            {
                                await FMCoreTools.AsyncTask.Delay(100);
                                _timer_connecting += 100;
                                DebugLog("reconnecting");
                            }
                            DebugLog("reconnecting: " + !IsWebSocketConnected());
                        }
                        else
                        {
                            Close();//Close FMWebSocket automatically...
                        }
                    }
                    else
                    {
                        FMPing();
                    }
                }
                FMWebSocket_Close();
            }

            internal override void FMWebSocket_Close()
            {
                initialised = false;
                wsJoinedRoom = false;
                wsRoomMaster = false;

                if (ws != null)
                {
                    if (ws.IsAlive)
                    {
                        if (!useAsync) ws.Close();
                        if (useAsync) ws.CloseAsync();
                    }
                }
                wsConnected = false;

                connectionStatus = FMWebSocketConnectionStatus.Disconnected;
            }

            //use ws.Send Sync method is suggested, with the least latency
            //however, if using MainThreadSender, better use SendAsync, which won't block the mainthread with failure connection
            internal override void FMWebSocket_Send(byte[] _byteData, bool sendAsync = false) { WebSocket_Send(_byteData, sendAsync); }
            internal override void FMWebSocket_Send(string _stringData, bool sendAsync = false) { WebSocket_Send(_stringData, sendAsync); }
            internal override void WebSocket_Send(byte[] _byteData, bool sendAsync = false)
            {
                try
                {
                    if (sendAsync) { ws.SendAsync(_byteData, wsSendAsyncAction); }
                    else { ws.Send(_byteData); }
                }
                catch { }
            }
            internal override void WebSocket_Send(string _stringData, bool sendAsync = false)
            {
                try
                {
                    if (sendAsync) { ws.SendAsync(_stringData, wsSendAsyncAction); }
                    else { ws.Send(_stringData); }
                }
                catch { }
            }

            internal override bool IsWebSocketConnected() { return (ws.ReadyState == WebSocketState.Open || ws.ReadyState == WebSocketState.Closing) ? true : false; }

            internal override void WebSocketSenderLoop() { WebSocketSenderAsync(); }
            private async void WebSocketSenderAsync()
            {
                if (UseMainThreadSender)
                {
                    while (!stoppedOrCancelled())
                    {
                        await FMCoreTools.AsyncTask.Delay(1);
                        await FMCoreTools.AsyncTask.Yield();
                        Sender(true);
                    }
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        while (!stoppedOrCancelled())
                        {
                            Sender();
                            await FMCoreTools.AsyncTask.Delay(1);
                        }
                    });
                }
            }

            internal override void WebSocketStartLoop() { WebSocketStartAsync(); }
            private async void WebSocketStartAsync()
            {
                stop = false;
                await FMCoreTools.AsyncTask.Delay(500);

                WebSocketSenderLoop();

                while (!stoppedOrCancelled())
                {
                    while (_appendQueueReceivedData.Count > 0)
                    {
                        if (_appendQueueReceivedData.TryDequeue(out byte[] _receivedData))
                        {
                            if (_receivedData.Length > 4)
                            {
                                byte[] _meta = new byte[] { _receivedData[0], _receivedData[1], _receivedData[2], _receivedData[3] };
                                byte[] _data = new byte[_receivedData.Length - 4];

                                if (_meta[1] == 3)
                                {
                                    //remove target wsid meta
                                    int _wsidByteLength = (int)BitConverter.ToUInt16(_receivedData, 4);
                                    _data = new byte[_receivedData.Length - 6 - _wsidByteLength];
                                    Buffer.BlockCopy(_receivedData, 6 + _wsidByteLength, _data, 0, _data.Length);
                                }
                                else
                                {
                                    Buffer.BlockCopy(_receivedData, 4, _data, 0, _data.Length);
                                }

                                switch (_meta[0])
                                {
                                    case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                    case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                }
                            }
                            Manager.GetRawReceivedByteDataEvent.Invoke(_receivedData);
                        }
                    }
                    while (_appendQueueReceivedStringData.Count > 0)
                    {
                        if (_appendQueueReceivedStringData.TryDequeue(out string _receivedData))
                        {
                            OnMessageCheck(_receivedData);
                            Manager.GetRawReceivedStringDataEvent.Invoke(_receivedData);
                        }
                    }
                    await FMCoreTools.AsyncTask.Delay(1);
                    await FMCoreTools.AsyncTask.Yield();
                }
            }

        }
    }
}