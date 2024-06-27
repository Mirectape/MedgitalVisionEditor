using System;
using UnityEngine;
using UnityEditor;
using FMSolution.Editor;

namespace FMSolution.FMWebSocket
{
    [CustomEditor(typeof(FMWebSocketManager))]
    [CanEditMultipleObjects]
    public class FMWebSocketManager_Editor : UnityEditor.Editor
    {
        private FMWebSocketManager FMWebSocket;
        SerializedProperty AutoInitProp;
        SerializedProperty NetworkTypeProp;
        SerializedProperty RoomNameProp;

        SerializedProperty Settings_IPProp;
        SerializedProperty Settings_portProp;
        SerializedProperty Settings_sslEnabledProp;
        SerializedProperty Settings_sslProtocolsProp;
        SerializedProperty Settings_portRequiredProp;
        SerializedProperty Settings_UseMainThreadSenderProp;
        SerializedProperty Settings_ConnectionStatusProp;
        SerializedProperty Settings_wsidProp;
        SerializedProperty Settings_autoReconnectProp;

        SerializedProperty OnReceivedByteDataEventProp;
        SerializedProperty OnReceivedStringDataEventProp;
        SerializedProperty GetRawReceivedByteDataEventProp;
        SerializedProperty GetRawReceivedStringDataEventProp;

        SerializedProperty OnClientConnectedEventProp;
        SerializedProperty OnClientDisconnectedEventProp;
        SerializedProperty OnConnectionCountChangedEventProp;
        SerializedProperty OnJoinedLobbyEventProp;
        SerializedProperty OnJoinedRoomEventProp;

        SerializedProperty ShowLogProp;

        void OnEnable()
        {
            AutoInitProp = serializedObject.FindProperty("AutoInit");
            NetworkTypeProp = serializedObject.FindProperty("NetworkType");
            RoomNameProp = serializedObject.FindProperty("RoomName");

            Settings_IPProp = serializedObject.FindProperty("Settings.IP");
            Settings_portProp = serializedObject.FindProperty("Settings.port");
            Settings_sslEnabledProp = serializedObject.FindProperty("Settings.sslEnabled");
            Settings_sslProtocolsProp = serializedObject.FindProperty("Settings.sslProtocols");
            Settings_portRequiredProp = serializedObject.FindProperty("Settings.portRequired");
            Settings_UseMainThreadSenderProp = serializedObject.FindProperty("Settings.UseMainThreadSender");
            Settings_ConnectionStatusProp = serializedObject.FindProperty("Settings.ConnectionStatus");
            Settings_wsidProp = serializedObject.FindProperty("Settings.wsid");
            Settings_autoReconnectProp = serializedObject.FindProperty("Settings.autoReconnect");

            OnReceivedByteDataEventProp = serializedObject.FindProperty("OnReceivedByteDataEvent");
            OnReceivedStringDataEventProp = serializedObject.FindProperty("OnReceivedStringDataEvent");
            GetRawReceivedByteDataEventProp = serializedObject.FindProperty("GetRawReceivedByteDataEvent");
            GetRawReceivedStringDataEventProp = serializedObject.FindProperty("GetRawReceivedStringDataEvent");

            OnJoinedLobbyEventProp = serializedObject.FindProperty("OnJoinedLobbyEvent");
            OnJoinedRoomEventProp = serializedObject.FindProperty("OnJoinedRoomEvent");

            OnClientConnectedEventProp = serializedObject.FindProperty("OnClientConnectedEvent");
            OnClientDisconnectedEventProp = serializedObject.FindProperty("OnClientDisconnectedEvent");
            OnConnectionCountChangedEventProp = serializedObject.FindProperty("OnConnectionCountChangedEvent");

            ShowLogProp = serializedObject.FindProperty("ShowLog");
        }

        private bool drawSuccess = false;
        public override void OnInspectorGUI()
        {
            if (FMWebSocket == null) FMWebSocket = (FMWebSocketManager)target;

            serializedObject.Update();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                drawSuccess = FMCoreEditor.DrawHeader("\nFM WebSocket 4.0\n");

                if (!FMWebSocket.EditorShowNetworking || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) FMWebSocket.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) FMWebSocket.EditorShowNetworking = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(AutoInitProp, new GUIContent("Auto Init"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(NetworkTypeProp, new GUIContent("NetworkType"));
                        GUILayout.EndHorizontal();

                        if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Room)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(RoomNameProp, new GUIContent("RoomName"));
                            GUILayout.EndHorizontal();
                        }

                        if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Room
                            || FMWebSocket.NetworkType == FMWebSocketNetworkType.WebSocket)
                        {
                            GUILayout.BeginVertical();
                            {
                                if (!FMWebSocket.EditorShowWebSocketSettings)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("= WebSocket Settings")) FMWebSocket.EditorShowWebSocketSettings = true;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("- WebSocket Settings")) FMWebSocket.EditorShowWebSocketSettings = false;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        {
                                            string _url = "ws" + (FMWebSocket.Settings.sslEnabled ? "s" : "") + "://" + FMWebSocket.Settings.IP;
                                            if (FMWebSocket.Settings.portRequired) _url += ":" + FMWebSocket.Settings.port;
                                            GUILayout.Label("URL: " + _url);
                                        }
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        {
                                            string _url = "ws" + (FMWebSocket.Settings.sslEnabled ? "s" : "") + "://" + FMWebSocket.Settings.IP;
                                            if (FMWebSocket.Settings.portRequired) _url += ":" + FMWebSocket.Settings.port;
                                            GUILayout.Label("URL(WebGL): " + _url);
                                        }
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        {
                                            string _wsid = FMWebSocket.Settings.wsid.Length == 0 ? "null" : FMWebSocket.Settings.wsid;
                                            GUILayout.Label("WebSocket ID(wsid): " + _wsid);
                                        }
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        {
                                            GUILayout.Label("wsJoinedRoom: " + FMWebSocket.Settings.wsJoinedRoom);
                                        }
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        {
                                            GUILayout.Label("isRoomMaster: " + FMWebSocket.Settings.wsRoomMaster);
                                        }
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        {
                                            GUILayout.Label("pingMS: " + FMWebSocket.Settings.pingMS);
                                        }
                                        GUILayout.EndHorizontal();
                                        GUILayout.BeginHorizontal();
                                        {
                                            GUILayout.Label("latencyMS: " + FMWebSocket.Settings.latencyMS);
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_IPProp, new GUIContent("IP"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_portProp, new GUIContent("Port"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_sslEnabledProp, new GUIContent("ssl Enabled"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_sslProtocolsProp, new GUIContent("ssl Protocols"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_portRequiredProp, new GUIContent("port Required"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_ConnectionStatusProp, new GUIContent("Connection Status"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_autoReconnectProp, new GUIContent("auto Reconnect"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_UseMainThreadSenderProp, new GUIContent("UseMainThreadSender"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!FMWebSocket.EditorShowEvents || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Events")) FMWebSocket.EditorShowEvents = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Events")) FMWebSocket.EditorShowEvents = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Room)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            if (!FMWebSocket.EditorShowConnectionEvents)
                            {
                                GUILayout.BeginHorizontal();
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("= Connection Events")) FMWebSocket.EditorShowConnectionEvents = true;
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                GUILayout.BeginHorizontal();
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("- Connection Events")) FMWebSocket.EditorShowConnectionEvents = false;
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("- Room Status Events");
                                    }
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnJoinedLobbyEventProp, new GUIContent("OnJoinedLobbyEvent"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnJoinedRoomEventProp, new GUIContent("OnJoinedRoomEvent"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();
                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("- Room Server (Host only Events)");
                                    }
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("- Grant roomMaster permission: Action_RequestRoomMaster()");
                                    }
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginVertical("box");
                                    {

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnClientConnectedEventProp, new GUIContent("OnClientConnectedEvent"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnClientDisconnectedEventProp, new GUIContent("OnClientDisconnectedEvent"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnConnectionCountChangedEventProp, new GUIContent("OnConnectionCountChangedEvent"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();
                                }
                                GUILayout.EndVertical();
                            }
                        }
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginVertical("box");
                    {
                        if (!FMWebSocket.EditorShowReceiverEvents)
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("= Receiver Events")) FMWebSocket.EditorShowReceiverEvents = true;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("- Receiver Events")) FMWebSocket.EditorShowReceiverEvents = false;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();

                            //GUILayout.Label("- Receiver");
                            GUILayout.BeginVertical("box");
                            {
                                if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Room)
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(OnReceivedByteDataEventProp, new GUIContent("OnReceivedByteDataEvent"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(OnReceivedStringDataEventProp, new GUIContent("OnReceivedStringDataEvent"));
                                    GUILayout.EndHorizontal();
                                }

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(GetRawReceivedByteDataEventProp, new GUIContent("GetRawReceivedByteDataEvent"));
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(GetRawReceivedStringDataEventProp, new GUIContent("GetRawReceivedStringDataEvent"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!FMWebSocket.EditorShowDebug || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Debug")) FMWebSocket.EditorShowDebug = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Debug")) FMWebSocket.EditorShowDebug = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ShowLogProp, new GUIContent("ShowLog"));
                        GUILayout.EndHorizontal();

                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}