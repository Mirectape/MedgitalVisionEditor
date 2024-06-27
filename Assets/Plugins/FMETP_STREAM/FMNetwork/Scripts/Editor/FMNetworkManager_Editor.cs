using System;
using UnityEngine;
using UnityEditor;

namespace FMSolution.FMNetwork
{
    [CustomEditor(typeof(FMNetworkManager))]
    [CanEditMultipleObjects]
    public class FMNetworkManager_Editor : UnityEditor.Editor
    {
        private FMNetworkManager FMNetwork;

        SerializedProperty AutoInitProp;
        SerializedProperty NetworkTypeProp;

        SerializedProperty ServerSettings_ServerListenPortProp;
        SerializedProperty ServerSettings_UDPListenerTypeProp;
        SerializedProperty ServerSettings_UseMainThreadSenderProp;
        SerializedProperty ServerSettings_SupportMulticastProp;
        SerializedProperty ServerSettings_MulticastAddressProp;
        SerializedProperty ServerSettings_ConnectionCountProp;

        SerializedProperty ClientSettings_ClientListenPortProp;
        SerializedProperty ClientSettings_UDPListenerTypeProp;
        SerializedProperty ClientSettings_UseMainThreadSenderProp;
        SerializedProperty ClientSettings_AutoNetworkDiscoveryProp;
        SerializedProperty ClientSettings_ServerIPProp;
        SerializedProperty ClientSettings_ForceBroadcastProp;
        SerializedProperty ClientSettings_SupportMulticastProp;
        SerializedProperty ClientSettings_MulticastAddressProp;
        SerializedProperty ClientSettings_IsConnectedProp;

        SerializedProperty DataStreamSettings_DataStreamTypeProp;
        SerializedProperty DataStreamSettings_DataStreamProtocolProp;
        SerializedProperty DataStreamSettings_ClientListenPortProp;
        SerializedProperty DataStreamSettings_UDPTransferTypeProp;
        SerializedProperty DataStreamSettings_UDPListenerTypeProp;
        SerializedProperty DataStreamSettings_TCPSocketTypeProp;
        SerializedProperty DataStreamSettings_MulticastAddressProp;
        SerializedProperty DataStreamSettings_IsConnectedProp;
        SerializedProperty DataStreamSettings_ServerIPProp;
        SerializedProperty DataStreamSettings_ClientIPProp;
        SerializedProperty DataStreamSettings_ClientIPListProp;
        SerializedProperty DataStreamSettings_UseMainThreadSenderProp;

        SerializedProperty DebugStatusProp;
        SerializedProperty ShowLogProp;
        SerializedProperty UIStatusProp;

        SerializedProperty OnReceivedByteDataEventProp;
        SerializedProperty OnReceivedStringDataEventProp;
        SerializedProperty GetRawReceivedDataProp;

        SerializedProperty OnClientConnectedEventProp;
        SerializedProperty OnClientDisconnectedEventProp;
        SerializedProperty OnConnectionCountChangedEventProp;
        SerializedProperty OnFoundServerEventProp;
        SerializedProperty OnLostServerEventProp;

        //SerializedProperty NetworkObjectsProp;
        //SerializedProperty SyncFPSProp;
        //SerializedProperty EnableNetworkObjectsSyncProp;

        void OnEnable()
        {

            AutoInitProp = serializedObject.FindProperty("AutoInit");
            NetworkTypeProp = serializedObject.FindProperty("NetworkType");

            ServerSettings_ServerListenPortProp = serializedObject.FindProperty("ServerSettings.ServerListenPort");
            ServerSettings_UDPListenerTypeProp = serializedObject.FindProperty("ServerSettings.UDPListenerType");
            ServerSettings_UseMainThreadSenderProp = serializedObject.FindProperty("ServerSettings.UseMainThreadSender");
            ServerSettings_SupportMulticastProp = serializedObject.FindProperty("ServerSettings.SupportMulticast");
            ServerSettings_MulticastAddressProp = serializedObject.FindProperty("ServerSettings.MulticastAddress");
            ServerSettings_ConnectionCountProp = serializedObject.FindProperty("ServerSettings.ConnectionCount");


            ClientSettings_ClientListenPortProp = serializedObject.FindProperty("ClientSettings.ClientListenPort");
            ClientSettings_UDPListenerTypeProp = serializedObject.FindProperty("ClientSettings.UDPListenerType");
            ClientSettings_UseMainThreadSenderProp = serializedObject.FindProperty("ClientSettings.UseMainThreadSender");
            ClientSettings_AutoNetworkDiscoveryProp = serializedObject.FindProperty("ClientSettings.AutoNetworkDiscovery");
            ClientSettings_ServerIPProp = serializedObject.FindProperty("ClientSettings.ServerIP");
            ClientSettings_ForceBroadcastProp = serializedObject.FindProperty("ClientSettings.ForceBroadcast");
            ClientSettings_SupportMulticastProp = serializedObject.FindProperty("ClientSettings.SupportMulticast");
            ClientSettings_MulticastAddressProp = serializedObject.FindProperty("ClientSettings.MulticastAddress");
            ClientSettings_IsConnectedProp = serializedObject.FindProperty("ClientSettings.IsConnected");

            DataStreamSettings_DataStreamTypeProp = serializedObject.FindProperty("DataStreamSettings.DataStreamType");
            DataStreamSettings_DataStreamProtocolProp = serializedObject.FindProperty("DataStreamSettings.DataStreamProtocol");
            DataStreamSettings_ClientListenPortProp = serializedObject.FindProperty("DataStreamSettings.ClientListenPort");
            DataStreamSettings_UDPTransferTypeProp = serializedObject.FindProperty("DataStreamSettings.UDPTransferType");
            DataStreamSettings_UDPListenerTypeProp = serializedObject.FindProperty("DataStreamSettings.UDPListenerType");
            DataStreamSettings_TCPSocketTypeProp = serializedObject.FindProperty("DataStreamSettings.TCPSocketType");
            DataStreamSettings_MulticastAddressProp = serializedObject.FindProperty("DataStreamSettings.MulticastAddress");
            DataStreamSettings_IsConnectedProp = serializedObject.FindProperty("DataStreamSettings.IsConnected");
            DataStreamSettings_ServerIPProp = serializedObject.FindProperty("DataStreamSettings.ServerIP");
            DataStreamSettings_ClientIPProp = serializedObject.FindProperty("DataStreamSettings.ClientIP");
            DataStreamSettings_ClientIPListProp = serializedObject.FindProperty("DataStreamSettings.ClientIPList");
            DataStreamSettings_UseMainThreadSenderProp = serializedObject.FindProperty("DataStreamSettings.UseMainThreadSender");

            DebugStatusProp = serializedObject.FindProperty("DebugStatus");
            ShowLogProp = serializedObject.FindProperty("ShowLog");
            UIStatusProp = serializedObject.FindProperty("UIStatus");

            OnReceivedByteDataEventProp = serializedObject.FindProperty("OnReceivedByteDataEvent");
            OnReceivedStringDataEventProp = serializedObject.FindProperty("OnReceivedStringDataEvent");
            GetRawReceivedDataProp = serializedObject.FindProperty("GetRawReceivedData");

            OnClientConnectedEventProp = serializedObject.FindProperty("OnClientConnectedEvent");
            OnClientDisconnectedEventProp = serializedObject.FindProperty("OnClientDisconnectedEvent");
            OnConnectionCountChangedEventProp = serializedObject.FindProperty("OnConnectionCountChangedEvent");
            OnFoundServerEventProp = serializedObject.FindProperty("OnFoundServerEvent");
            OnLostServerEventProp = serializedObject.FindProperty("OnLostServerEvent");

            //NetworkObjectsProp = serializedObject.FindProperty("NetworkObjects");
            //SyncFPSProp = serializedObject.FindProperty("SyncFPS");
            //EnableNetworkObjectsSyncProp = serializedObject.FindProperty("EnableNetworkObjectsSync");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (FMNetwork == null) FMNetwork = (FMNetworkManager)target;

            serializedObject.Update();


            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                {
                    //Header
                    Color32 _color1 = new Color32(70, 157, 76, 255);//#469D4C	
                    Color32 _color2 = new Color32(255, 255, 255, 255);//#ffffff

                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = _color2;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 18;
                    style.fontStyle = FontStyle.Bold;

                    Texture2D backgroundTexture = new Texture2D(1, 1);
                    backgroundTexture.SetPixel(0, 0, _color1);
                    backgroundTexture.Apply();
                    style.normal.background = backgroundTexture;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("\nFM Network 4.0\n", style);
                    GUILayout.EndHorizontal();
                }

                if (!FMNetwork.EditorShowNetworking)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) FMNetwork.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) FMNetwork.EditorShowNetworking = false;
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

                        if (FMNetwork.NetworkType == FMNetworkType.Server || FMNetwork.NetworkType == FMNetworkType.Client)
                        {
                            GUILayout.BeginVertical();
                            {
                                if (!FMNetwork.EditorShowServerSettings)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("= Server Settings")) FMNetwork.EditorShowServerSettings = true;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();

                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("- Server Settings")) FMNetwork.EditorShowServerSettings = false;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(ServerSettings_ServerListenPortProp, new GUIContent("ServerListenPort"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(ServerSettings_UDPListenerTypeProp, new GUIContent("UDPListenerType"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(ServerSettings_UseMainThreadSenderProp, new GUIContent("UseMainThreadSender"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(ServerSettings_ConnectionCountProp, new GUIContent("Connection Count"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(ServerSettings_SupportMulticastProp, new GUIContent("SupportMulticast(Sender)"));
                                        GUILayout.EndHorizontal();

                                        if (FMNetwork.ServerSettings.SupportMulticast)
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ServerSettings_MulticastAddressProp, new GUIContent("Multicast Address"));
                                            GUILayout.EndHorizontal();
                                        }
                                    }
                                    GUILayout.EndVertical();
                                }
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical();
                            {
                                if (!FMNetwork.EditorShowClientSettings)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("= Client Settings")) FMNetwork.EditorShowClientSettings = true;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("- Client Settings")) FMNetwork.EditorShowClientSettings = false;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_ClientListenPortProp, new GUIContent("ClientListenPort"));
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();

                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_UDPListenerTypeProp, new GUIContent("UDPListenerType"));
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_UseMainThreadSenderProp, new GUIContent("UseMainThreadSender"));
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_ForceBroadcastProp, new GUIContent("ForceBroadcast"));
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();

                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_SupportMulticastProp, new GUIContent("SupportMulticast(Receiver)"));
                                            GUILayout.EndHorizontal();

                                            if (FMNetwork.ClientSettings.SupportMulticast)
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(ClientSettings_MulticastAddressProp, new GUIContent("Multicast Address"));
                                                GUILayout.EndHorizontal();
                                            }
                                        }
                                        GUILayout.EndVertical();

                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_AutoNetworkDiscoveryProp, new GUIContent("AutoNetworkDiscovery"));
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_ServerIPProp, new GUIContent("ServerIP"));
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(ClientSettings_IsConnectedProp, new GUIContent("IsConnected"));
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();
                                    }
                                    GUILayout.EndVertical();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        else
                        {
                            GUILayout.BeginVertical();
                            {
                                if (FMNetwork.EditorShowDataStreamSettings)
                                {
                                    GUILayout.BeginHorizontal();

                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("- DataStream Settings")) FMNetwork.EditorShowDataStreamSettings = false;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(DataStreamSettings_DataStreamTypeProp, new GUIContent("DataStreamType"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    if (FMNetwork.DataStreamSettings.DataStreamType == FMDataStreamType.Sender)
                                    {
                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            if (FMNetwork.DataStreamSettings.UDPTransferType == FMUDPTransferType.MultipleUnicast)
                                            {
                                                EditorGUILayout.PropertyField(DataStreamSettings_ClientIPListProp, new GUIContent("Client IP List"));
                                            }
                                            else if (FMNetwork.DataStreamSettings.UDPTransferType == FMUDPTransferType.Unicast)
                                            {
                                                EditorGUILayout.PropertyField(DataStreamSettings_ClientIPProp, new GUIContent("Client IP"));
                                            }
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(DataStreamSettings_ClientListenPortProp, new GUIContent("ClientListenPort"));
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();

                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(DataStreamSettings_UDPTransferTypeProp, new GUIContent("UDP Transfer Type"));
                                            GUILayout.EndHorizontal();

                                            if (FMNetwork.DataStreamSettings.UDPTransferType == FMUDPTransferType.Multicast)
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(DataStreamSettings_MulticastAddressProp, new GUIContent("Multicast Address"));
                                                GUILayout.EndHorizontal();
                                            }

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(DataStreamSettings_UseMainThreadSenderProp, new GUIContent("UseMainThreadSender"));
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();
                                    }
                                    else if (FMNetwork.DataStreamSettings.DataStreamType == FMDataStreamType.Receiver)
                                    {
                                        GUILayout.BeginVertical("box");
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(DataStreamSettings_DataStreamProtocolProp, new GUIContent("DataStreamProtocol"));
                                            GUILayout.EndHorizontal();

                                            if (FMNetwork.DataStreamSettings.TCPSocketType == FMTCPSocketType.TCPClient)
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(DataStreamSettings_ServerIPProp, new GUIContent("Server IP"));
                                                GUILayout.EndHorizontal();
                                            }

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(DataStreamSettings_ClientListenPortProp, new GUIContent("ClientListenPort"));
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(DataStreamSettings_IsConnectedProp, new GUIContent("IsConnected"));
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();

                                        if (FMNetwork.DataStreamSettings.DataStreamProtocol == FMProtocol.UDP)
                                        {
                                            GUILayout.BeginVertical("box");
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(DataStreamSettings_UDPListenerTypeProp, new GUIContent("UDP Listener Type"));
                                                GUILayout.EndHorizontal();
                                            }
                                            GUILayout.EndVertical();
                                        }
                                        else if(FMNetwork.DataStreamSettings.DataStreamProtocol == FMProtocol.TCP)
                                        {
                                            GUILayout.BeginVertical("box");
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(DataStreamSettings_TCPSocketTypeProp, new GUIContent("TCP Socket Type"));
                                                GUILayout.EndHorizontal();
                                            }
                                            GUILayout.EndVertical();
                                        }
                                    }
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("+ DataStream Settings")) FMNetwork.EditorShowDataStreamSettings = true;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            //if (FMNetwork.NetworkType == FMNetworkType.Server || FMNetwork.NetworkType == FMNetworkType.Client)
            //{
            //    GUILayout.Space(2);
            //    GUILayout.BeginVertical("box");
            //    {
            //        if (!FMNetwork.EditorShowSyncTransformation)
            //        {
            //            GUILayout.BeginHorizontal();
            //            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            //            if (GUILayout.Button("+ Sync Transformation")) FMNetwork.EditorShowSyncTransformation = true;
            //            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            //            GUILayout.EndHorizontal();
            //        }
            //        else
            //        {
            //            GUILayout.BeginHorizontal();
            //            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            //            if (GUILayout.Button("- Sync Transformation")) FMNetwork.EditorShowSyncTransformation = false;
            //            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            //            GUILayout.EndHorizontal();

            //            GUILayout.BeginHorizontal();
            //            GUIStyle style = new GUIStyle();
            //            style.normal.textColor = Color.yellow;
            //            GUILayout.Label(" * New method: \n adding FMNetworkTransformView to your target object", style);
            //            GUILayout.EndHorizontal();

            //            //GUILayout.BeginVertical("box");
            //            //{
            //            //    int NetworkObjectsNum = NetworkObjectsProp.FindPropertyRelative("Array.size").intValue;

            //            //    if (!FMNetwork.EditorShowNetworkObjects)
            //            //    {
            //            //        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            //            //        if (GUILayout.Button("= NetworkObjects: " + NetworkObjectsNum)) FMNetwork.EditorShowNetworkObjects = true;
            //            //        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            //            //    }
            //            //    else
            //            //    {
            //            //        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            //            //        if (GUILayout.Button("- NetworkObjects: " + NetworkObjectsNum)) FMNetwork.EditorShowNetworkObjects = false;
            //            //        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            //            //        DrawPropertyArray(NetworkObjectsProp);
            //            //    }

            //            //    //GUILayout.BeginHorizontal();
            //            //    //EditorGUILayout.PropertyField(SyncFPSProp, new GUIContent("SyncFPS"));
            //            //    //GUILayout.EndHorizontal();

            //            //    //GUILayout.BeginHorizontal();
            //            //    //EditorGUILayout.PropertyField(EnableNetworkObjectsSyncProp, new GUIContent("EnableNetworkObjectsSync"));
            //            //    //GUILayout.EndHorizontal();
            //            //}
            //            //GUILayout.EndVertical();
            //        }
            //    }
            //    GUILayout.EndVertical();
            //}

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!FMNetwork.EditorShowEvents)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Events")) FMNetwork.EditorShowEvents = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Events")) FMNetwork.EditorShowEvents = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    if (FMNetwork.NetworkType == FMNetworkType.Server || FMNetwork.NetworkType == FMNetworkType.Client)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            if (!FMNetwork.EditorShowConnectionEvents)
                            {
                                GUILayout.BeginHorizontal();
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("= Connection Events")) FMNetwork.EditorShowConnectionEvents = true;
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                GUILayout.BeginHorizontal();
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("- Connection Events")) FMNetwork.EditorShowConnectionEvents = false;
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginVertical("box");
                                {
                                    if (FMNetwork.NetworkType == FMNetworkType.Server)
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
                                    else if (FMNetwork.NetworkType == FMNetworkType.Client)
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnFoundServerEventProp, new GUIContent("OnFoundServerEvent"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(OnLostServerEventProp, new GUIContent("OnLostServerEvent"));
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndVertical();
                            }
                        }
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginVertical("box");
                    {
                        if (!FMNetwork.EditorShowReceiverEvents)
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("= Receiver Events")) FMNetwork.EditorShowReceiverEvents = true;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("- Receiver Events")) FMNetwork.EditorShowReceiverEvents = false;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();

                            //GUILayout.Label("- Receiver");
                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(OnReceivedByteDataEventProp, new GUIContent("OnReceivedByteDataEvent"));
                                GUILayout.EndHorizontal();

                                if (FMNetwork.NetworkType == FMNetworkType.Server || FMNetwork.NetworkType == FMNetworkType.Client)
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(OnReceivedStringDataEventProp, new GUIContent("OnReceivedStringDataEvent"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(GetRawReceivedDataProp, new GUIContent("GetRawReceivedData"));
                                    GUILayout.EndHorizontal();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                if (!FMNetwork.EditorShowDebug)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Debug")) FMNetwork.EditorShowDebug = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Debug")) FMNetwork.EditorShowDebug = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(DebugStatusProp, new GUIContent("Debug Status"));
                        GUILayout.EndHorizontal();

                        if (FMNetwork.DebugStatus)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Status: ");
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label(FMNetwork.Status);
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(UIStatusProp, new GUIContent("UIStatus"));
                        GUILayout.EndHorizontal();

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

        private void DrawPropertyArray(SerializedProperty property)
        {
            SerializedProperty arraySizeProp = property.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(arraySizeProp);

            EditorGUI.indentLevel++;

            for (int i = 0; i < arraySizeProp.intValue; i++)
            {
                EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i));
            }

            EditorGUI.indentLevel--;
        }
    }
}