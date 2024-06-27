using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMSolution.FMETP
{
    public class FillCameraBackground : MonoBehaviour
    {
        public GameObject quad;
        private Material material;
        public Camera mainCam;

        private float CW = 1f;
        private float CH = 1f;
        private float CAspect;

        private float TW = 1f;
        private float TH = 1f;
        private float TAspect = 1f;
        private float ScaleMag = 1f;

        private Vector3 quadScale;

        [Range(0.0f, 1.0f)]
        public float Dist = 0.5f;

        // Start is called before the first frame update
        private void Start()
        {
            if (mainCam == null) mainCam = Camera.main;
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            float fov = mainCam.fieldOfView;
            float nearClipplane = mainCam.nearClipPlane;
            float farClipplane = mainCam.farClipPlane;
            float targetDist = nearClipplane + (farClipplane - nearClipplane) * Dist;
            //float CAspect = mainCam.aspect;
            CAspect = (float)Screen.width / (float)Screen.height;

            CH = Mathf.Tan((fov / 2f) * Mathf.Deg2Rad) * targetDist * 2f;
            CW = CAspect * CH;

            ScaleMag = 1f;

            if (material == null) material = quad.GetComponent<Renderer>().material;
            if (material.mainTexture != null)
            {
                TW = material.mainTexture.width;
                TH = material.mainTexture.height;
            }
            TAspect = TW / TH;

            if (TAspect > CAspect)
            {
                ScaleMag = (TAspect / CAspect);
                quadScale = new Vector3(CW * ScaleMag, CH, 1f);
            }
            else
            {
                ScaleMag = (CAspect / TAspect);
                quadScale = new Vector3(CW, CH * ScaleMag, 1f);
            }

            quad.transform.position = mainCam.transform.position + mainCam.transform.forward * targetDist;
            quad.transform.localScale = quadScale;
        }
    }
}