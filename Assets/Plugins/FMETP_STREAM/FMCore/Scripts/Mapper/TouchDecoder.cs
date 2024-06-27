using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMSolution.FMETP
{
    [AddComponentMenu("FMETP/Mapper/TouchDecoder")]
    public class TouchDecoder : MonoBehaviour
    {
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 9001;

        private int touchCount = 0;
        private Touch[] touches;
        public int TouchCount { get { return touchCount; } }
        public Touch[] Touches { get { return touches; } }

        [SerializeField] private int screenWidth = 0;
        [SerializeField] private int screenHeight = 0;

        [SerializeField] private int touchScreenWidth = 0;
        [SerializeField] private int touchScreenHeight = 0;

        public UnityEventInputTouch OnTouchUpdatedEvent = new UnityEventInputTouch();
        public UnityEventInputTouch OnScaledTouchUpdatedEvent = new UnityEventInputTouch();

        private void Start()
        {
            touchCount = 0;
            touches = new Touch[10];

            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }

        public void Action_ProcessTouchesData(byte[] byteData)
        {
            if (!enabled) return;
            if (byteData.Length <= 2) return;

            UInt16 _label = BitConverter.ToUInt16(byteData, 0);
            if (_label != label) return;

            touchCount = (int)byteData[2];
            if (touchCount > 0)
            {
                touchScreenWidth = BitConverter.ToUInt16(byteData, 3);
                touchScreenHeight = BitConverter.ToUInt16(byteData, 5);
                for (int i = 0; i < touchCount; i++)
                {
                    int _index = 3 + 4 + (i * (10)); //2 + 1 + (i * (4 + 1 + 4 + 4))

                    touches[i].fingerId = (int)byteData[_index];
                    switch (byteData[_index + 1])
                    {
                        case 0: touches[i].phase = TouchPhase.Began; break;
                        case 1: touches[i].phase = TouchPhase.Moved; break;
                        case 2: touches[i].phase = TouchPhase.Stationary; break;
                        case 3: touches[i].phase = TouchPhase.Ended; break;
                        case 4: touches[i].phase = TouchPhase.Canceled; break;
                    }

                    Vector2 _position = new Vector2(BitConverter.ToSingle(byteData, _index + 2), BitConverter.ToSingle(byteData, _index + 6));

                    touches[i].deltaPosition = _position - touches[i].position;
                    touches[i].position = _position;

                }

                Touch[] _updatedTouches = new Touch[touchCount];
                for (int i = 0; i < _updatedTouches.Length; i++)
                {
                    _updatedTouches[i].fingerId = touches[i].fingerId;
                    _updatedTouches[i].phase = touches[i].phase;
                    _updatedTouches[i].position = touches[i].position;
                    _updatedTouches[i].deltaPosition = touches[i].deltaPosition;
                }
                OnTouchUpdatedEvent.Invoke(_updatedTouches);

                float _ratioWidth = (float)screenWidth / (float)touchScreenWidth;
                float _ratioHeight = (float)screenHeight / (float)touchScreenHeight;
                Touch[] _updatedScaledTouches = new Touch[touchCount];
                for (int i = 0; i < _updatedScaledTouches.Length; i++)
                {
                    _updatedScaledTouches[i].fingerId = touches[i].fingerId;
                    _updatedScaledTouches[i].phase = touches[i].phase;
                    _updatedScaledTouches[i].position = new Vector2(touches[i].position.x * _ratioWidth, touches[i].position.y * _ratioHeight);
                    _updatedScaledTouches[i].deltaPosition = new Vector2(touches[i].deltaPosition.x * _ratioWidth, touches[i].deltaPosition.y * _ratioHeight);
                }
                OnScaledTouchUpdatedEvent.Invoke(_updatedScaledTouches);
            }
            else
            {
                OnTouchUpdatedEvent.Invoke(new Touch[0]);
            }
        }
    }
}