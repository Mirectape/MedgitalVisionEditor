using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FMSolution.FMNetwork
{
    public class FMServer
    {
        public class FMServerComponent : MonoBehaviour
        {
            private int udpSendBufferSize = 1024 * 65; //max 65535
            private int udpReceiveBufferSize = 1024 * 512; //max 2147483647

            [HideInInspector] public FMNetworkManager Manager;

            [HideInInspector] public int ServerListenPort = 3333;
            [HideInInspector] public int ClientListenPort = 3334;

            public bool SupportMulticast = true;
            public string MulticastAddress = "239.255.255.255";
            private UdpClient mcastServer;
            private IPEndPoint mcastEndPoint;

            private void SendMulticast(byte[] _byte)
            {
                try
                {
                    if (mcastServer == null)
                    {
                        mcastServer = new UdpClient(ClientListenPort);
                        mcastServer.Client.SendBufferSize = udpSendBufferSize;
                        mcastServer.Client.ReceiveBufferSize = udpReceiveBufferSize;
                        mcastServer.Client.SendTimeout = 500;

                        mcastServer.MulticastLoopback = true;
                        mcastServer.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));

                        mcastEndPoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), ClientListenPort);
                    }
                    mcastServer.Send(_byte, _byte.Length, mcastEndPoint);
                }
                catch
                {
                    if (mcastServer != null)
                    {
                        mcastServer.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
                        mcastServer.Close();
                        mcastServer = null;
                    }
                }
            }

            [Serializable]
            public class ConnectedClient
            {
                private int udpSendBufferSize = 1024 * 65; //max 65535
                private int udpReceiveBufferSize = 1024 * 512; //max 2147483647

                public string IP;
                public int Port;

                public ConcurrentQueue<byte[]> AppendQueueAck = new ConcurrentQueue<byte[]>();
                public ConcurrentQueue<FMPacket> AppendQueueRetryPacket = new ConcurrentQueue<FMPacket>();
                public ConcurrentQueue<FMPacket> AppendQueueMissingPacket = new ConcurrentQueue<FMPacket>();

                private int _getSyncID = 0;
                private uint syncIDMax = UInt16.MaxValue - 1024;
                public UInt16 getSyncID
                {
                    get
                    {
                        _getSyncID = Interlocked.Increment(ref _getSyncID);
                        if (_getSyncID >= syncIDMax) _getSyncID = Interlocked.Exchange(ref _getSyncID, 1);
                        return (UInt16)_getSyncID;
                    }
                }

                private long _lastSeenTimeMS = 0;
                public int LastSeenTimeMS
                {
                    get { return (int)Interlocked.Read(ref _lastSeenTimeMS); }
                    set { Interlocked.Exchange(ref _lastSeenTimeMS, (long)value); }
                }
                private long _lastSentTimeMS = 0;
                public int LastSentTimeMS
                {
                    get { return Convert.ToInt32(Interlocked.Read(ref _lastSentTimeMS)); }
                    set { Interlocked.Exchange(ref _lastSentTimeMS, (long)value); }
                }

                public UdpClient Client;
                public void SendHandShaking() { Send(new byte[] { (byte)FMNetworkSignal.handshake }); }
                public void Send(byte[] _byte)
                {
                    try
                    {
                        if (Client == null)
                        {
                            Client = new UdpClient();
                            Client.Client.SendBufferSize = udpSendBufferSize;
                            Client.Client.ReceiveBufferSize = udpReceiveBufferSize;
                            Client.Client.SendTimeout = 500;
                            Client.EnableBroadcast = true;
                        }
                        Client.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Parse(IP), Port));
                        LastSentTimeMS = Environment.TickCount;
                    }
                    catch { Close(); }
                }

                public int sendBufferSize = 0;
                public int sendBufferThreshold = 1024 * 128;
                public int sendBufferThresholdMin = 1024 * 8;
                public int sendBufferThresholdMax = 1024 * 128;

                public void Send(FMPacket _packet)
                {
                    sendBufferSize += _packet.SendByte.Length;
                    if (sendBufferSize > sendBufferThreshold)
                    {
                        sendBufferSize = 0;
                        GC.Collect();
                        System.Threading.Thread.Sleep(1);
                    }

                    Send(_packet.SendByte);
                    if (_packet.Reliable) AddRetryPacket(_packet);
                }

                public void AddRetryPacket(FMPacket _packet)
                {
                    _packet.syncID = getSyncID;
                    Buffer.BlockCopy(BitConverter.GetBytes(_packet.syncID), 0, _packet.SendByte, 2, 2);

                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = IP;
                    _packet.SendByte[1] = 3; //target ip

                    byte[] _ip = IPAddress.Parse(IP).GetAddressBytes();
                    Buffer.BlockCopy(_ip, 0, _packet.SendByte, 4, _ip.Length);

                    AppendQueueRetryPacket.Enqueue(_packet);
                }

                public void Close()
                {
                    AppendQueueAck = new ConcurrentQueue<byte[]>();
                    AppendQueueMissingPacket = new ConcurrentQueue<FMPacket>();
                    AppendQueueRetryPacket = new ConcurrentQueue<FMPacket>();
                    GC.Collect();
                }
            }

            public bool IsConnected = false;
            public int ConnectionCount = 0;
            public List<ConnectedClient> ConnectedClients = new List<ConnectedClient>();
            public List<string> ConnectedIPs = new List<string>();

            public void Action_CheckClientStatus(string _ip, FMNetworkSignal _signal = FMNetworkSignal.none, FMAckSignal _ack = FMAckSignal.none, byte[] _ackResponseByte = null, UInt16 _verifiedAckID = 0)
            {
                bool _isExistedClient = false;
                int _matchedIndex = 0;
                for (int i = 0; i < ConnectedClients.Count; i++)
                {
                    if (_ip == ConnectedClients[i].IP)
                    {
                        if (_signal == FMNetworkSignal.close)
                        {
                            //remove client immediately, when received CLOSE SIGNAL
                            ConnectedClients[i].Close();
                            _appendQueueDisconnectedClient.Enqueue(ConnectedClients[i].IP);

                            ConnectedClients.Remove(ConnectedClients[i]);
                            ConnectedIPs.Remove(ConnectedIPs[i]);
                        }
                        else
                        {
                            _isExistedClient = true;
                            ConnectedClients[i].IP = _ip;
                            ConnectedClients[i].LastSeenTimeMS = Environment.TickCount;

                            _matchedIndex = i;
                        }
                    }
                }

                if (_isExistedClient)
                {
                    switch (_ack)
                    {
                        case FMAckSignal.none: break;
                        case FMAckSignal.ackRespone:
                            ConnectedClients[_matchedIndex].AppendQueueAck.Enqueue(_ackResponseByte);
                            break;
                        case FMAckSignal.ackReceived:
                            if (ConnectedClients[_matchedIndex].AppendQueueRetryPacket.Count > 0)
                            {
                                bool _completed = false;
                                if (_verifiedAckID == 0) _completed = true;
                                while (!_completed)
                                {
                                    if (ConnectedClients[_matchedIndex].AppendQueueRetryPacket.Count <= 0)
                                    {
                                        _completed = true;
                                    }
                                    else
                                    {
                                        if (ConnectedClients[_matchedIndex].AppendQueueRetryPacket.TryDequeue(out FMPacket retryPacket))
                                        {
                                            UInt16 _syncID = BitConverter.ToUInt16(retryPacket.SendByte, 2);
                                            if (_syncID == _verifiedAckID)
                                            {
                                                _completed = true;
                                            }
                                            else
                                            {
                                                ConnectedClients[_matchedIndex].AppendQueueMissingPacket.Enqueue(retryPacket);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }

                switch (_signal)
                {
                    case FMNetworkSignal.handshake:
                        if (!_isExistedClient)
                        {
                            //register new client
                            ConnectedClient NewClient = new ConnectedClient();
                            NewClient.IP = _ip;
                            NewClient.Port = ClientListenPort;
                            NewClient.LastSeenTimeMS = Environment.TickCount;

                            //for handshaking signal
                            NewClient.SendHandShaking();

                            ConnectedClients.Add(NewClient);
                            ConnectedIPs.Add(NewClient.IP);
                            _appendQueueConnectedClient.Enqueue(NewClient.IP);
                        }
                        break;
                    case FMNetworkSignal.close:
                        //closed in for-loop already
                        break;
                }
            }

            private int EnvironmentTickCountDelta(int currentMS, int lastMS)
            {
                if (currentMS < 0 && lastMS > 0) return Mathf.Abs(currentMS - int.MinValue) + (int.MaxValue - lastMS);
                return currentMS - lastMS;
            }

            private long _currentSeenTimeMS = 0;
            public int CurrentSeenTimeMS
            {
                get { return Convert.ToInt32(Interlocked.Read(ref _currentSeenTimeMS)); }
                set { Interlocked.Exchange(ref _currentSeenTimeMS, (long)value); }
            }
            private ConcurrentQueue<string> _appendQueueConnectedClient = new ConcurrentQueue<string>();
            private ConcurrentQueue<string> _appendQueueDisconnectedClient = new ConcurrentQueue<string>();

            public int CmdLength;

            private ConcurrentQueue<FMPacket> _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
            private ConcurrentQueue<FMPacket> _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();

            public void Action_CloseClientConnection(string _targetIP)
            {
                FMPacket _packet = new FMPacket();
                _packet.Reliable = false;
                _packet.SendByte = new byte[] { (byte)FMNetworkSignal.close };
                _packet.SendType = FMSendType.TargetIP;
                _packet.TargetIP = _targetIP;
                _appendQueueSendPacket.Enqueue(_packet);
            }

            public void Action_AddPacket(byte[] _byteData, FMSendType _type, FMPacketDataType _dataType, bool _reliable)
            {
                byte[] _meta = new byte[4];
                _meta[0] = (byte)_dataType; //_meta[0] = 0;//raw byte

                if (_type == FMSendType.All) _meta[1] = 0;//all clients
                if (_type == FMSendType.Server) _meta[1] = 1;//all clients
                if (_type == FMSendType.Others) _meta[1] = 2;//skip sender

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = _type;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }
            public void Action_AddPacket(string _stringData, FMSendType _type, FMPacketDataType _dataType, bool _reliable)
            {
                byte[] _byteData = Encoding.ASCII.GetBytes(_stringData);

                byte[] _meta = new byte[4];
                _meta[0] = (byte)_dataType; //_meta[0] = 1;//string data

                if (_type == FMSendType.All) _meta[1] = 0;//all clients
                if (_type == FMSendType.Server) _meta[1] = 1;//all clients
                if (_type == FMSendType.Others) _meta[1] = 2;//skip sender

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = _type;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            public void Action_AddPacket(byte[] _byteData, string _targetIP, FMPacketDataType _dataType, bool _reliable)
            {
                byte[] _meta = new byte[4];
                _meta[0] = (byte)_dataType; //_meta[0] = 0;//raw byte
                _meta[1] = 3;//target ip

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = _targetIP;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }
            public void Action_AddPacket(string _stringData, string _targetIP, FMPacketDataType _dataType,  bool _reliable)
            {
                byte[] _byteData = Encoding.ASCII.GetBytes(_stringData);

                byte[] _meta = new byte[4];
                _meta[0] = (byte)_dataType; //_meta[0] = 1;//string data
                _meta[1] = 3;//target ip

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = _targetIP;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            public void Action_AddPacket(FMPacket _packet)
            {
                //if (_appendQueueSendPacket.Count < 120)
                {
                    if (BitConverter.ToUInt16(_packet.SendByte, 2) != 0) _packet.Reliable = true;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            private long _stop = 0;
            private bool stop
            {
                get { return Interlocked.Read(ref _stop) == 1; }
                set { Interlocked.Exchange(ref _stop, Convert.ToInt64(value)); }
            }

            private void Start() { StartAll(); }

            public void Action_StartServer()
            {
                NetworkServerStartAsync();
                BroadcastCheckerAsync();
            }

            [Header("[Experimental] for supported devices only")]
            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Asynchronous;
            [Header("[Experimental] suggested for mobile")]
            public bool UseMainThreadSender = false;

            private UdpClient serverListener;
            private IPEndPoint clientEp;

            private void InitializeServerListener()
            {
                serverListener = new UdpClient(ServerListenPort);
                serverListener.Client.SendBufferSize = udpSendBufferSize;
                serverListener.Client.ReceiveBufferSize = udpReceiveBufferSize;
                serverListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                serverListener.Client.EnableBroadcast = true;
                //serverListener.Client.ReceiveTimeout = 2000;

                clientEp = new IPEndPoint(IPAddress.Any, ServerListenPort);
            }
            //private int EnvironmentTickCountDelta(int currentMS)
            //{
            //    int _gap = 0;
            //    if (currentMS < 0 && lastMS > 0)
            //    {
            //        _gap = Mathf.Abs(currentMS - int.MinValue) + (int.MaxValue - lastMS);
            //    }
            //    else
            //    {
            //        _gap = currentMS - lastMS;
            //    }
            //    return _gap;
            //}
            private UdpClient BroadcastClient = new UdpClient();
            private async void BroadcastCheckerAsync()
            {
                while (!stoppedOrCancelled())
                {
                    await FMCoreTools.AsyncTask.Yield();
                    if (BroadcastClient == null)
                    {
                        BroadcastClient = new UdpClient();
                        BroadcastClient.Client.SendTimeout = 200;
                        BroadcastClient.EnableBroadcast = true;
                    }

                    try { await BroadcastClient.SendAsync(new byte[] { (byte)FMNetworkSignal.handshake }, 1, new IPEndPoint(IPAddress.Parse(Manager.ReadBroadcastAddress), ClientListenPort)); }
                    catch { if (BroadcastClient != null) BroadcastClient.Close(); BroadcastClient = null; }

                    await FMCoreTools.AsyncTask.Delay(5000);//delay 5ms
                }
            }

            private async void ServerReceiverAsync()
            {
                FMUDPListenerType _udpListenerType = UDPListenerType;
                await Task.Run(async () =>
                {
                    while (!stoppedOrCancelled())
                    {
                        try
                        {
                            if (serverListener == null) InitializeServerListener();
                            if (_udpListenerType == FMUDPListenerType.Synchronous)
                            {
                                await FMCoreTools.AsyncTask.Delay(1);
                                while (!stoppedOrCancelled() && serverListener.Client.Poll(100, SelectMode.SelectRead))
                                {
                                    while (!stoppedOrCancelled() && serverListener.Client.Available > 0)
                                    {
                                        //=======================Queue Received Data=======================
                                        byte[] _receivedData = serverListener.Receive(ref clientEp);
                                        UdpClientReceiver(_receivedData, clientEp);
                                    }
                                }
                            }
                            else if (_udpListenerType == FMUDPListenerType.Asynchronous)
                            {
                                UdpReceiveResult _result = await serverListener.ReceiveAsync();
                                UdpClientReceiver(_result.Buffer, _result.RemoteEndPoint);
                            }
                        }
                        catch
                        {
                            if (serverListener != null) serverListener.Close(); serverListener = null;
                        }
                    }
                });
            }

            private void UdpClientReceiver(byte[] inputReceivedData, IPEndPoint inputClientEndPoint)
            {
                //=======================Queue Received Data=======================
                int _receivedDataLength = inputReceivedData.Length;
                string _clientIP = inputClientEndPoint.Address.ToString();
                if (_receivedDataLength > 4)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.SendByte = inputReceivedData;

                    //others, skip sender
                    if (inputReceivedData[1] == 2) _packet.SkipIP = _clientIP;
                    _appendQueueReceivedPacket.Enqueue(_packet);
                }
                //=======================Queue Received Data=======================

                //=======================Check is new client?=======================
                FMNetworkSignal _signal = FMNetworkSignal.none;
                FMAckSignal _ack = FMAckSignal.none;
                byte[] ackResponseByte = null;
                if (_receivedDataLength == 1)
                {
                    //Received Auto Network Discovery signal from Client
                    if (inputReceivedData[0] == 93) _signal = FMNetworkSignal.handshake;
                    if (inputReceivedData[0] == 94) _signal = FMNetworkSignal.close;
                }

                UInt16 _verifiedAckID = 0;
                if (inputReceivedData.Length > 4)
                {
                    //ack send queue
                    if (inputReceivedData[2] != 0 && inputReceivedData[3] != 0)
                    {
                        //_appendQueueAck.Enqueue(new byte[] { ReceivedData[2], ReceivedData[3] });
                        _ack = FMAckSignal.ackRespone;
                        ackResponseByte = new byte[] { inputReceivedData[2], inputReceivedData[3] };
                    }
                }
                else if (_receivedDataLength <= 2)
                {
                    _ack = FMAckSignal.ackReceived;
                    if (_receivedDataLength == 2) _verifiedAckID = BitConverter.ToUInt16(inputReceivedData, 0);
                }

                Action_CheckClientStatus(_clientIP, _signal, _ack, ackResponseByte, _verifiedAckID);
                //=======================Check is new client?=======================
            }

            private async void NetworkServerStartAsync()
            {
                stop = false;

                await FMCoreTools.AsyncTask.Yield();

                ServerReceiverAsync();
                ServerSenderAsync();

                //processing
                while (!stoppedOrCancelled())
                {
                    CurrentSeenTimeMS = Environment.TickCount;

                    #region Check Connection Status
                    IsConnected = (ConnectionCount > 0) ? true : false;
                    CmdLength = _appendQueueSendPacket.Count;

                    while (_appendQueueConnectedClient.Count > 0)
                    {
                        if (_appendQueueConnectedClient.TryDequeue(out string connectedClient))
                        {
                            Manager.OnClientConnected(connectedClient);
                            Manager.OnConnectionCountChanged(ConnectionCount);
                        }
                    }
                    while (_appendQueueDisconnectedClient.Count > 0)
                    {
                        if (_appendQueueDisconnectedClient.TryDequeue(out string disconnectedClient))
                        {
                            Manager.OnClientDisconnected(disconnectedClient);
                            Manager.OnConnectionCountChanged(ConnectionCount);
                        }
                    }
                    #endregion

                    while (_appendQueueReceivedPacket.Count > 0)
                    {
                        if (_appendQueueReceivedPacket.TryDequeue(out FMPacket _packet))
                        {
                            if (Manager != null)
                            {
                                byte[] _receivedData = _packet.SendByte;
                                if (_receivedData.Length > 4)
                                {
                                    byte[] _meta = new byte[] { _receivedData[0], _receivedData[1], 0, 0 };
                                    if (_meta[1] == 3)
                                    {
                                        //Send to TargetIP, contains 4 bytes ip after meta data
                                        _packet.TargetIP = new IPAddress(new byte[] { _receivedData[4], _receivedData[5], _receivedData[6], _receivedData[7] }).ToString();
                                        byte[] _data = new byte[_receivedData.Length - 6];
                                        Buffer.BlockCopy(_receivedData, 8, _data, 0, _data.Length);

                                        if (_packet.TargetIP == Manager.ReadLocalIPAddress)
                                        {
                                            //process received data>> byte data: 0, string msg: 1, network object data: 2
                                            switch (_meta[0])
                                            {
                                                case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                                case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                                case 2: Manager.OnReceivedNetworkTransform(_data); break;
                                                case 11:
                                                    try
                                                    {
                                                        FMNetworkFunction _fmfunction = JsonUtility.FromJson<FMNetworkFunction>(Encoding.ASCII.GetString(_data));
                                                        if (_fmfunction != null) Manager.OnReceivedNetworkFunction(_fmfunction);
                                                    }
                                                    catch { }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            //redirect the data to target IP
                                            if (_packet.TargetIP != Manager.ReadLocalIPAddress)
                                            {
                                                _packet.SendType = FMSendType.TargetIP;
                                                _packet.SendByte = new byte[_meta.Length + _data.Length];
                                                _packet.SendByte[0] = _meta[0]; _packet.SendByte[1] = _meta[1];
                                                Buffer.BlockCopy(_data, 0, _packet.SendByte, 4, _data.Length);
                                                Action_AddPacket(_packet);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        byte[] _data = new byte[_receivedData.Length - 4];
                                        Buffer.BlockCopy(_receivedData, 4, _data, 0, _data.Length);

                                        //process received data>> byte data: 0, string msg: 1
                                        switch (_meta[0])
                                        {
                                            case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                            case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                            case 11:
                                                try
                                                {
                                                    FMNetworkFunction _fmfunction = JsonUtility.FromJson<FMNetworkFunction>(Encoding.ASCII.GetString(_data));
                                                    if (_fmfunction != null) Manager.OnReceivedNetworkFunction(_fmfunction);
                                                }
                                                catch { }
                                                break;
                                        }

                                        //check send type
                                        switch (_meta[1])
                                        {
                                            //send to all, redirect msg to all other clients
                                            case 0: Action_AddPacket(_packet); break;
                                            //send to server only, do not need to do anything
                                            case 1: break;
                                            //skip sender
                                            case 2: Action_AddPacket(_packet); break;
                                        }
                                    }
                                }

                                Manager.GetRawReceivedData.Invoke(_receivedData);
                            }
                        }
                    }
                    await FMCoreTools.AsyncTask.Delay(1);
                    await FMCoreTools.AsyncTask.Yield();
                }
            }

            private async void ServerSenderAsync()
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
                ConnectionCount = ConnectedClients.Count;
                for (int i = ConnectionCount - 1; i >= 0; i--)
                {
                    bool _active = EnvironmentTickCountDelta(CurrentSeenTimeMS, ConnectedClients[i].LastSeenTimeMS) < 3000;
                    if (_active == false)
                    {
                        //remove it if didn't receive any data from client for 3000 ms
                        ConnectedClients[i].Close();
                        _appendQueueDisconnectedClient.Enqueue(ConnectedClients[i].IP);

                        ConnectedClients.Remove(ConnectedClients[i]);
                        ConnectedIPs.Remove(ConnectedIPs[i]);
                    }
                }
                ConnectionCount = ConnectedClients.Count;

                if (ConnectionCount > 0)
                {
                    for (int i = ConnectionCount - 1; i >= 0; i--)
                    {
                        if (ConnectedClients[i].AppendQueueAck.Count > 0 || ConnectedClients[i].AppendQueueMissingPacket.Count > 0)
                        {
                            //send queuedAck
                            int ackCount = 0;
                            while (ConnectedClients[i].AppendQueueAck.Count > 0 && ackCount < 100)
                            {
                                ackCount++;
                                if (ConnectedClients[i].AppendQueueAck.TryDequeue(out byte[] _ackBytes))
                                {
                                    ConnectedClients[i].Send(_ackBytes);
                                }
                            }

                            //check missing count
                            int missingCount = 0;
                            while (ConnectedClients[i].AppendQueueMissingPacket.Count > 0 && missingCount < 100)
                            {
                                missingCount++;
                                if (ConnectedClients[i].AppendQueueMissingPacket.TryDequeue(out FMPacket _missingPacket))
                                {
                                    _missingPacket.Reliable = true;
                                    SendPacket(_missingPacket);
                                }
                            }
                            ConnectedClients[i].sendBufferThreshold = missingCount > 0 ? ConnectedClients[i].sendBufferThresholdMin : ConnectedClients[i].sendBufferThresholdMax;
                        }
                    }
                }

                if (_appendQueueSendPacket.Count > 0)
                {
                    //limit 30 packet sent in each frame, solved overhead issue on receiver
                    int _k = 0;
                    //there are some commands in queue
                    //Debug.Log(_appendQueueSendPacket.Count);
                    while (_appendQueueSendPacket.Count > 0 && _k < 100)
                    {
                        _k++;
                        if (_appendQueueSendPacket.TryDequeue(out FMPacket _packet))
                        {
                            SendPacket(_packet);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < ConnectedClients.Count; i++)
                    {
                        //send empty byte for checking, after 1000 ms
                        if (EnvironmentTickCountDelta(CurrentSeenTimeMS, ConnectedClients[i].LastSentTimeMS) > 1000) ConnectedClients[i].SendHandShaking();
                    }
                }
            }

            private void SendPacket(FMPacket _packet)
            {
                if (_packet.SendType != FMSendType.TargetIP)
                {
                    if (SupportMulticast && !_packet.Reliable && _packet.SkipIP == null)
                    {
                        // if it's non-relaible packet, use multicast when supported
                        SendMulticast(_packet.SendByte);
                    }
                    else
                    {
                        // else, use default unicast per client
                        for (int i = 0; i < ConnectedClients.Count; i++)
                        {
                            if (ConnectedClients[i].IP != _packet.SkipIP) ConnectedClients[i].Send(_packet);
                        }
                    }
                }
                else
                {
                    //sending to target ip only
                    for (int i = 0; i < ConnectedClients.Count; i++)
                    {
                        if (ConnectedClients[i].IP == _packet.TargetIP)
                        {
                            ConnectedClients[i].Send(_packet);
                        }
                        else
                        {
                            if (EnvironmentTickCountDelta(CurrentSeenTimeMS, ConnectedClients[i].LastSentTimeMS) > 1000) ConnectedClients[i].SendHandShaking();
                        }
                    }
                }
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
            private void OnEnable() { StartAll(20); }

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

                if (inputDelayMS > 0f)
                {
                    StartAllDelayAsync(inputDelayMS);
                    return;
                }

                if (initialised) return;
                initialised = true;

                stop = false;
                Action_StartServer();
            }
            private void StopAll()
            {
                initialised = false;

                //skip, if stopped already
                if (stop)
                {
                    StopAllAsync();
                    return;
                }

                if (mcastServer != null)
                {
                    try
                    {
                        if(SupportMulticast) mcastServer.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
                        mcastServer.Close();
                    }
                    catch (Exception e) { DebugLog(e.Message); }
                    mcastServer = null;
                }

                if (IsConnected && serverListener != null)
                {
                    if (ConnectedClients.Count > 0)
                    {
                        foreach (ConnectedClient cc in ConnectedClients)
                        {
                            //send status "closed" in background before end...
                            SendServerClosedAsync(cc.IP, cc.Port);

                            cc.Close();
                            Manager.OnClientDisconnected(cc.IP);
                        }
                    }

                    try { serverListener.Close(); }
                    catch (Exception e) { DebugLog(e.Message); }
                    serverListener = null;
                }

                stop = true;
                IsConnected = false;
                StopAllAsync();

                if (ConnectedClients != null) ConnectedClients.Clear();
                ConnectedClients = new List<ConnectedClient>();
                _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
                _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();
            }

            private async void SendServerClosedAsync(string IP, int Port)
            {
                await FMCoreTools.AsyncTask.Yield();
                UdpClient _serverClose = new UdpClient();
                try
                {
                    _serverClose.Client.SendBufferSize = udpSendBufferSize;
                    _serverClose.Client.ReceiveBufferSize = udpReceiveBufferSize;
                    _serverClose.Client.SendTimeout = 500;
                    _serverClose.EnableBroadcast = true;

                    byte[] _byte = new byte[] { (byte)FMNetworkSignal.serverDown };
                    await _serverClose.SendAsync(_byte, _byte.Length, new IPEndPoint(IPAddress.Parse(IP), Port));
                }
                catch { }
            }
        }
    }
}