using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMSolution.FMETP
{
    [AddComponentMenu("FMETP/Mapper/TouchEncoder")]
    public class TouchEncoder : MonoBehaviour
    {
        public int TouchCount = 0;
        private List<byte> SendByte;
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 9001;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

        private bool detectedTouch = false;

        private void Update()
        {
            TouchCount = Input.touchCount;
            if (TouchCount > 10) TouchCount = 10; //maximum 10 finger touches
            if (TouchCount > 0) detectedTouch = true;
            if (detectedTouch)
            {
                SendByte = new List<byte>();
                SendByte.AddRange(BitConverter.GetBytes(label));
                SendByte.Add((byte)TouchCount);

                UInt16 _screenWidth = (UInt16)Screen.width;
                UInt16 _screenHeight = (UInt16)Screen.height;
                SendByte.AddRange(BitConverter.GetBytes(_screenWidth));
                SendByte.AddRange(BitConverter.GetBytes(_screenHeight));

                Touch[] touches = Input.touches;
                for (int i = 0; i < TouchCount; i++)
                {
                    int _fingerId = touches[i].fingerId;
                    SendByte.Add((byte)_fingerId);
                    byte _phaseByte = (byte)0;
                    switch (touches[i].phase)
                    {
                        case TouchPhase.Began: _phaseByte = (byte)0; break;
                        case TouchPhase.Moved: _phaseByte = (byte)1; break;
                        case TouchPhase.Stationary: _phaseByte = (byte)2; break;
                        case TouchPhase.Ended: _phaseByte = (byte)3; break;
                        case TouchPhase.Canceled: _phaseByte = (byte)4; break;
                    }
                    SendByte.Add(_phaseByte);
                    SendByte.AddRange(BitConverter.GetBytes(touches[i].position.x));
                    SendByte.AddRange(BitConverter.GetBytes(touches[i].position.y));
                }

                OnDataByteReadyEvent.Invoke(SendByte.ToArray());
                SendByte.Clear();

                if (TouchCount == 0) detectedTouch = false; //end...
            }
        }
    }
}