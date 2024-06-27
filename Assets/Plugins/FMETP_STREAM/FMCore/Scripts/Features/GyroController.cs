using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FMSolution.FMETP
{
    public class GyroController : MonoBehaviour
    {
        public static GyroController instance;

        private bool gyroEnabled;
        private Gyroscope gyro;

        private GameObject cameraContainer;
        private Quaternion rot;

        public GameObject offsetContainer;
        private Quaternion offsetRot;

        public GameObject CamChildrenGrp;

        public Vector3 localRotAng;

        public bool EnableGyroRoation = true;
        public bool EnableTouchDragRotation = true;
        [Range(1, 10)]
        public int TouchDragRequireFingerCount = 3;

        private void Awake()
        {
            if (instance == null) instance = this;
        }

        // Use this for initialization
        private void Start()
        {
            cameraContainer = new GameObject("Camera Container");
            cameraContainer.transform.position = transform.position;
            transform.SetParent(cameraContainer.transform);

            gyroEnabled = EnableGyro();

            offsetContainer = new GameObject("Offset Container");
            offsetContainer.transform.position = transform.position;
            cameraContainer.transform.SetParent(offsetContainer.transform);
        }

        private bool EnableGyro()
        {
            if (SystemInfo.supportsGyroscope)
            {
                gyro = Input.gyro;
                gyro.enabled = true;

                cameraContainer.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
                rot = new Quaternion(0f, 0f, 1f, 0f);

                gyro.updateInterval = 0.01f;
                return true;
            }

            return false;
        }

        // Update is called once per frame
        private void Update()
        {


            if (gyroEnabled)
            {
                if (EnableGyroRoation) transform.localRotation = gyro.attitude * rot;
            }
            else
            {
                MouseDragRot();
            }


            localRotAng = transform.localRotation.eulerAngles;

            //for debug only
            if (EnableTouchDragRotation) TouchDragRot();
        }

        //float offsetAng = 0f;
        public void AimingOffset(float _angle)
        {
            offsetContainer.transform.Rotate(new Vector3(0f, _angle, 0f), Space.World);
        }

        private Vector2 MD_oldPos;
        private Vector2 MD_currentPos;
        private Vector2 MD_deltaPos;
        private float MD_speed = 3000f;
        private void MouseDragRot()
        {
            if (Input.GetMouseButtonDown(1))
            {
                MD_currentPos.x = Input.mousePosition.x / Screen.width;
                MD_currentPos.y = Input.mousePosition.y / Screen.height;
            }
            else if (Input.GetMouseButton(1))
            {
                MD_oldPos = MD_currentPos;
                MD_currentPos.x = Input.mousePosition.x / Screen.width;
                MD_currentPos.y = Input.mousePosition.y / Screen.height;

                MD_deltaPos = MD_currentPos - MD_oldPos;
                offsetContainer.transform.Rotate(new Vector3(0f, -MD_deltaPos.x * Time.deltaTime * MD_speed, 0f), Space.World);
                offsetContainer.transform.Rotate(new Vector3(MD_deltaPos.y * Time.deltaTime * MD_speed, 0f, 0f), Space.Self);
            }
        }

        private bool ignoreFirstTouch = false;
        private void TouchDragRot()
        {
            if (Input.touchCount >= TouchDragRequireFingerCount)
            {
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    MD_currentPos.x = Input.touches[0].position.x / Screen.width;
                    MD_currentPos.y = Input.touches[0].position.y / Screen.height;
                }
                else
                {
                    MD_oldPos = MD_currentPos;
                    MD_currentPos.x = Input.touches[0].position.x / Screen.width;
                    MD_currentPos.y = Input.touches[0].position.y / Screen.height;

                    MD_deltaPos = MD_currentPos - MD_oldPos;
                }

                if (ignoreFirstTouch)
                {
                    ignoreFirstTouch = false;
                }
                else
                {
                    offsetContainer.transform.Rotate(new Vector3(0f, -MD_deltaPos.x * Time.deltaTime * MD_speed, 0f), Space.World);
                }

            }

            if (Input.touchCount == 0) ignoreFirstTouch = true;
        }
    }
}