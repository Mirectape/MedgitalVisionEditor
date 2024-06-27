using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FMSolution.FMWebSocket
{
    public enum FMWebSocketNetworkType { Room, WebSocket }
    public enum FMWebSocketSendType { All, Server, Others, Target, RoomMaster }
    public enum FMWebSocketConnectionStatus { Disconnected, WebSocketReady, FMWebSocketConnected }
    public enum FMSslProtocols
    {
        Default = 0xF0,
        None = 0x0,
        Ssl2 = 0xC,
        Ssl3 = 0x30,
        Tls = 0xC0,
#if UNITY_2019_1_OR_NEWER
        Tls11 = 0x300,
        Tls12 = 0xC00
#endif
    }

    [System.Serializable]
    public class ConnectedFMWebSocketClient
    {
        public string wsid = "";
    }

    [AddComponentMenu("FMETP/Network/FMWebSocketManager")]
    public class FMWebSocketManager : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowNetworking = true;
        public bool EditorShowSyncTransformation = true;
        public bool EditorShowEvents = true;
        public bool EditorShowDebug = true;

        public bool EditorShowWebSocketSettings = false;

        public bool EditorShowNetworkObjects = false;

        public bool EditorShowReceiverEvents = false;
        public bool EditorShowConnectionEvents = false;
        #endregion

        private string[] urlStartStrings = new string[] { "http://", "https://", "ws://", "wss://" };
        private void CheckSettingsIP()
        {
            if (Settings.IP == null) return;
            Settings.IP = Settings.IP.TrimEnd(' ');
            Settings.IP = Settings.IP.TrimEnd('/');
            for (int i = 0; i < urlStartStrings.Length; i++)
            {
                if (Settings.IP.StartsWith(urlStartStrings[i])) Settings.IP = Settings.IP.Substring(urlStartStrings[i].Length);
            }
        }
        /// <summary name="Action_SetIP()">
        /// Assign new IP address for connection
        /// </summary>
        /// <param name="_ip">ip address of server, "127.0.0.1" by default</param>
        public void Action_SetIP(string _ip) { Settings.IP = _ip; CheckSettingsIP(); }
        /// <summary>
        /// Assign new port number for connection
        /// </summary>
        /// <param name="_port">port of server, (string)3000 -> (int)3000 by default</param>
        public void Action_SetPort(string _port) { Settings.port = int.Parse(_port); }
        /// <summary>
        /// Turn on/off Ssl support
        /// </summary>
        /// <param name="_value">true: enable Ssl; false: disable Ssl</param>
        public void Action_SetSslEnabled(bool _value) { Settings.sslEnabled = _value; }
        /// <summary>
        /// Turn on/off "portRequired"
        /// </summary>
        /// <param name="_value">true: require port; false: not require port</param>
        public void Action_SetPortRequired(bool _value) { Settings.portRequired = _value; }
        /// <summary name="Action_SetIP()">
        /// Assign new IP address for connection
        /// </summary>
        /// <param name="_ip">ip address of server, "127.0.0.1" by default</param>
        public void Action_SetRoomName(string _roomName) { RoomName = _roomName; }

        [System.Serializable]
        public class FMWebSocketSettings
        {
            public string IP = "127.0.0.1";
            public int port = 3000;
            public bool sslEnabled = false;
            public FMSslProtocols sslProtocols = FMSslProtocols.Default;

            public bool portRequired = true;

            [Tooltip("(( suggested for low-end mobile, but not recommend for streaming large data ))")]
            public bool UseMainThreadSender = true;

            public FMWebSocketConnectionStatus ConnectionStatus = FMWebSocketConnectionStatus.Disconnected;
            public string wsid = "";
            public bool autoReconnect = true;

            public int pingMS = 0;
            public int latencyMS = 0;
            public bool wsJoinedRoom = false;
            public bool wsRoomMaster = false;
        }

        public List<ConnectedFMWebSocketClient> ConnectedClients
        {
            get
            {
                if (!Initialised) return null;
                //if (NetworkType != FMWebSocketNetworkType.Server) return null;
                return fmwebsocket.ConnectedClients;
            }
        }

        public static FMWebSocketManager instance;
        private void Awake()
        {
            Application.runInBackground = true;
            if (instance == null) instance = this;

            Initialised = false;
            Settings.ConnectionStatus = FMWebSocketConnectionStatus.Disconnected;
        }

        private void Start() { if (AutoInit) Init(); }
        public bool AutoInit = true;
        public bool Initialised = false;
        public FMWebSocketNetworkType NetworkType = FMWebSocketNetworkType.Room;
        public string RoomName = "MyRoomTest";
        public FMWebSocketSettings Settings;

        [HideInInspector] public FMWebSocketCore fmwebsocket;

        public UnityEventByteArray OnReceivedByteDataEvent = new UnityEventByteArray();
        public UnityEventString OnReceivedStringDataEvent = new UnityEventString();
        public UnityEventByteArray GetRawReceivedByteDataEvent = new UnityEventByteArray();
        public UnityEventString GetRawReceivedStringDataEvent = new UnityEventString();

        public UnityEventString OnClientConnectedEvent = new UnityEventString();
        public UnityEventString OnClientDisconnectedEvent = new UnityEventString();
        public UnityEventInt OnConnectionCountChangedEvent = new UnityEventInt();

        //room
        public UnityEventString OnJoinedLobbyEvent = new UnityEventString();
        public UnityEventString OnJoinedRoomEvent = new UnityEventString();
        public void OnJoinedLobby(string inputWSID)
        {
            OnJoinedLobbyEvent.Invoke(inputWSID);
            if (ShowLog) Debug.Log("OnJoinedLobbyEvent");
        }
        public void OnJoinedRoom(string inputRoomName)
        {
            OnJoinedRoomEvent.Invoke(inputRoomName);
            if (ShowLog) Debug.Log("OnJoinedRoom: " + inputRoomName);
        }
        //room

        public void OnClientConnected(string inputClientWSID)
        {
            OnClientConnectedEvent.Invoke(inputClientWSID);
            if (ShowLog) Debug.Log("OnClientConnected: " + inputClientWSID);
        }
        public void OnClientDisconnected(string inputClientWSID)
        {
            OnClientDisconnectedEvent.Invoke(inputClientWSID);
            if (ShowLog) Debug.Log("OnClientDisonnected: " + inputClientWSID);
        }
        public void OnConnectionCountChanged(int inputConnectionCount)
        {
            OnConnectionCountChangedEvent.Invoke(inputConnectionCount);
            if (ShowLog) Debug.Log("OnConnectionCountChanged: " + inputConnectionCount);
        }

        public bool ShowLog = true;

        /// <summary>
        /// Initialise FMWebSocket server
        /// </summary>
        public void Init()
        {
            CheckSettingsIP();
            if (fmwebsocket == null)
            {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
                fmwebsocket = this.gameObject.AddComponent<FMWebSocketPlatformStandalone.Component>();
#else
                fmwebsocket = this.gameObject.AddComponent<FMWebSocketPlatformWebGL.Component>();
#endif
            }

            fmwebsocket.hideFlags = HideFlags.HideInInspector;

            fmwebsocket.Manager = this;
            fmwebsocket.NetworkType = NetworkType;

            fmwebsocket.IP = Settings.IP;
            fmwebsocket.port = Settings.port;

            fmwebsocket.sslEnabled = Settings.sslEnabled;
            fmwebsocket.sslProtocols = Settings.sslProtocols;
            fmwebsocket.portRequired = Settings.portRequired;

            fmwebsocket.autoReconnect = Settings.autoReconnect;

            fmwebsocket.UseMainThreadSender = Application.platform == RuntimePlatform.WebGLPlayer ? true : Settings.UseMainThreadSender;
            fmwebsocket.ShowLog = ShowLog;

            fmwebsocket.Connect();

            Initialised = true;
        }

        public void Action_Close()
        {
            fmwebsocket.Close();
            Initialised = false;
            Settings.ConnectionStatus = FMWebSocketConnectionStatus.Disconnected;
        }
        public void Action_JoinOrCreateRoom()
        {
            NetworkType = FMWebSocketNetworkType.Room;
            Init();
        }
        public void Action_JoinOrCreateRoom(string inputRoomName)
        {
            NetworkType = FMWebSocketNetworkType.Room;
            RoomName = inputRoomName;
            Init();
        }
        public void Action_InitAsWebSocket()
        {
            NetworkType = FMWebSocketNetworkType.WebSocket;
            Init();
        }

        public void Action_RequestRoomMaster()
        {
            if (NetworkType == FMWebSocketNetworkType.Room)
            {
                if (!Initialised) return;
                if (!fmwebsocket.IsWebSocketConnected()) return;
                fmwebsocket.FMWebSocketEvent("requestRoomMaster", Settings.wsid);
            }
        }

        public void Send(byte[] _byteData, FMWebSocketSendType _type) { Send(_byteData, _type, null); }
        public void Send(string _stringData, FMWebSocketSendType _type) { Send(_stringData, _type, null); }

        public void SendToAll(byte[] _byteData) { Send(_byteData, FMWebSocketSendType.All, null); }
        public void SendToServer(byte[] _byteData) { Send(_byteData, FMWebSocketSendType.Server, null); }
        public void SendToOthers(byte[] _byteData) { Send(_byteData, FMWebSocketSendType.Others, null); }
        public void SendToTarget(byte[] _byteData, string _wsid) { Send(_byteData, FMWebSocketSendType.Target, _wsid); }

        public void SendToAll(string _stringData) { Send(_stringData, FMWebSocketSendType.All, null); }
        public void SendToServer(string _stringData) { Send(_stringData, FMWebSocketSendType.Server, null); }
        public void SendToOthers(string _stringData) { Send(_stringData, FMWebSocketSendType.Others, null); }
        public void SendToTarget(string _stringData, string _wsid) { Send(_stringData, FMWebSocketSendType.Target, _wsid); }

        /// <summary name="Send()">
        /// Send FMWebSocket data as byte[]
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: Server or Client</param>
        public void Send(byte[] _byteData, FMWebSocketSendType _type, string _targetID)
        {
            if (!Initialised) return;
            if (NetworkType == FMWebSocketNetworkType.Room && !Settings.wsJoinedRoom) return;
            fmwebsocket.Send(_byteData, _type, _targetID);
        }

        /// <summary name="Send()">
        /// Send FMWebSocket message as string
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: Server or Client</param>
        public void Send(string _stringData, FMWebSocketSendType _type, string _targetID)
        {
            if (!Initialised) return;
            if (NetworkType == FMWebSocketNetworkType.Room && !Settings.wsJoinedRoom) return;
            fmwebsocket.Send(_stringData, _type, _targetID);
        }

        /// <summary name="Send()">
        /// Send WebSocket message as string
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: WebSocket</param>
        public void WebSocketSend(string _stringData)
        {
            if (!Initialised) return;
            fmwebsocket.WebSocketSend(_stringData);
        }

        /// <summary name="Send()">
        /// Send WebSocket data as byte[]
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: WebSocket</param>
        public void WebSocketSend(byte[] _byteData)
        {
            if (!Initialised) return;
            fmwebsocket.WebSocketSend(_byteData);
        }
    }
}