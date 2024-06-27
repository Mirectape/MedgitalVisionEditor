using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;


using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

namespace FMSolution.FMNetwork
{
    public enum FMProtocol { UDP, TCP }
    public enum FMNetworkType { Server, Client, DataStream }
    public enum FMSendType { All, Server, Others, TargetIP }
    public enum FMNetworkSignal
    {
        none = (byte)0,
        handshake = (byte)93,
        close = (byte)94,
        serverDown = (byte)95
    }
    public enum FMAckSignal { none, ackRespone, ackReceived }
    public enum FMPacketDataType { rawByte = (byte)0, stringData = (byte)1, networkTransform = (byte)2, networkFunction = (byte)11 }

    public enum FMDataStreamType { Receiver, Sender }
    public enum FMUDPTransferType { Unicast, MultipleUnicast, Multicast, Broadcast }
    public enum FMUDPListenerType { Synchronous, Asynchronous }
    public enum FMTCPSocketType { TCPServer, TCPClient }
    public struct FMPacket
    {
        public byte[] SendByte;
        public string SkipIP;
        public FMSendType SendType;
        public string TargetIP;

        public bool Reliable;
        public UInt16 syncID;
    }

    public enum FMNetworkTransformSyncType
    {
        None = 0,
        All = 255,
        PositionOnly = 1,
        RotationOnly = 2,
        ScaleOnly = 3,
        PositionAndRotation = 4,
        PositionAndScale = 5,
        RotationAndScale = 6,
    }
    public struct FMNetworkTransformSyncData
    {
        public int viewID;
        public bool isOwner;
        public FMNetworkTransformSyncType syncType;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    [AddComponentMenu("FMETP/Network/FMNetworkManager")]
    public class FMNetworkManager : MonoBehaviour
    {
        #region EditorProps

        public bool EditorShowNetworking = true;
        public bool EditorShowSyncTransformation = true;
        public bool EditorShowEvents = true;
        public bool EditorShowDebug = true;

        public bool EditorShowServerSettings = false;
        public bool EditorShowClientSettings = false;
        public bool EditorShowDataStreamSettings = false;

        public bool EditorShownetworkedObjects = false;

        public bool EditorShowReceiverEvents = false;
        public bool EditorShowConnectionEvents = false;
        #endregion

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        public string GetBroadcastAddress()
        {
            byte[] foundLocalIPBytes = IPAddress.Parse(ReadLocalIPAddress).GetAddressBytes();
            byte[] foundBroadcastAddress = IPAddress.Broadcast.GetAddressBytes();
            try
            {
                byte[] foundSubnetMaskBytes = GetSubnetMask(IPAddress.Parse(LocalIPAddress())).GetAddressBytes();
                foundBroadcastAddress = foundSubnetMaskBytes;

                byte[] outputBroadcastBytes = new byte[foundBroadcastAddress.Length];
                for (int i = 0; i < outputBroadcastBytes.Length; i++)
                {
                    outputBroadcastBytes[i] = foundBroadcastAddress[i] == (byte)255 ? foundLocalIPBytes[i] : (byte)255;
                }
                //calculate subnet broadcast address, as Mac OS may block the global broadcast 255.255.255.255
                return new IPAddress(outputBroadcastBytes).ToString();
            }
            catch
            {
                //use default broadcast address...
                return new IPAddress(foundBroadcastAddress).ToString();
            }
        }
        private string _BroadcastAddress;
        public string ReadBroadcastAddress
        {
            get
            {
                if (_BroadcastAddress == null) _BroadcastAddress = GetBroadcastAddress();
                if (_BroadcastAddress.Length <= 3) _BroadcastAddress = GetBroadcastAddress();
                return _BroadcastAddress;
            }
        }

        public string LocalIPAddress()
        {
            string localIP = "0.0.0.0";
            //ssIPHostEntry host;
            //host = Dns.GetHostEntry(Dns.GetHostName());

            List<string> detectedIPs = new List<string>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                //if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                //commented above condition, as it may not work on Android, found issues on Google Pixel Phones, its type returns "0" for unknown reason.
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (ip.IsDnsEligible)
                            {
                                string detectedIP = ip.Address.ToString();
                                if (detectedIP != "127.0.0.1" && detectedIP != "0.0.0.0")
                                {
                                    try
                                    {
                                        if (ip.AddressValidLifetime / 2 != int.MaxValue)
                                        {
                                            localIP = detectedIP;
                                        }
                                        else
                                        {
                                            //if didn't find any yet, this is the only one
                                            if (localIP == "0.0.0.0") localIP = detectedIP;
                                        }
                                    }
                                    catch
                                    {
                                        localIP = detectedIP;
                                    }

                                    detectedIPs.Add(localIP);
                                }
                            }
                        }
                    }
                }
            }

#if UNITY_EDITOR || UNITY_STANDALONE || WINDOWS_UWP
        if (detectedIPs.Count > 1)
        {
            string endPointIP = "0.0.0.0";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    endPointIP = endPoint.Address.ToString();
                    if (socket.Connected) socket.Disconnect(true);
                }
            }
            catch { }

            for (int i = 0; i < detectedIPs.Count; i++)
            {
                if (detectedIPs[i] == endPointIP) localIP = detectedIPs[i];
            }
        }
#endif
            return localIP;
        }

        private string _localIP;
        public string ReadLocalIPAddress
        {
            get
            {
                if (_localIP == null) _localIP = LocalIPAddress();
                if (_localIP.Length <= 3) _localIP = LocalIPAddress();
                return _localIP;
            }
        }

        public static FMNetworkManager instance;
        public bool AutoInit = true;

        public bool Initialised { get { return initialised; } set { initialised = value; } }
        private long _initialised = 0;
        private bool initialised
        {
            get { return Interlocked.Read(ref _initialised) == 1; }
            set { Interlocked.Exchange(ref _initialised, Convert.ToInt64(value)); }
        }

        [Tooltip("Initialise as Server or Client. Otherwise, using DataStream for general udp or tcp streaming from Gstreamer and StereoPi")]
        public FMNetworkType NetworkType;

        [HideInInspector] public FMServer.FMServerComponent Server;
        [HideInInspector] public FMClient.FMClientComponent Client;
        [HideInInspector] public FMDataStream.FMDataStreamComponent DataStream;

        [Serializable]
        public class FMServerSettings
        {
            public int ServerListenPort = 3333;

            [Tooltip("(( on supported devices only ))")]
            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Asynchronous;

            [Tooltip("(( suggested for low-end mobile, but not recommend for streaming large data ))")]
            public bool UseMainThreadSender = true;

            [Tooltip("(( use Multicast for Server to All Clients, for reducing server's loading))")]
            public bool SupportMulticast = false;

            [Tooltip("(( Multicast Address, cannot change during runtime))")]
            public string MulticastAddress = "239.255.255.255";

            public int ConnectionCount;
        }

        [Serializable]
        public class FMClientSettings
        {
            public int ClientListenPort = 3334;

            [Tooltip("(( on supported devices only ))")]
            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Asynchronous;

            [Tooltip("(( suggested for low-end mobile, but not recommend for streaming large data ))")]
            public bool UseMainThreadSender = true;

            [Tooltip("(( Experimental: broadcast data to all devices in local network, and this client will be discovered and registered by multiple servers. However, it's not reliable for important data. ))")]
            public bool ForceBroadcast = false;

            [Tooltip("(( Join Multicast Group))")]
            public bool SupportMulticast = false;
            [Tooltip("(( Multicast Address, cannot change during runtime))")]
            public string MulticastAddress = "239.255.255.255";

            [Tooltip("(( true by default ))")]
            public bool AutoNetworkDiscovery = true;
            [Tooltip("(( only applied when Auto Network Discovery is off ))")]
            public string ServerIP;
            public bool IsConnected;
        }

        [Serializable]
        public class FMDataStreamSettings
        {
            public FMDataStreamType DataStreamType = FMDataStreamType.Receiver;

            public FMProtocol DataStreamProtocol = FMProtocol.UDP;
            public int ClientListenPort = 3001;

            [Tooltip("(( UDP Listener Type))")]
            public FMUDPTransferType UDPTransferType = FMUDPTransferType.Unicast;
            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Synchronous;
            public FMTCPSocketType TCPSocketType = FMTCPSocketType.TCPServer;
            [Tooltip("(( Multicast Address, cannot change during runtime))")]
            public string MulticastAddress = "239.255.255.255";

            public bool IsConnected;

            //tcp client
            public string ServerIP = "127.0.0.1";

            //sender
            public string ClientIP = "127.0.0.1";
            public List<string> ClientIPList = new List<string>();
            public bool UseMainThreadSender = true;
        }

        [Tooltip("Network Settings for Server")]
        public FMServerSettings ServerSettings;
        [Tooltip("Network Settings for Client")]
        public FMClientSettings ClientSettings;
        [Tooltip("Network Settings for DataStream")]
        public FMDataStreamSettings DataStreamSettings;

        public bool DebugStatus = true;
        public bool ShowLog = true;
        [TextArea(1, 10)]
        public string Status;
        public Text UIStatus;

        public UnityEventByteArray OnReceivedByteDataEvent = new UnityEventByteArray();
        public UnityEventString OnReceivedStringDataEvent = new UnityEventString();
        public UnityEventByteArray GetRawReceivedData = new UnityEventByteArray();

        private List<GameObject> networkFunctionListenerList = new List<GameObject>();
        public void RegisterNetworkFunctionListener(GameObject inputGameObject)
        {
            if (!networkFunctionListenerList.Contains(inputGameObject)) networkFunctionListenerList.Add(inputGameObject);
        }
        public void UnregisterNetworkFunctionListener(GameObject inputGameObject)
        {
            if (networkFunctionListenerList.Contains(inputGameObject)) networkFunctionListenerList.Remove(inputGameObject);
        }
        //public List<GameObject> GetNetworkedObjects() { return networkedObjects; }
        public void OnReceivedNetworkFunction(FMNetworkFunction _fmfunction)
        {
            if (_fmfunction != null)
            {
                for (int i = 0; i < networkFunctionListenerList.Count; i++)
                {
                    try
                    {
                        networkFunctionListenerList[i].SendMessage(_fmfunction.FunctionName, _fmfunction.Variables != null ? _fmfunction.Variables : new object[0], SendMessageOptions.DontRequireReceiver);
                    }
                    catch { }
                }
            }
        }
        public void OnReceivedNetworkTransform(byte[] inputData)
        {
            Action_DecodeNetworkTransformView(inputData);
        }

        //server events
        public UnityEventString OnClientConnectedEvent = new UnityEventString();
        public UnityEventString OnClientDisconnectedEvent = new UnityEventString();
        public UnityEventInt OnConnectionCountChangedEvent = new UnityEventInt();
        public void OnClientConnected(string inputClientIP)
        {
            OnClientConnectedEvent.Invoke(inputClientIP);
            if (ShowLog) Debug.Log("OnClientConnected: " + inputClientIP);
        }
        public void OnClientDisconnected(string inputClientIP)
        {
            OnClientDisconnectedEvent.Invoke(inputClientIP);
            if (ShowLog) Debug.Log("OnClientDisonnected: " + inputClientIP);
        }
        public void OnConnectionCountChanged(int inputConnectionCount)
        {
            OnConnectionCountChangedEvent.Invoke(inputConnectionCount);
            if (ShowLog) Debug.Log("OnConnectionCountChanged: " + inputConnectionCount);
        }

        //client events
        public UnityEventString OnFoundServerEvent = new UnityEventString();
        public UnityEventString OnLostServerEvent = new UnityEventString();
        public void OnFoundServer(string ServerIP)
        {
            OnFoundServerEvent.Invoke(ServerIP);
            if (ShowLog) Debug.Log("OnFoundServer: " + ServerIP);
        }

        public void OnLostServer(string ServerIP)
        {
            OnLostServerEvent.Invoke(ServerIP);
            if (ShowLog) Debug.Log("OnLostServer: " + ServerIP);
        }

        #region Network Objects Setup
        private List<GameObject> networkedObjects = new List<GameObject>();
        public List<GameObject> GetNetworkedObjects() { return networkedObjects; }
        private Dictionary<int, FMNetworkTransformView> transformViewDictionary = new Dictionary<int, FMNetworkTransformView>();
        public Dictionary<int, FMNetworkTransformView> GetTransformViewDictionary() { return transformViewDictionary; }
        private int maximumTransformViewCount = 2000;

        public void RegisterFMNetworkID(FMNetworkTransformView inputTransformView)
        {
            if (NetworkType != FMNetworkType.Server && NetworkType != FMNetworkType.Client) return;
            if (transformViewDictionary.Count > maximumTransformViewCount)
            {
                if (ShowLog) Debug.LogError("reached maximum transform view count: " + maximumTransformViewCount);
                return;
            }

            transformViewDictionary.Add(inputTransformView.GetViewID(), inputTransformView);
            if (ShowLog) Debug.Log("register: view id: " + inputTransformView.GetViewID() + ", total:" + transformViewDictionary.Count);

            //update network object list
            if (!networkedObjects.Contains(inputTransformView.gameObject))
            {
                networkedObjects.Add(inputTransformView.gameObject);
                UpdateNetworkedObjectOwnersStatus();
            }
        }

        public void UnregisterFMNetworkID(FMNetworkTransformView inputTransformView)
        {
            if (NetworkType != FMNetworkType.Server && NetworkType != FMNetworkType.Client) return;
            if (transformViewDictionary.ContainsKey(inputTransformView.GetViewID()))
            {
                transformViewDictionary.Remove(inputTransformView.GetViewID());
            }

            if (ShowLog) Debug.Log("unregister: view id: " + inputTransformView.GetViewID() + ", total:" + transformViewDictionary.Count);

            //update network object list
            if (networkedObjects.Contains(inputTransformView.gameObject)) networkedObjects.Remove(inputTransformView.gameObject);
        }

        public Queue<FMNetworkTransformSyncData> AppendQueueTransformSyncData = new Queue<FMNetworkTransformSyncData>();
        public void Action_EnqueueTransformSyncData(FMNetworkTransformSyncData inputSyncData)
        {
            if (!enabled) return;
            if (!Initialised) return;
            if (ServerSettings.ConnectionCount == 0 && !ClientSettings.IsConnected) return;
            AppendQueueTransformSyncData.Enqueue(inputSyncData);
        }

        private void EncodeSyncData(FMNetworkTransformSyncData inputData, bool assignPosition, bool assignRotation, bool assignScale, ref List<byte> referenceByteList)
        {
            if (assignPosition)
            {
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.position.x));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.position.y));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.position.z));
            }
            if (assignRotation)
            {
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.rotation.x));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.rotation.y));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.rotation.z));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.rotation.w));
            }
            if (assignScale)
            {
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.localScale.x));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.localScale.y));
                referenceByteList.AddRange(BitConverter.GetBytes(inputData.localScale.z));
            }
        }

        //reserve range 0-999 for internal usage
        [Range(0, 999)] private UInt16 labelTransformView = 101;
        private void DequeueTransformSyncData()
        {
            if (AppendQueueTransformSyncData.Count == 0) return;

            byte[] _labelBytes = BitConverter.GetBytes(labelTransformView);
            byte[] _timestampBytes = BitConverter.GetBytes(Time.realtimeSinceStartup);

            //metadata: label(2) + timestamp(4)
            //syncData: viewID(4) + syncType(1) + position(12) + rotation(16) + scale(12)
            //4 + 1 + 12 + 16 + 12 = 45
            while (AppendQueueTransformSyncData.Count > 0)
            {
                List<byte> _sentBytes = new List<byte>();
                _sentBytes.AddRange(_labelBytes);
                _sentBytes.AddRange(_timestampBytes);

                while (_sentBytes.Count < 1350 && AppendQueueTransformSyncData.Count > 0)
                {
                    FMNetworkTransformSyncData _syncData = AppendQueueTransformSyncData.Dequeue();

                    _sentBytes.AddRange(BitConverter.GetBytes(_syncData.viewID));
                    _sentBytes.Add((byte)_syncData.syncType);

                    switch (_syncData.syncType)
                    {
                        case FMNetworkTransformSyncType.All: EncodeSyncData(_syncData, true, true, true, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.PositionOnly: EncodeSyncData(_syncData, true, false, false, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.RotationOnly: EncodeSyncData(_syncData, false, true, false, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.ScaleOnly: EncodeSyncData(_syncData, false, false, true, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.PositionAndRotation: EncodeSyncData(_syncData, true, true, false, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.PositionAndScale: EncodeSyncData(_syncData, true, false, true, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.RotationAndScale: EncodeSyncData(_syncData, false, true, true, ref _sentBytes); break;
                        case FMNetworkTransformSyncType.None: break;
                    }
                }
                if (_sentBytes.Count >= 17) SendNetworkTransformToOthers(_sentBytes.ToArray());
            }
        }

        private int DecodeSyncData(byte[] inputData, int inputOffset, bool decodePosition, bool decodeRotation, bool decodeScale, ref FMNetworkTransformSyncData referenceSyncData)
        {
            int _offset = inputOffset;
            if (decodePosition)
            {
                referenceSyncData.position.x = BitConverter.ToSingle(inputData, _offset);
                referenceSyncData.position.y = BitConverter.ToSingle(inputData, _offset + 4);
                referenceSyncData.position.z = BitConverter.ToSingle(inputData, _offset + 8);
                _offset += 12;
            }
            if (decodeRotation)
            {
                referenceSyncData.rotation.x = BitConverter.ToSingle(inputData, _offset);
                referenceSyncData.rotation.y = BitConverter.ToSingle(inputData, _offset + 4);
                referenceSyncData.rotation.z = BitConverter.ToSingle(inputData, _offset + 8);
                referenceSyncData.rotation.w = BitConverter.ToSingle(inputData, _offset + 12);
                _offset += 16;
            }
            if (decodeScale)
            {
                referenceSyncData.localScale.x = BitConverter.ToSingle(inputData, _offset);
                referenceSyncData.localScale.y = BitConverter.ToSingle(inputData, _offset + 4);
                referenceSyncData.localScale.z = BitConverter.ToSingle(inputData, _offset + 8);
                _offset += 12;
            }

            return _offset;
        }

        private void Action_DecodeNetworkTransformView(byte[] inputData)
        {
            int _count = inputData.Length;
            int _offset = 0;

            UInt16 _label = BitConverter.ToUInt16(inputData, _offset);
            _offset += 2;

            if (_label != labelTransformView) return;

            float Timestamp = BitConverter.ToSingle(inputData, _offset);
            _offset += 4;

            while (_offset < _count)
            {
                FMNetworkTransformSyncData _syncData = new FMNetworkTransformSyncData();
                _syncData.viewID = BitConverter.ToInt32(inputData, _offset);
                _syncData.syncType = (FMNetworkTransformSyncType)((int)inputData[_offset + 4]);
                _offset += 5;

                switch (_syncData.syncType)
                {
                    case FMNetworkTransformSyncType.All: _offset = DecodeSyncData(inputData, _offset, true, true, true, ref _syncData); break;
                    case FMNetworkTransformSyncType.PositionOnly: _offset = DecodeSyncData(inputData, _offset, true, false, false, ref _syncData); break;
                    case FMNetworkTransformSyncType.RotationOnly: _offset = DecodeSyncData(inputData, _offset, false, true, false, ref _syncData); break;
                    case FMNetworkTransformSyncType.ScaleOnly: _offset = DecodeSyncData(inputData, _offset, false, false, true, ref _syncData); break;
                    case FMNetworkTransformSyncType.PositionAndRotation: _offset = DecodeSyncData(inputData, _offset, true, true, false, ref _syncData); break;
                    case FMNetworkTransformSyncType.PositionAndScale: _offset = DecodeSyncData(inputData, _offset, true, false, true, ref _syncData); break;
                    case FMNetworkTransformSyncType.RotationAndScale: _offset = DecodeSyncData(inputData, _offset, false, true, true, ref _syncData); break;
                    case FMNetworkTransformSyncType.None:
                        break;
                }

                if (transformViewDictionary.TryGetValue(_syncData.viewID, out FMNetworkTransformView _transformView))
                {
                    _transformView.Action_UpdateSyncData(_syncData, Timestamp);
                }
            }
        }
        #endregion

        public void Action_InitAsServer() { NetworkType = FMNetworkType.Server; Init(); } 
        public void Action_InitAsClient() { NetworkType = FMNetworkType.Client; Init(); } 
        public void Action_InitDataStream() { NetworkType = FMNetworkType.DataStream; Init(); }
        public void Action_InitDataStream(string inputClientIP)
        {
            DataStreamSettings.ClientIP = inputClientIP;
            NetworkType = FMNetworkType.DataStream;
            Init();
        }

        /// <summary>
        /// Close connection locally, for either Server or Client
        /// </summary>
        public void Action_Close()
        {
            Initialised = false;
            if (Server != null) Destroy(Server);
            if (Client != null) Destroy(Client);
            if (DataStream != null) Destroy(DataStream);

            ServerSettings.ConnectionCount = 0;
            ClientSettings.IsConnected = false;
            ClientSettings.ServerIP = "";
            DataStreamSettings.IsConnected = false;

            UpdateDebugText();

            OnReceivedByteDataEvent.RemoveListener(Action_DecodeNetworkTransformView);
            GC.Collect();
        }

        /// <summary>
        /// Server Commands only, close client's connection remotely
        /// </summary>
        public void Action_CloseClientConnection(string _clientIP)
        {
            if (NetworkType != FMNetworkType.Server) return;
            if (!Server.IsConnected) return;
            Server.Action_CloseClientConnection(_clientIP);
        }

        /// <summary>
        /// Server Commands only, close all clients' connection remotely
        /// </summary>
        public void Action_CloseAllClientsConnection()
        {
            if (NetworkType != FMNetworkType.Server) return;
            if (!Server.IsConnected) return;

            if (ServerSettings.ConnectionCount > 0)
            {
                for (int i = 0; i < Server.ConnectedIPs.Count; i++)
                {
                    Server.Action_CloseClientConnection(Server.ConnectedIPs[i]);
                }
            }
        }

        private void Init()
        {
            if (Initialised) Action_Close();

            switch (NetworkType)
            {
                case FMNetworkType.Server:
                    Server = this.gameObject.AddComponent<FMServer.FMServerComponent>();
                    Server.hideFlags = HideFlags.HideInInspector;

                    Server.Manager = this;

                    Server.ServerListenPort = ServerSettings.ServerListenPort;
                    Server.ClientListenPort = ClientSettings.ClientListenPort;

                    Server.UDPListenerType = ServerSettings.UDPListenerType;
                    Server.UseMainThreadSender = ServerSettings.UseMainThreadSender;

                    Server.SupportMulticast = ServerSettings.SupportMulticast;
                    Server.MulticastAddress = ServerSettings.MulticastAddress;

                    OnReceivedByteDataEvent.AddListener(Action_DecodeNetworkTransformView);
                    break;
                case FMNetworkType.Client:
                    Client = this.gameObject.AddComponent<FMClient.FMClientComponent>();
                    Client.hideFlags = HideFlags.HideInInspector;

                    Client.Manager = this;

                    Client.ServerListenPort = ServerSettings.ServerListenPort;
                    Client.ClientListenPort = ClientSettings.ClientListenPort;

                    Client.UDPListenerType = ClientSettings.UDPListenerType;
                    Client.UseMainThreadSender = ClientSettings.UseMainThreadSender;
                    Client.AutoNetworkDiscovery = ClientSettings.AutoNetworkDiscovery;
                    if (ClientSettings.ServerIP == "") ClientSettings.ServerIP = "127.0.0.1";
                    if (!Client.AutoNetworkDiscovery) Client.ServerIP = ClientSettings.ServerIP;

                    Client.ForceBroadcast = ClientSettings.ForceBroadcast;
                    Client.SupportMulticast = ClientSettings.SupportMulticast;
                    Client.MulticastAddress = ClientSettings.MulticastAddress;

                    OnReceivedByteDataEvent.AddListener(Action_DecodeNetworkTransformView);
                    break;
                case FMNetworkType.DataStream:
                    DataStream = this.gameObject.AddComponent<FMDataStream.FMDataStreamComponent>();
                    DataStream.hideFlags = HideFlags.HideInInspector;

                    DataStream.Manager = this;

                    DataStream.DataStreamType = DataStreamSettings.DataStreamType;

                    DataStream.Protocol = DataStreamSettings.DataStreamProtocol;
                    DataStream.ClientListenPort = DataStreamSettings.ClientListenPort;
                    DataStream.UDPTransferType = DataStreamSettings.UDPTransferType;
                    DataStream.UDPListenerType = DataStreamSettings.UDPListenerType;
                    DataStream.TCPSocketType = DataStreamSettings.TCPSocketType;
                    DataStream.MulticastAddress = DataStreamSettings.MulticastAddress;

                    DataStream.UseMainThreadSender = DataStreamSettings.UseMainThreadSender;

                    break;
            }

            Initialised = true;
            UpdateNetworkedObjectOwnersStatus();
        }

        private void UpdateNetworkedObjectOwnersStatus()
        {
            if (NetworkType == FMNetworkType.Server || NetworkType == FMNetworkType.Client)
            {
                foreach (GameObject obj in networkedObjects)
                {
                    FMNetworkTransformView _view;
                    if (!obj.TryGetComponent(out _view)) _view = obj.AddComponent<FMNetworkTransformView>();

                    //by default, server is the master owner
                    if (NetworkType == FMNetworkType.Server && initialised) _view.IsOwner = true;
                }
            }
        }

        private void Awake()
        {
            Application.runInBackground = true;
            if (instance == null) instance = this;
        }

        //private void Awake()
        //{
        //    if (instance == null)
        //    {
        //        instance = this;
        //        this.gameObject.transform.parent = null;
        //        DontDestroyOnLoad(this.gameObject);
        //    }
        //    else
        //    {
        //        Destroy(this.gameObject);
        //    }
        //}

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

            if (Server != null)
            {
                Action_Close();
                Invoke(nameof(Action_InitAsServer), 1f);
            }
            else if (Client != null)
            {
                Action_Close();
                Invoke(nameof(Action_InitAsClient), 1f);
            }
            else if (DataStream != null)
            {
                Action_Close();
                Invoke(nameof(Action_InitDataStream), 1f);
            }
        }
        private void OnApplicationPause(bool pause)
        {
            if (!Initialised) return; //ignore it if not initialised yet

            isPaused_old = isPaused;
            isPaused = pause;
            if (isPaused && !isPaused_old) needResetFromPaused = true;
            if (!isPaused && isPaused_old) ResetFromPause();
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!Initialised) return; //ignore it if not initialised yet

            isPaused_old = isPaused;
            isPaused = !focus;
            if (isPaused && !isPaused_old) needResetFromPaused = true;
            if (!isPaused && isPaused_old) ResetFromPause();
        }
#endif

        private void OnEnable()
        {
            if (!Initialised) return;
            switch (NetworkType)
            {
                case FMNetworkType.Server: if (Server != null) Server.enabled = true;  break;
                case FMNetworkType.Client: if (Client != null) Client.enabled = true; break;
                case FMNetworkType.DataStream: if (DataStream != null) DataStream.enabled = true; break;
            }

            needResetFromPaused = false;
            UpdateDebugText();
        }
        private void OnDisable()
        {
            if (!Initialised) return;
            switch (NetworkType)
            {
                case FMNetworkType.Server: if (Server != null) Server.enabled = false; break;
                case FMNetworkType.Client: if (Client != null) Client.enabled = false; break;
                case FMNetworkType.DataStream: if (DataStream != null) DataStream.enabled = false; break;
            }

            needResetFromPaused = false;
            UpdateDebugText(true);
        }

        // Use this for initialization
        private void Start() { if (AutoInit) Init(); }

        private float syncTimer = 0f;
        private float syncFPS = 60f;
        private void LateUpdate()
        {
            if (Initialised == false) return;

            //network view sync(sender)
            syncTimer += Time.deltaTime;
            if (syncTimer > (1f / syncFPS))
            {
                syncTimer %= (1f / syncFPS);
                DequeueTransformSyncData();
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (Initialised == false) return;

            updateDebugTextTimer += Time.deltaTime;
            if (updateDebugTextTimer > updateDebugTextThreshold)
            {
                updateDebugTextTimer %= updateDebugTextThreshold;
                UpdateDebugText();

                //by default, those information will be updated via event
                //in case something wrong, this is the fallback solution
                switch (NetworkType)
                {
                    case FMNetworkType.Server: ServerSettings.ConnectionCount = Server.ConnectionCount; break;
                    case FMNetworkType.Client: ClientSettings.IsConnected = Client.IsConnected; break;
                    case FMNetworkType.DataStream: DataStreamSettings.IsConnected = DataStream.IsConnected; break;
                }
            }
        }

        private float updateDebugTextTimer = 0f;
        private float updateDebugTextThreshold = 0.03333f;
        private void UpdateDebugText(bool onNetworkManagerDisabled = false)
        {
            //====================Update Debug Text============================
            #region Debug Status
            if (DebugStatus)
            {
                string _status = "";
                //_status += "Thread: " + Loom.numThreads + " / " + Loom.maxThreads + "\n";
                _status += "Network Type: " + NetworkType.ToString() + "\n";
                _status += "Local IP: " + ReadLocalIPAddress + "\n";

                if (!onNetworkManagerDisabled)
                {
                    try
                    {
                        switch (NetworkType)
                        {
                            case FMNetworkType.Server:
                                _status += "Connection Count: " + ServerSettings.ConnectionCount + "\n"
                                    + "UDP Listener Type: " + ServerSettings.UDPListenerType + "\n"
                                    + "Use Main Thread Sender: " + ServerSettings.UseMainThreadSender + "\n";

                                for (int i = 0; i < Server.ConnectedClients.Count; i++)
                                {
                                    if (Server.ConnectedClients[i] != null)
                                    {
                                        _status += "connected ip: " + Server.ConnectedClients[i].IP + "\n"
                                            + "last seen: " + Server.ConnectedClients[i].LastSeenTimeMS + "\n"
                                            + "last send: " + Server.ConnectedClients[i].LastSentTimeMS + "\n";
                                    }
                                    else { _status += "Connected Client: null/unknown issue" + "\n"; }
                                }
                                break;
                            case FMNetworkType.Client:
                                _status += "Is Connected: " + ClientSettings.IsConnected + "\n"
                                    + "Use Main Thread Sender: " + ClientSettings.UseMainThreadSender + "\n";

                                if (ClientSettings.IsConnected)
                                {
                                    _status += "last send: " + Client.LastSentTimeMS + "\n"
                                        + "last received: " + Client.LastReceivedTimeMS + "\n";
                                }
                                break;
                            case FMNetworkType.DataStream:
                                _status += "Is Connected: " + DataStream.IsConnected + "\n"
                                    + "last received: " + DataStream.LastReceivedTimeMS + "\n";
                                break;
                        }
                        Status = _status;
                    }
                    catch(Exception e) { if (ShowLog) Debug.LogWarning(e); }
                }
                else
                {
                    switch (NetworkType)
                    {
                        case FMNetworkType.Server:
                            _status += "Connection Count: " + "0" + "\n";
                            _status += "UDP Listener Type: " + ServerSettings.UDPListenerType + "\n";
                            _status += "Use Main Thread Sender: " + ServerSettings.UseMainThreadSender + "\n";
                            break;
                        case FMNetworkType.Client:
                            _status += "Is Connected: " + false + "\n";
                            _status += "Use Main Thread Sender: " + ClientSettings.UseMainThreadSender + "\n";
                            break;
                        case FMNetworkType.DataStream:
                            _status += "Is Connected: " + false + "\n";
                            break;
                    }
                    Status = _status;
                }
                if (UIStatus != null) UIStatus.text = Status;
            }
            #endregion
            //====================Update Debug Text============================
        }

        #region SENDER MAPPING
        public void SendFunctionToAll(string inputFunctionName, object[] inputVariables = null)
        {
            SendFunction(FMSendType.All, inputFunctionName, inputVariables);
        }
        public void SendFunctionToServer(string inputFunctionName, object[] inputVariables = null)
        {
            SendFunction(FMSendType.Server, inputFunctionName, inputVariables);
        }
        public void SendFunctionToOthers(string inputFunctionName, object[] inputVariables = null)
        {
            SendFunction(FMSendType.Others, inputFunctionName, inputVariables);
        }
        public void SendFunctionToTargetIP(string inputTargetIP, string inputFunctionName, object[] inputVariables = null)
        {
            SendFunction(FMSendType.All, inputFunctionName, inputVariables);
        }
        public void SendFunctionToAllReliable(string inputFunctionName, object[] inputVariables = null)
        {
            SendFunctionReliable(FMSendType.All, inputFunctionName, inputVariables);
        }
        public void SendFunctionToServerReliable(string inputFunctionName, object[] inputVariables = null)
        {
            SendFunctionReliable(FMSendType.Server, inputFunctionName, inputVariables);
        }
        public void SendFunctionToOthersReliable(string inputFunctionName, object[] inputVariables = null)
        {
            SendFunctionReliable(FMSendType.Others, inputFunctionName, inputVariables);
        }
        public void SendFunctionToTargetIPReliable(string inputTargetIP, string inputFunctionName, object[] inputVariables = null)
        {
            SendFunctionReliable(FMSendType.All, inputFunctionName, inputVariables);
        }
        public void SendFunction(FMSendType inputSendType, string inputFunctionName, object[] inputVariables = null, string inputTargetIP = null)
        {
            FMNetworkFunction _fmfunction = new FMNetworkFunction(inputFunctionName, inputVariables);
            Send(_fmfunction, inputSendType, inputTargetIP, false);
        }
        public void SendFunctionReliable(FMSendType inputSendType, string inputFunctionName, object[] inputVariables = null, string inputTargetIP = null)
        {
            FMNetworkFunction _fmfunction = new FMNetworkFunction(inputFunctionName, inputVariables);
            Send(_fmfunction, inputSendType, inputTargetIP, true);
        }

        public void StreamData(byte[] _byteData)
        {
            if (!Initialised) return;
            if (NetworkType != FMNetworkType.DataStream) return;
            DataStream.Action_AddBytes(_byteData);
        }
        public void Send(byte[] _byteData, FMSendType _type, bool _reliable = false) { Send(_byteData, _type, null, _reliable); }
        public void Send(string _stringData, FMSendType _type, bool _reliable = false) { Send(_stringData, _type, null, _reliable); }

        public void SendToAll(byte[] _byteData) { Send(_byteData, FMSendType.All, null, false); }
        public void SendToServer(byte[] _byteData) { Send(_byteData, FMSendType.Server, null, false); }
        public void SendToOthers(byte[] _byteData) { Send(_byteData, FMSendType.Others, null, false); }

        public void SendToAll(string _stringData) { Send(_stringData, FMSendType.All, null, false); }
        public void SendToServer(string _stringData) { Send(_stringData, FMSendType.Server, null, false); }
        public void SendToOthers(string _stringData) { Send(_stringData, FMSendType.Others, null, false); }

        public void SendToAllReliable(byte[] _byteData) { Send(_byteData, FMSendType.All, null, true); }
        public void SendToServerReliable(byte[] _byteData) { Send(_byteData, FMSendType.Server, null, true); }
        public void SendToOthersReliable(byte[] _byteData) { Send(_byteData, FMSendType.Others, null, true); }

        public void SendToAllReliable(string _stringData) { Send(_stringData, FMSendType.All, null, true); }
        public void SendToServerReliable(string _stringData) { Send(_stringData, FMSendType.Server, null, true); }
        public void SendToOthersReliable(string _stringData) { Send(_stringData, FMSendType.Others, null, true); }

        public void SendToTargetReliable(byte[] _byteData, string _targetIP) { SendToTarget(_byteData, _targetIP, true); }
        public void SendToTargetReliable(string _stringData, string _targetIP) { SendToTarget(_stringData, _targetIP, true); }

        public void SendToTarget(byte[] _byteData, string _targetIP, bool _reliable = false)
        {
            if (NetworkType == FMNetworkType.Server)
            {
                if (Server.ConnectedIPs.Contains(_targetIP))
                {
                    Send(_byteData, FMSendType.TargetIP, _targetIP, _reliable);
                }
                else
                {
                    if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                    {
                        OnReceivedByteDataEvent.Invoke(_byteData);
                    }
                }
            }
            else
            {
                if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                {
                    OnReceivedByteDataEvent.Invoke(_byteData);
                }
                else
                {
                    Send(_byteData, FMSendType.TargetIP, _targetIP, _reliable);
                }
            }
        }
        public void SendToTarget(string _stringData, string _targetIP, bool _reliable = false)
        {
            if (NetworkType == FMNetworkType.Server)
            {
                if (Server.ConnectedIPs.Contains(_targetIP))
                {
                    Send(_stringData, FMSendType.TargetIP, _targetIP, _reliable);
                }
                else
                {
                    if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                    {
                        OnReceivedStringDataEvent.Invoke(_stringData);
                    }
                }
            }
            else
            {
                if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                {
                    OnReceivedStringDataEvent.Invoke(_stringData);
                }
                else
                {
                    Send(_stringData, FMSendType.TargetIP, _targetIP, _reliable);
                }
            }
        }
        public void SendToTarget(FMNetworkFunction _fmfunction, string _targetIP, bool _reliable = false)
        {
            if (NetworkType == FMNetworkType.Server)
            {
                if (Server.ConnectedIPs.Contains(_targetIP))
                {
                    Send(_fmfunction, FMSendType.TargetIP, _targetIP, _reliable);
                }
                else
                {
                    if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                    {
                        OnReceivedNetworkFunction(_fmfunction);
                    }
                }
            }
            else
            {
                if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                {
                    OnReceivedNetworkFunction(_fmfunction);
                }
                else
                {
                    Send(_fmfunction, FMSendType.TargetIP, _targetIP, _reliable);
                }
            }
        }

        private void Send(FMNetworkFunction _fmfunction, FMSendType _type, string _targetIP, bool _reliable = false)
        {
            if (!Initialised) return;
            if (NetworkType == FMNetworkType.Client && !Client.IsConnected) return;

            string _stringData = JsonUtility.ToJson(_fmfunction, false);
            if (NetworkType == FMNetworkType.Client)
            {
                if (Client.ForceBroadcast)
                {
                    if (_type == FMSendType.All) OnReceivedStringDataEvent.Invoke(_stringData);
                    //_type = FMSendType.Server; //when broadcast mode enabled, force the send type to server, then it won't send twice to others
                }
            }

            switch (_type)
            {
                case FMSendType.All:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_stringData, _type, FMPacketDataType.networkFunction, _reliable);
                        OnReceivedNetworkFunction(_fmfunction);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, FMPacketDataType.networkFunction, _reliable);
                    }
                    break;
                case FMSendType.Server:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        OnReceivedNetworkFunction(_fmfunction);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, FMPacketDataType.networkFunction, _reliable);
                    }
                    break;
                case FMSendType.Others:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_stringData, _type, FMPacketDataType.networkFunction, _reliable);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, FMPacketDataType.networkFunction, _reliable);
                    }
                    break;
                case FMSendType.TargetIP:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        if (_targetIP.Length > 6) Server.Action_AddPacket(_stringData, _targetIP, FMPacketDataType.networkFunction, _reliable);
                    }
                    else
                    {
                        if (_targetIP.Length > 6) Client.Action_AddPacket(_stringData, _targetIP, FMPacketDataType.networkFunction, _reliable);
                    }
                    break;
            }
        }

        public void SendNetworkTransformToOthers(byte[] _byteData) { SendNetworkTransform(_byteData, FMSendType.Others, null, false); }
        private void SendNetworkTransform(byte[] _byteData, FMSendType _type, string _targetIP, bool _reliable = false)
        {
            if (!Initialised) return;
            if (NetworkType == FMNetworkType.Client && !Client.IsConnected) return;

            if (NetworkType == FMNetworkType.Client)
            {
                if (Client.ForceBroadcast)
                {
                    if (_type == FMSendType.All) OnReceivedNetworkTransform(_byteData);
                    //_type = FMSendType.Server; //when broadcast mode enabled, force the send type to server, then it won't send twice to others
                }
            }

            switch (_type)
            {
                case FMSendType.All:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_byteData, _type, FMPacketDataType.networkTransform, _reliable);
                        OnReceivedNetworkTransform(_byteData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, FMPacketDataType.networkTransform, _reliable);
                    }
                    break;
                case FMSendType.Server:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        OnReceivedNetworkTransform(_byteData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, FMPacketDataType.networkTransform, _reliable);
                    }
                    break;
                case FMSendType.Others:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_byteData, _type, FMPacketDataType.networkTransform, _reliable);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, FMPacketDataType.networkTransform, _reliable);
                    }
                    break;
                case FMSendType.TargetIP:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        if (_targetIP.Length > 6) Server.Action_AddPacket(_byteData, _targetIP, FMPacketDataType.networkTransform, _reliable);
                    }
                    else
                    {
                        if (_targetIP.Length > 6) Client.Action_AddPacket(_byteData, _targetIP, FMPacketDataType.networkTransform, _reliable);
                    }
                    break;
            }
        }
        private void Send(byte[] _byteData, FMSendType _type, string _targetIP, bool _reliable = false)
        {
            if (!Initialised) return;
            if(NetworkType == FMNetworkType.DataStream)
            {
                Debug.LogError("Your FMNetwork Type is \"DataStream\", please use StreamData() method instead. This data are ignored and not sent due to mis-matching network type and send method.");
                return;
            }

            if (NetworkType == FMNetworkType.Client && !Client.IsConnected) return;

            if (NetworkType == FMNetworkType.Client)
            {
                if (Client.ForceBroadcast)
                {
                    if (_type == FMSendType.All) OnReceivedByteDataEvent.Invoke(_byteData);
                    //_type = FMSendType.Server; //when broadcast mode enabled, force the send type to server, then it won't send twice to others
                }
            }

            switch (_type)
            {
                case FMSendType.All:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_byteData, _type, FMPacketDataType.rawByte, _reliable);
                        OnReceivedByteDataEvent.Invoke(_byteData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, FMPacketDataType.rawByte, _reliable);
                    }
                    break;
                case FMSendType.Server:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        OnReceivedByteDataEvent.Invoke(_byteData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, FMPacketDataType.rawByte, _reliable);
                    }
                    break;
                case FMSendType.Others:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_byteData, _type, FMPacketDataType.rawByte, _reliable);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, FMPacketDataType.rawByte, _reliable);
                    }
                    break;
                case FMSendType.TargetIP:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        if (_targetIP.Length > 4) Server.Action_AddPacket(_byteData, _targetIP, FMPacketDataType.rawByte, _reliable);
                    }
                    else
                    {
                        if (_targetIP.Length > 4) Client.Action_AddPacket(_byteData, _targetIP, FMPacketDataType.rawByte, _reliable);
                    }
                    break;
            }
        }

        private void Send(string _stringData, FMSendType _type, string _targetIP, bool _reliable = false)
        {
            if (!Initialised) return;
            if (NetworkType == FMNetworkType.Client && !Client.IsConnected) return;

            if (NetworkType == FMNetworkType.Client)
            {
                if (Client.ForceBroadcast)
                {
                    if (_type == FMSendType.All) OnReceivedStringDataEvent.Invoke(_stringData);
                    //_type = FMSendType.Server; //when broadcast mode enabled, force the send type to server, then it won't send twice to others
                }
            }

            switch (_type)
            {
                case FMSendType.All:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_stringData, _type, FMPacketDataType.stringData, _reliable);
                        OnReceivedStringDataEvent.Invoke(_stringData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, FMPacketDataType.stringData, _reliable);
                    }
                    break;
                case FMSendType.Server:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        OnReceivedStringDataEvent.Invoke(_stringData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, FMPacketDataType.stringData, _reliable);
                    }
                    break;
                case FMSendType.Others:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_stringData, _type, FMPacketDataType.stringData, _reliable);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, FMPacketDataType.stringData, _reliable);
                    }
                    break;
                case FMSendType.TargetIP:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        if (_targetIP.Length > 6) Server.Action_AddPacket(_stringData, _targetIP, FMPacketDataType.stringData, _reliable);
                    }
                    else
                    {
                        if (_targetIP.Length > 6) Client.Action_AddPacket(_stringData, _targetIP, FMPacketDataType.stringData, _reliable);
                    }
                    break;
            }
        }

        #endregion

        public void Action_ReloadScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

    }

}