using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;

namespace FMSolution.FMWebSocket
{
    public class FMWebSocketCore : MonoBehaviour
    {
        private void OnEnable()
        {
            //don't auto start if there is no connection before...
            if (Manager == null) return;
            if (Manager.Initialised) StartAll();
        }
        private void Start() { StartAll(); }

        private void OnApplicationQuit() { StopAll(); }
        private void OnDisable() { StopAll(); }
        private void OnDestroy() { StopAll(); }

        internal int EnvironmentTickCountDelta(int currentMS, int lastMS)
        {
            if (currentMS < 0 && lastMS > 0) return Mathf.Abs(currentMS - int.MinValue) + (int.MaxValue - lastMS);
            return currentMS - lastMS;
        }

        public FMWebSocketNetworkType NetworkType = FMWebSocketNetworkType.Room;

        [HideInInspector] public FMWebSocketManager Manager;
        public string IP = "127.0.0.1";
        public int port = 3000;
        public bool sslEnabled = false;
        public FMSslProtocols sslProtocols = FMSslProtocols.Default;
        public bool portRequired = true;
        public string url = "ws://127.0.0.1:3000";

        public bool autoReconnect = true;
        public bool UseMainThreadSender = true;

        public string wsid = "";
        public string serverWSID = "";
        public List<ConnectedFMWebSocketClient> ConnectedClients = new List<ConnectedFMWebSocketClient>();

        public bool ShowLog = true;
        internal void DebugLog(string _value) { if (ShowLog) Debug.Log("FMLog: " + _value); }

        internal FMWebSocketConnectionStatus _connectionStatus = FMWebSocketConnectionStatus.Disconnected;
        internal FMWebSocketConnectionStatus connectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                _connectionStatus = value;
                Manager.Settings.ConnectionStatus = _connectionStatus;
            }
        }

        internal class FMWebSocketData
        {
            public bool isBinary = true;
            public byte[] ByteData = new byte[0];
            public string StringData = null;
            public FMWebSocketData(byte[] inputBytes)
            {
                isBinary = true;
                ByteData = inputBytes;
            }
            public FMWebSocketData(string inputString)
            {
                isBinary = false;
                StringData = inputString;
            }
        }
        internal ConcurrentQueue<FMWebSocketData> _appendQueueSendFMWebSocketData = new ConcurrentQueue<FMWebSocketData>();
        internal ConcurrentQueue<byte[]> _appendQueueSendWebSocketByteData = new ConcurrentQueue<byte[]>();
        internal ConcurrentQueue<string> _appendQueueSendWebSocketStringData = new ConcurrentQueue<string>();
        internal ConcurrentQueue<byte[]> _appendQueueReceivedData = new ConcurrentQueue<byte[]>();
        internal ConcurrentQueue<string> _appendQueueReceivedStringData = new ConcurrentQueue<string>();
        internal void ResetConcurrentQueues()
        {
            wsSendAsyncCompleted = true;

            _appendQueueSendFMWebSocketData = new ConcurrentQueue<FMWebSocketData>();
            _appendQueueSendWebSocketByteData = new ConcurrentQueue<byte[]>();
            _appendQueueSendWebSocketStringData = new ConcurrentQueue<string>();

            _appendQueueReceivedData = new ConcurrentQueue<byte[]>();
            _appendQueueReceivedStringData = new ConcurrentQueue<string>();
        }

        internal int pingThreshold = 2000;
        internal bool waitingPong = false;
        internal bool wsConnected = false;
        internal bool wsJoinedRoom { get { return Manager.Settings.wsJoinedRoom; } set { Manager.Settings.wsJoinedRoom = value; } }
        internal bool wsRoomMaster { get { return Manager.Settings.wsRoomMaster; } set { Manager.Settings.wsRoomMaster = value; } }

        internal virtual bool IsWebSocketConnected() { return false; }
        public void Connect() { if (connectionStatus == FMWebSocketConnectionStatus.Disconnected) StartAll(); }
        public void Close() { StopAll(); }

        internal bool initialised = false;
        internal virtual void StopAll() { }
        internal virtual void StartAll() { }

        internal void FMWebSocketEvent(string inputType, string inputVariable, bool sendAsync = false)
        {
            WebSocket_Send(FMWebSocketEncode(inputType, inputVariable), sendAsync);
        }
        internal string FMWebSocketEncode(string inputType, string inputVariable)
        {
            return "fmevent" + ":" + inputType + ":" + inputVariable;
        }
        internal string[] FMWebSocketEventDecode(string inputString)
        {
            return inputString.Split(':');
        }

        internal bool IsClientExisted(string inputWSID)
        {
            bool _existed = false;
            for (int i = 0; i < ConnectedClients.Count; i++)
            {
                if (inputWSID == ConnectedClients[i].wsid) _existed = true;
            }
            return _existed;
        }

        internal void RegisterNewClient(string inputWSID)
        {
            //register new client
            ConnectedFMWebSocketClient NewClient = new ConnectedFMWebSocketClient();
            NewClient.wsid = inputWSID;
            ConnectedClients.Add(NewClient);
            Manager.OnClientConnected(inputWSID);
            Manager.OnConnectionCountChanged(ConnectedClients.Count);
        }

        internal void RegisterRoom()
        {
            FMWebSocketEvent("roomName", Manager.RoomName);
        }
        internal void RegisterNetworkType()
        {
            FMWebSocketEvent("networkType", NetworkType.ToString());
            GetWSConnectionStatus();

            //ping pong checking...
            waitingPong = false;
            waitingLatency = false;
            FMPing();
        }

        internal virtual void WebSocketStartLoop() { }
        internal virtual void WebSocketSenderLoop() { }

        internal bool GetWSConnectionStatus()
        {
            if (NetworkType != FMWebSocketNetworkType.Room)
            {
                //return wsConnected && fmconnected; //require checking heartbeat seen time for Room system
                wsConnected = IsWebSocketConnected();
                connectionStatus = wsConnected ? FMWebSocketConnectionStatus.WebSocketReady : FMWebSocketConnectionStatus.Disconnected;
                return wsConnected;
            }
            else
            {
                wsConnected = IsWebSocketConnected();
                CurrentSeenTimeMS = Environment.TickCount;
                bool _pong = LastPongTimeMS == 0 || EnvironmentTickCountDelta(CurrentSeenTimeMS, LastPongTimeMS) < pingThreshold;

                if (wsConnected)
                {
                    connectionStatus = _pong ? FMWebSocketConnectionStatus.FMWebSocketConnected : FMWebSocketConnectionStatus.WebSocketReady;
                }
                else
                {
                    connectionStatus = FMWebSocketConnectionStatus.Disconnected;
                }
                return wsConnected && _pong;
            }
        }

        public int CurrentSeenTimeMS = 0;
        public int PingMS = 0;
        public int GetPingMS()
        {
            int _pingMS = 0;
            int _ping = LastPingTimeMS;
            int _pong = LastPongTimeMS;
            if (waitingPong)
            {
                if (_ping == 0) return 0;
                _pingMS = EnvironmentTickCountDelta(Environment.TickCount, _ping);
            }
            else
            {
                _pingMS = EnvironmentTickCountDelta(_pong, _ping);
            }
            Manager.Settings.pingMS = _pingMS;
            return _pingMS;
        }

        internal long _lastPingTimeMS;
        internal virtual int LastPingTimeMS
        {
            get { return (int)_lastPingTimeMS; }
            set { _lastPingTimeMS = (long)value; }
        }
        internal long _lastPongTimeMS;
        internal virtual int LastPongTimeMS
        {
            get { return (int)_lastPongTimeMS; }
            set { _lastPongTimeMS = (long)value; }
        }

        internal void FMPing(bool sendAsync = false)
        {
            //ping pong checking...
            if (!waitingPong)
            {
                int _currentTickCount = Environment.TickCount;
                if (LastPingTimeMS == 0 || EnvironmentTickCountDelta(_currentTickCount, LastPingTimeMS) > 1000)
                {
                    waitingPong = true;
                    LastPingTimeMS = _currentTickCount;
                    //FMWebSocketEvent("ping", LastPingTimeMS.ToString());
                    FMWebSocketEvent("ping", "");
                }
            }
            else
            {
                GetWSConnectionStatus();
            }
        }

        internal bool waitingLatency = false;
        internal long _lastLatencyPingTimeMS;
        internal virtual int LastLatencyPingTimeMS
        {
            get { return (int)_lastLatencyPingTimeMS; }
            set { _lastLatencyPingTimeMS = (long)value; }
        }
        internal void FMLatencyPing()
        {
            if (!wsJoinedRoom) return;
            if (!waitingLatency)
            {
                int _currentTickCount = Environment.TickCount;
                if (LastLatencyPingTimeMS == 0 || EnvironmentTickCountDelta(_currentTickCount, LastLatencyPingTimeMS) > 1000)
                {
                    waitingLatency = true;
                    LastLatencyPingTimeMS = _currentTickCount;
                    _appendQueueSendFMWebSocketData.Enqueue(new FMWebSocketData(FMWebSocketEncode("lping", LastLatencyPingTimeMS.ToString()))); ;
                }
            }
        }

        internal void OnMessageCheck(string _msg)
        {
            //DebugLog("msg: " + _msg);
            if (_msg.Contains("fmevent:"))
            {
                string[] decodedResult = FMWebSocketEventDecode(_msg);
                if (decodedResult.Length == 3)
                {
                    string decodedType = decodedResult[1];
                    string decodedValue = decodedResult[2];

                    if (decodedType != "pong" && decodedType != "lpong") DebugLog("msg: " + _msg);
                    switch (decodedType)
                    {
                        case "lpong":
                            waitingLatency = false;
                            if (int.TryParse(decodedValue, out int _lping)) Manager.Settings.latencyMS = EnvironmentTickCountDelta(Environment.TickCount, _lping);
                            break;
                        case "pong":
                            waitingPong = false;
                            LastPongTimeMS = Environment.TickCount;
                            GetWSConnectionStatus();//check FMWebSocket status...

                            string[] values = decodedValue.Split(',');
                            bool _wsRoomMaster = false;
                            if (values.Length >= 2)
                            {
                                if (values[1].Contains("roomMaster")) _wsRoomMaster = true;
                            }
                            Manager.Settings.wsRoomMaster = _wsRoomMaster;

                            PingMS = GetPingMS();
                            break;
                        case "OnReceivedWSIDEvent":
                            wsid = decodedValue;
                            Manager.Settings.wsid = wsid;
                            break;

                        case "OnJoinedLobbyEvent":
                            wsid = decodedValue;
                            Manager.OnJoinedLobby(wsid);

                            RegisterRoom();
                            break;
                        case "OnJoinedRoom":
                            Manager.OnJoinedRoom(decodedValue);
                            wsJoinedRoom = true;
                            break;

                        case "OnRoomClientsUpdated":
                            ConnectedClients = new List<ConnectedFMWebSocketClient>();
                            string[] results = decodedValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string result in results)
                            {
                                if (!IsClientExisted(result)) RegisterNewClient(result);
                            }
                            break;
                        case "OnClientConnectedEvent":
                            if (!IsClientExisted(decodedValue)) RegisterNewClient(decodedValue);
                            break;
                        case "OnClientDisconnectedEvent":
                            for (int i = 0; i < ConnectedClients.Count; i++)
                            {
                                if (decodedValue == ConnectedClients[i].wsid)
                                {
                                    //remove disconnected client
                                    ConnectedClients.Remove(ConnectedClients[i]);
                                    Manager.OnClientDisconnected(decodedValue);
                                    Manager.OnConnectionCountChanged(ConnectedClients.Count);
                                }
                            }
                            break;
                    }
                }
            }
        }

        internal virtual void FMWebSocket_Close() { }
        internal virtual void FMWebSocket_Send(byte[] _byteData, bool sendAsync = false) { }
        internal virtual void FMWebSocket_Send(string _stringData, bool sendAsync = false) { }
        internal virtual void WebSocket_Send(byte[] _byteData, bool sendAsync = false) { }
        internal virtual void WebSocket_Send(string _stringData, bool sendAsync = false) { }

        internal bool wsSendAsyncCompleted = true;
        internal void wsSendAsyncAction(bool _inputCompleted) { wsSendAsyncCompleted = true; }
        internal async void Sender(bool sendAsync = false)
        {
            if (NetworkType != FMWebSocketNetworkType.WebSocket)
            {
                FMPing(sendAsync);
                FMLatencyPing();

                //FM WebSocket Mode
                if (connectionStatus != FMWebSocketConnectionStatus.FMWebSocketConnected) return;
                if (_appendQueueSendFMWebSocketData.Count > 0)
                {
                    //limit 100 packet sent in each frame, solved overhead issue on receiver
                    int k = 0;
                    while (_appendQueueSendFMWebSocketData.Count > 0 && k < 100)
                    {
                        k++;
                        try
                        {
                            if (_appendQueueSendFMWebSocketData.TryDequeue(out FMWebSocketData _fmwebsocketData))
                            {
                                if (_fmwebsocketData.isBinary)
                                {
                                    FMWebSocket_Send(_fmwebsocketData.ByteData, sendAsync);
                                }
                                else
                                {
                                    FMWebSocket_Send(_fmwebsocketData.StringData, sendAsync);
                                }
                            }
                            if (sendAsync)
                            {
                                while (!wsSendAsyncCompleted)
                                {
                                    await FMCoreTools.AsyncTask.Yield();
                                    if (!wsSendAsyncCompleted) await FMCoreTools.AsyncTask.Delay(1);
                                }
                            }
                        }
                        catch (Exception e) { DebugLog(e.ToString()); }
                    }
                }
            }
            else
            {
                //Raw WebSocket
                if (connectionStatus == FMWebSocketConnectionStatus.Disconnected) return;
                if (_appendQueueSendWebSocketByteData.Count > 0)
                {
                    //limit 100 packet sent in each frame, solved overhead issue on receiver
                    int k = 0;
                    while (_appendQueueSendWebSocketByteData.Count > 0 && k < 100)
                    {
                        k++;
                        try
                        {
                            if (_appendQueueSendWebSocketByteData.TryDequeue(out byte[] _bytes)) WebSocket_Send(_bytes, sendAsync);
                            if (sendAsync)
                            {
                                while (!wsSendAsyncCompleted)
                                {
                                    await FMCoreTools.AsyncTask.Yield();
                                    if (!wsSendAsyncCompleted) await FMCoreTools.AsyncTask.Delay(1);
                                }
                            }
                        }
                        catch (Exception e) { DebugLog(e.ToString()); }
                    }
                }
                if (_appendQueueSendWebSocketStringData.Count > 0)
                {
                    //limit 100 packet sent in each frame, solved overhead issue on receiver
                    int k = 0;
                    while (_appendQueueSendWebSocketStringData.Count > 0 && k < 100)
                    {
                        k++;
                        try
                        {
                            if (_appendQueueSendWebSocketStringData.TryDequeue(out string _string)) WebSocket_Send(_string, sendAsync);
                            if (sendAsync)
                            {
                                while (!wsSendAsyncCompleted)
                                {
                                    await FMCoreTools.AsyncTask.Yield();
                                    if (!wsSendAsyncCompleted) await FMCoreTools.AsyncTask.Delay(1);
                                }
                            }
                        }
                        catch (Exception e) { DebugLog(e.ToString()); }
                    }
                }
            }
        }

        public void Send(byte[] _byteData, FMWebSocketSendType _type, string _targetID, bool _isEncodedString = false)
        {
            if (NetworkType == FMWebSocketNetworkType.WebSocket || connectionStatus != FMWebSocketConnectionStatus.FMWebSocketConnected) return;
            if (!wsJoinedRoom)
            {
                Debug.LogError("skip, not joined room yet");
                return;
            }
            //if (PingMS > 200) return;//skip it if ping too high..

            byte[] _meta = new byte[4]; _meta[0] = (byte)(_isEncodedString ? 1 : 0);//raw byte(0), encoded string(1)
            switch (_type)
            {
                case FMWebSocketSendType.All: _meta[1] = 0; break;
                case FMWebSocketSendType.Server: _meta[1] = 1; break;
                case FMWebSocketSendType.Others: _meta[1] = 2; break;
                case FMWebSocketSendType.Target: _meta[1] = 3; break;
            }

            if (_type != FMWebSocketSendType.Target)
            {
                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);
                _appendQueueSendFMWebSocketData.Enqueue(new FMWebSocketData(_sendByte));
            }
            else
            {
                byte[] _wsid = Encoding.ASCII.GetBytes(_targetID);
                byte[] _wsidByteLength = BitConverter.GetBytes((UInt16)_wsid.Length);
                byte[] _meta_wsid = new byte[_wsid.Length + _wsidByteLength.Length];
                Buffer.BlockCopy(_wsidByteLength, 0, _meta_wsid, 0, _wsidByteLength.Length);
                Buffer.BlockCopy(_wsid, 0, _meta_wsid, 2, _wsid.Length);

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length + _meta_wsid.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_meta_wsid, 0, _sendByte, 4, _meta_wsid.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, _meta.Length + _meta_wsid.Length, _byteData.Length);
                _appendQueueSendFMWebSocketData.Enqueue(new FMWebSocketData(_sendByte));
            }
        }

        public void Send(string _stringData, FMWebSocketSendType _type, string _targetID) { Send(Encoding.ASCII.GetBytes(_stringData), _type, _targetID, true); }
        public void WebSocketSend(byte[] _byteData)
        {
            if (NetworkType != FMWebSocketNetworkType.WebSocket || connectionStatus == FMWebSocketConnectionStatus.Disconnected) return;
            _appendQueueSendWebSocketByteData.Enqueue(_byteData);
        }
        public void WebSocketSend(string _stringData)
        {
            if (NetworkType != FMWebSocketNetworkType.WebSocket || connectionStatus == FMWebSocketConnectionStatus.Disconnected) return;
            _appendQueueSendWebSocketStringData.Enqueue(_stringData);
        }
    }
}