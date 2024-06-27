using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FMSolution.FMWebSocket;

namespace FMSolution.FMWebSocket
{
    public class FMWebSocketNetwork_debug : MonoBehaviour
    {
        public Text debugText;
        public void Action_SendStringAll(string _string)
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToAll(_string);
        }
        public void Action_SendStringServer(string _string)
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToServer(_string);
        }

        public void Action_SendStringOthers(string _string)
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToOthers(_string);
        }

        public void Action_SendByteAll()
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToAll(new byte[3]);
        }
        public void Action_SendByteServer()
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToServer(new byte[4]);
        }
        public void Action_SendByteOthers()
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToOthers(new byte[5]);
        }

        [SerializeField] private string targetWSID = "12345678-1234";
        public void Action_SetTargetWSID(string _wsid) { targetWSID = _wsid; }
        public void Action_SendStringTarget(string _string)
        {
            if(FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToTarget(_string, targetWSID);
        }
        public void Action_SendByteTarget()
        {
            if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToTarget(new byte[8], targetWSID);
        }

        // Update is called once per frame
        private void Update()
        {
            debugText.text = "";
            if (FMWebSocketManager.instance != null)
            {
                debugText.text += "[connected: " + FMWebSocketManager.instance.Settings.ConnectionStatus + "]";
                debugText.text += "\nwsid: " + FMWebSocketManager.instance.Settings.wsid + (FMWebSocketManager.instance.Settings.wsRoomMaster ? " (roomMaster)" : " (roomClient)");
            }
            debugText.text += "\n" + _received;
        }

        private string _received = "";
        public void Action_OnReceivedData(string _string)
        {
            //debugText.text = "received: " + _string;
            _received = "received: " + _string;
        }
        public void Action_OnReceivedData(byte[] _byte)
        {
            //debugText.text = "received(byte): " + _byte.Length;
            _received = "received(byte): " + _byte.Length;
        }
    }
}