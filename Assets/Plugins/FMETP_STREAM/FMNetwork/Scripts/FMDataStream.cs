using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/*
 * + StereoPi Commands(Example):
 * Connect via ssh:
 * ssh root@192.168.xx.xx
 * Pwd: root
 *
 * Stop Default Stream:
 * /opt/StereoPi/stop.sh
 *
 * Sending stream from Raspberry:
 *
 * For UDP:
 * raspivid -t 0 -w 1280 -h 720 -fps 30 -3d sbs -cd MJPEG -o - | nc 192.168.1.10 3001 -u
 *
 * For TCP:
 * raspivid -t 0 -w 1280 -h 720 -fps 30 -3d sbs -cd MJPEG -o - | nc 192.168.1.10 3001
 *
 * where 192.168.1.10 3001 - IP and port
*/

/*
 * + GStreamer Commands(Example):
 * + Desktop Capture to Unity
 * gst-launch-1.0 gdiscreencapsrc ! queue ! video/x-raw,framerate=60/1,width=1920, height=1080 ! jpegenc ! rndbuffersize max=65000 ! udpsink host=192.168.1.10 port=3001
 *
 * + Video Stream to Unity
 * gst-launch-1.0 filesrc location="videopath.mp4" ! queue ! decodebin ! videoconvert ! jpegenc ! rndbuffersize max=65000 ! udpsink host=192.168.1.10 port=3001
 */

namespace FMSolution.FMNetwork
{
    public class FMDataStream
    {
        public class FMDataStreamComponent : MonoBehaviour
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            private int udpSendBufferSize = 1024 * 65; //max 65535
            private int udpReceiveBufferSize = 1024 * 1024 * 4; //max 2147483647
#else
            private int udpSendBufferSize = 1024 * 60; //max 65535
            private int udpReceiveBufferSize = 1024 * 512; //max 2147483647
#endif

            [HideInInspector] public FMNetworkManager Manager;

            public FMDataStreamType DataStreamType = FMDataStreamType.Receiver;
            public FMProtocol Protocol = FMProtocol.UDP;

            public void BroadcastChecker()
            {
                UdpClient _broadcastClient = new UdpClient();
                try
                {
                    _broadcastClient.Client.SendTimeout = 200;
                    _broadcastClient.EnableBroadcast = true;

                    byte[] _byte = new byte[1];
                    _broadcastClient.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Parse(Manager.ReadBroadcastAddress), ClientListenPort));

                    if (_broadcastClient != null) _broadcastClient.Close();
                }
                catch
                {
                    if (_broadcastClient != null) _broadcastClient.Close();
                }
            }

            //TCP Client props..
            public string ServerIP { get { return Manager.DataStreamSettings.ServerIP; } }
            //Sender props..
            public string ClientIP { get { return Manager.DataStreamSettings.ClientIP; } }
            public List<string> ClientIPList { get { return Manager.DataStreamSettings.ClientIPList; } }
            public UdpClient udpClient_Sender;
            private ConcurrentQueue<byte[]> _appendSendBytes = new ConcurrentQueue<byte[]>();
            public bool UseMainThreadSender = false;

            public void Action_AddBytes(byte[] inputBytes) { _appendSendBytes.Enqueue(inputBytes); }
            private async void NetworkClientStartUDPSenderAsync()
            {
                stop = false;

                BroadcastChecker();
                await FMCoreTools.AsyncTask.Yield();

                NetworkClientUDPSenderAsync();
            }

            private async void NetworkClientUDPSenderAsync()
            {
                if (UseMainThreadSender)
                {
                    while (!stoppedOrCancelled())
                    {
                        await FMCoreTools.AsyncTask.Delay(1);
                        await FMCoreTools.AsyncTask.Yield();
                        Sender();
                    }
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            while (!stoppedOrCancelled())
                            {
                                Sender();
                                await FMCoreTools.AsyncTask.Delay(1);
                            }
                        }
                        catch { }
                    });
                }
            }

            private void Sender()
            {
                try
                {
                    if (udpClient_Sender == null)
                    {
                        udpClient_Sender = new UdpClient();
                        udpClient_Sender.Client.SendBufferSize = udpSendBufferSize;
                        udpClient_Sender.Client.ReceiveBufferSize = udpReceiveBufferSize;
                        udpClient_Sender.Client.SendTimeout = 500;
                        udpClient_Sender.EnableBroadcast = true;
                        udpClient_Sender.MulticastLoopback = UDPTransferType == FMUDPTransferType.Multicast;
                    }

                    //send to server ip only
                    if (_appendSendBytes.Count > 0)
                    {
                        //limit 30 packet sent in each frame, solved overhead issue on receiver
                        int sendCount = 0;
                        while (_appendSendBytes.Count > 0 && sendCount < 100)
                        {
                            sendCount++;
                            if (_appendSendBytes.TryDequeue(out byte[] _bytes))
                            {
                                if (UDPTransferType == FMUDPTransferType.Broadcast)
                                {
                                    udpClient_Sender.Send(_bytes, _bytes.Length, new IPEndPoint(IPAddress.Parse(Manager.ReadBroadcastAddress), ClientListenPort));
                                }
                                else
                                {
                                    if (UDPTransferType == FMUDPTransferType.MultipleUnicast)
                                    {
                                        if (ClientIPList.Count > 0)
                                        {
                                            for (int i = 0; i < ClientIPList.Count; i++)
                                            {
                                                udpClient_Sender.Send(_bytes, _bytes.Length, new IPEndPoint(IPAddress.Parse(ClientIPList[i]), ClientListenPort));
                                            }
                                        }
                                    }
                                    else if (UDPTransferType == FMUDPTransferType.Unicast)
                                    {
                                        udpClient_Sender.Send(_bytes, _bytes.Length, new IPEndPoint(IPAddress.Parse(ClientIP), ClientListenPort));
                                    }
                                    else if (UDPTransferType == FMUDPTransferType.Multicast)
                                    {
                                        udpClient_Sender.Send(_bytes, _bytes.Length, new IPEndPoint(IPAddress.Parse(MulticastAddress), ClientListenPort));
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //DebugLog("client sender timeout: " + socketException.ToString());
                    if (udpClient_Sender != null) udpClient_Sender.Close(); udpClient_Sender = null;
                }
            }

            public int ClientListenPort = 3001;

            public FMUDPTransferType UDPTransferType = FMUDPTransferType.Unicast;
            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Synchronous;
            public FMTCPSocketType TCPSocketType = FMTCPSocketType.TCPServer;
            public string MulticastAddress = "239.255.255.255";

            private UdpClient udpClient_Listener;
            private IPEndPoint ServerEp;

            private TcpListener tcpServer_Listener;
            private List<TcpClient> tcpServer_Clients = new List<TcpClient>();
            private List<NetworkStream> tcpServer_Streams = new List<NetworkStream>();
            private bool tcpServerCreated = false;
            public bool IsConnected = false;

            private int EnvironmentTickCountDelta(int currentMS, int lastMS)
            {
                int gap = 0;
                if (currentMS < 0 && lastMS > 0)
                {
                    gap = Mathf.Abs(currentMS - int.MinValue) + (int.MaxValue - lastMS);
                }
                else
                {
                    gap = currentMS - lastMS;
                }
                return gap;
            }

            private int connectionThreshold = 10000;//10sec
            private long _currentSeenTimeMS = 0;
            public int CurrentSeenTimeMS
            {
                get { return Convert.ToInt32(Interlocked.Read(ref _currentSeenTimeMS)); }
                set { Interlocked.Exchange(ref _currentSeenTimeMS, (long)value); }
            }
            private long _lastReceivedTimeMS = 0;
            public int LastReceivedTimeMS
            {
                get { return Convert.ToInt32(Interlocked.Read(ref _lastReceivedTimeMS)); }
                set { Interlocked.Exchange(ref _lastReceivedTimeMS, (long)value); }
            }
            private long _lastSentTimeMS = 0;
            public int LastSentTimeMS
            {
                get { return (int)Interlocked.Read(ref _lastSentTimeMS); }
                set { Interlocked.Exchange(ref _lastSentTimeMS, (long)value); }
            }

            private long _stop = 0;
            private bool stop
            {
                get { return Interlocked.Read(ref _stop) == 1; }
                set { Interlocked.Exchange(ref _stop, Convert.ToInt64(value)); }
            }

            private ConcurrentQueue<byte[]> _appendQueueReceivedBytes = new ConcurrentQueue<byte[]>();

            private int ReceivedCount = 0;

            #region TCP_Server_Listener
            private async void NetworkServerStartTCPListenerAsync()
            {
                if (!tcpServerCreated)
                {
                    tcpServerCreated = true;

                    // create tcpServer_Listener
                    tcpServer_Listener = new TcpListener(IPAddress.Any, ClientListenPort);
                    tcpServer_Listener.Start();
                    tcpServer_Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    TCPServer_ListenerAsync();

                    while (!stoppedOrCancelled())
                    {
                        ReceivedCount = _appendQueueReceivedBytes.Count;
                        while (_appendQueueReceivedBytes.Count > 0)
                        {
                            if (_appendQueueReceivedBytes.TryDequeue(out byte[] receivedBytes))
                            {
                                Manager.OnReceivedByteDataEvent.Invoke(receivedBytes);
                            }
                        }
                        await FMCoreTools.AsyncTask.Yield();
                    }
                }
            }
            private async void TCPServer_ListenerAsync()
            {
                while (!stoppedOrCancelled())
                {
                    await FMCoreTools.AsyncTask.Delay(1);
                    await FMCoreTools.AsyncTask.Yield();
                    // Wait for client connection
                    tcpServer_Clients.Add(await tcpServer_Listener.AcceptTcpClientAsync());
                    tcpServer_Clients[tcpServer_Clients.Count - 1].NoDelay = true;
                    //IsConnected = true;

                    tcpServer_Streams.Add(tcpServer_Clients[tcpServer_Clients.Count - 1].GetStream());
                    tcpServer_Streams[tcpServer_Streams.Count - 1].WriteTimeout = 500;

                    //IsConnected = true;
                    if (tcpServer_Clients != null)
                    {
                        if (tcpServer_Clients.Count > 0) TCPServer_ReceiverAsync(tcpServer_Clients[tcpServer_Clients.Count - 1], tcpServer_Streams[tcpServer_Streams.Count - 1]);
                    }
                }
            }
            private async void TCPServer_ReceiverAsync(TcpClient _client, NetworkStream _stream)
            {
                _stream.ReadTimeout = 1000;
                while (!_client.Connected)
                {
                    await FMCoreTools.AsyncTask.Delay(1);
                    await FMCoreTools.AsyncTask.Yield();
                }
                while (!stoppedOrCancelled())
                {
                    _stream.Flush();
                    byte[] bytes = new byte[300000];

                    // Loop to receive all the data sent by the client.
                    int _length = 0;
                    while (!stoppedOrCancelled() && (_length = await _stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                    {
                        if (_length > 0)
                        {
                            byte[] _s = new byte[_length];
                            Buffer.BlockCopy(bytes, 0, _s, 0, _length);
                            _appendQueueReceivedBytes.Enqueue(_s);
                            LastReceivedTimeMS = Environment.TickCount;
                        }
                        await FMCoreTools.AsyncTask.Delay(1);
                        await FMCoreTools.AsyncTask.Yield();
                    }
                    await FMCoreTools.AsyncTask.Delay(1);
                    await FMCoreTools.AsyncTask.Yield();

                    if (_length == 0)
                    {
                        if (_stream != null)
                        {
                            try { _stream.Close(); }
                            catch (Exception e) { DebugLog(e.Message); }
                        }

                        if (_client != null)
                        {
                            try { _client.Close(); }
                            catch (Exception e) { DebugLog(e.Message); }
                        }

                        for (int i = 0; i < tcpServer_Clients.Count; i++)
                        {
                            if (_client == tcpServer_Clients[i])
                            {
                                tcpServer_Streams.Remove(tcpServer_Streams[i]);
                                tcpServer_Clients.Remove(tcpServer_Clients[i]);
                            }
                        }
                    }
                }
            }
            #endregion

            #region TCP_Client_Receiver
            private bool tcpClientCreated = false;
            private TcpClient tcpClient_Receiver;
            private NetworkStream tcpClient_Stream;
            private async void NetworkClientStartTCPReceiverAsync()
            {
                if (!tcpClientCreated)
                {
                    tcpClientCreated = true;

                    //Connect to TCP Server using Async connection method
                    TCPClient_ConnectAndReceiveAsync();

                    while (!stoppedOrCancelled())
                    {
                        ReceivedCount = _appendQueueReceivedBytes.Count;
                        while (_appendQueueReceivedBytes.Count > 0)
                        {
                            if (_appendQueueReceivedBytes.TryDequeue(out byte[] receivedBytes))
                            {
                                Manager.OnReceivedByteDataEvent.Invoke(receivedBytes);
                            }
                        }
                        await FMCoreTools.AsyncTask.Delay(1);
                        await FMCoreTools.AsyncTask.Yield();
                    }
                }
            }

            private async void TCPClient_ConnectAndReceiveAsync()
            {
                try
                {
                    tcpClient_Receiver = new TcpClient();
                    await tcpClient_Receiver.ConnectAsync(IPAddress.Parse(ServerIP), ClientListenPort);
                    if (!tcpClient_Receiver.Connected)
                    {
                        //try reconnect
                        tcpClient_Receiver.Close();
                        TCPClient_ConnectAndReceiveAsync();
                    }
                    else
                    {
                        tcpClient_Stream = tcpClient_Receiver.GetStream();
                        while (!stoppedOrCancelled())
                        {
                            await FMCoreTools.AsyncTask.Delay(1);

                            //readTCPDataByteArray
                            byte[] bytes = new byte[300000];
                            int _length;
                            while ((_length = await tcpClient_Stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                            {
                                if (_length > 0)
                                {
                                    byte[] _s = new byte[_length];
                                    Buffer.BlockCopy(bytes, 0, _s, 0, _length);
                                    _appendQueueReceivedBytes.Enqueue(_s);
                                    LastReceivedTimeMS = Environment.TickCount;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
                finally
                {
                    tcpClient_Receiver.Close();
                }
            }
            #endregion

            #region UDP
            private async void UdpReceiveAsync()
            {
                FMUDPListenerType _udpListenerType = UDPListenerType;
                await Task.Run(async () =>
                {
                    while (!stoppedOrCancelled())
                    {
                        try
                        {
                            if (udpClient_Listener == null)
                            {
                                udpClient_Listener = new UdpClient(ClientListenPort, AddressFamily.InterNetwork);
                                udpClient_Listener.Client.ReceiveTimeout = 2000;

                                switch (UDPTransferType)
                                {
                                    case FMUDPTransferType.Unicast: break;
                                    case FMUDPTransferType.MultipleUnicast: break;
                                    case FMUDPTransferType.Multicast:
                                        udpClient_Listener.MulticastLoopback = true;
                                        udpClient_Listener.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
                                        break;
                                    case FMUDPTransferType.Broadcast:
                                        udpClient_Listener.EnableBroadcast = true;
                                        break;
                                }

                                ServerEp = new IPEndPoint(IPAddress.Any, ClientListenPort);
                            }
                            if (_udpListenerType == FMUDPListenerType.Synchronous)
                            {
                                await FMCoreTools.AsyncTask.Delay(1);
                                while (!stoppedOrCancelled() && udpClient_Listener.Client.Poll(100, SelectMode.SelectRead))
                                {
                                    while (!stoppedOrCancelled() && udpClient_Listener.Client.Available > 0)
                                    {
                                        byte[] _receivedData = udpClient_Listener.Receive(ref ServerEp);

                                        //=======================Decode Data=======================
                                        _appendQueueReceivedBytes.Enqueue(_receivedData);
                                    }
                                }
                            }
                            else if (_udpListenerType == FMUDPListenerType.Asynchronous)
                            {
                                UdpReceiveResult _result = await udpClient_Listener.ReceiveAsync();
                                _appendQueueReceivedBytes.Enqueue(_result.Buffer);
                            }
                        }
                        catch
                        {
                            if (udpClient_Listener != null) udpClient_Listener.Close(); udpClient_Listener = null;
                        }
                    }
                });
            }
            private async void NetworkClientStartUDPListenerAsync()
            {
                LastReceivedTimeMS = Environment.TickCount;

                stop = false;
                await FMCoreTools.AsyncTask.Delay(500);

                BroadcastChecker();
                await FMCoreTools.AsyncTask.Delay(500);

                UdpReceiveAsync();
                //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Client Receiver ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

                while (!stoppedOrCancelled())
                {
                    ReceivedCount = _appendQueueReceivedBytes.Count;
                    while (_appendQueueReceivedBytes.Count > 0)
                    {
                        if (_appendQueueReceivedBytes.TryDequeue(out byte[] receivedBytes))
                        {
                            Manager.OnReceivedByteDataEvent.Invoke(receivedBytes);
                        }
                    }
                    await FMCoreTools.AsyncTask.Delay(1);
                    await FMCoreTools.AsyncTask.Yield();
                }
            }
            #endregion

            public void Action_StartClient() { StartAll(); }
            public void Action_StopClient() { StopAll(); }

            private long _initialised = 0;
            private bool initialised
            {
                get { return Interlocked.Read(ref _initialised) == 1; }
                set { Interlocked.Exchange(ref _initialised, Convert.ToInt64(value)); }
            }

            private async void StartAllDelayAsync(int inputDelayMS = 20)
            {
                await FMCoreTools.AsyncTask.Delay(inputDelayMS);
                if (!cancellationTokenSource_global.IsCancellationRequested) StartAll();
            }
            private void StartAll(int inputDelayMS = 0)
            {
                cancellationTokenSource_global = new CancellationTokenSource();

                if (inputDelayMS > 0)
                {
                    StartAllDelayAsync(inputDelayMS);
                    return;
                }

                if (initialised) return;
                initialised = true;

                stop = false;

                CurrentSeenTimeMS = Environment.TickCount;
                if (CurrentSeenTimeMS > 0)
                {
                    LastSentTimeMS = CurrentSeenTimeMS - connectionThreshold;
                    LastReceivedTimeMS = CurrentSeenTimeMS - connectionThreshold;
                }
                else
                {
                    LastSentTimeMS = int.MaxValue - connectionThreshold;
                    LastReceivedTimeMS = int.MaxValue - connectionThreshold;
                }

                if (DataStreamType == FMDataStreamType.Sender) { NetworkClientStartUDPSenderAsync(); }
                else
                {
                    switch (Protocol)
                    {
                        case FMProtocol.UDP: NetworkClientStartUDPListenerAsync(); break;
                        case FMProtocol.TCP:
                            if (TCPSocketType == FMTCPSocketType.TCPServer)
                            {
                                NetworkServerStartTCPListenerAsync();
                            }
                            else if (TCPSocketType == FMTCPSocketType.TCPClient)
                            {
                                NetworkClientStartTCPReceiverAsync();
                            }
                            break;
                    }
                }
            }

            private void StopAll()
            {
                initialised = false;

                //skip, if stopped already
                if (stop)
                {
                    StopAllAsync();//stop all Async again, just in case
                    return;
                }

                stop = true;

                if (DataStreamType == FMDataStreamType.Receiver)
                {
                    switch (Protocol)
                    {
                        case FMProtocol.UDP:
                            StopAllAsync();
                            if (udpClient_Listener != null)
                            {
                                try { udpClient_Listener.Close(); }
                                catch (Exception e) { DebugLog(e.Message); }
                                udpClient_Listener = null;
                            }
                            break;
                        case FMProtocol.TCP:
                            if (TCPSocketType == FMTCPSocketType.TCPServer)
                            {
                                tcpServerCreated = false; //just in case, for reset
                                foreach (TcpClient client in tcpServer_Clients)
                                {
                                    if (client != null)
                                    {
                                        try { client.Close(); }
                                        catch (Exception e) { DebugLog(e.Message); }
                                    }
                                    IsConnected = false;
                                }
                            }
                            else if (TCPSocketType == FMTCPSocketType.TCPClient)
                            {
                                tcpClientCreated = false; //just in case, for reset
                                if (tcpClient_Receiver != null)
                                {
                                    if (tcpClient_Stream != null)
                                    {
                                        try { tcpClient_Stream.Close(); ; }
                                        catch (Exception e) { DebugLog(e.Message); }
                                    }
                                    if (tcpClient_Receiver != null)
                                    {
                                        try { tcpClient_Receiver.Close(); ; }
                                        catch (Exception e) { DebugLog(e.Message); }
                                    }
                                }
                                IsConnected = false;
                            }
                            break;
                    }
                }
                else
                {
                    StopAllAsync();
                    _appendSendBytes = new ConcurrentQueue<byte[]>();
                }
            }

            // Start is called before the first frame update
            private void Start() { StartAll(); }
            private void Update()
            {
                CurrentSeenTimeMS = Environment.TickCount;
                IsConnected = EnvironmentTickCountDelta(CurrentSeenTimeMS, LastReceivedTimeMS) < connectionThreshold;
            }

            public bool ShowLog { get { return Manager.ShowLog; } }
            public void DebugLog(string _value) { if (ShowLog) Debug.Log(_value); }

            private CancellationTokenSource cancellationTokenSource_global;
            private bool stoppedOrCancelled() { return stop || cancellationTokenSource_global.IsCancellationRequested; }
            private void StopAllAsync()
            {
                if (cancellationTokenSource_global != null)
                {
                    if (!cancellationTokenSource_global.IsCancellationRequested) cancellationTokenSource_global.Cancel();
                }
            }

            private void OnApplicationQuit() { StopAll(); }
            private void OnDisable() { StopAll(); }
            private void OnDestroy() { StopAll(); }
            private void OnEnable() { StartAll(20); } //this may cause error on android

            private bool isPaused = false;
            private bool isPaused_old = false;
            private long _needResetFromPaused = 0;
            private bool needResetFromPaused
            {
                get { return Interlocked.Read(ref _needResetFromPaused) == 1; }
                set { Interlocked.Exchange(ref _needResetFromPaused, Convert.ToInt64(value)); }
            }

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP)
            //try fixing Android/Mobile connection issue after a pause...
            //some devices will trigger OnApplicationPause only, when some devices will trigger both...etc
            private void ResetFromPause()
            {
                if (!needResetFromPaused) return;
                needResetFromPaused = false;

                StopAll();
                StartAll(20);
            }
            private void OnApplicationPause(bool pause)
            {
                if (!initialised) return; //ignore it if not initialised yet

                isPaused_old = isPaused;
                isPaused = pause;
                if (isPaused && !isPaused_old) needResetFromPaused = true;
                if (!isPaused && isPaused_old) ResetFromPause();
            }
            private void OnApplicationFocus(bool focus)
            {
                if (!initialised) return; //ignore it if not initialised yet

                isPaused_old = isPaused;
                isPaused = !focus;
                if (isPaused && !isPaused_old) needResetFromPaused = true;
                if (!isPaused && isPaused_old) ResetFromPause();
            }
#endif
        }
    }
}
