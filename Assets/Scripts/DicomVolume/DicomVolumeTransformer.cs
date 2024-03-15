using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DicomVolumeTransformer : MonoBehaviour
{
    // 1 0 0 = LR
    // -1 0 0 = RL
    // 0 1 0 = AP
    // 0 -1 0 = PA
    // 0 0 1 = IS
    // 0 0 -1 = SI

    private Vector3 _focalPoint = new Vector3(0, 0, 0);
    private Vector3 _rotationAxis;
    private float _rotationAngle;

    private Vector4 LR = new Vector4(1, 0, 0, 0);
    private Vector4 RL = new Vector4(-1, 0, 0, 0);
    private Vector4 AP = new Vector4(0, 1, 0, 0);
    private Vector4 PA = new Vector4(0, -1, 0, 0);
    private Vector4 IS = new Vector4(0, 0, 1, 0);
    private Vector4 SI = new Vector4(0, 0, -1, 0);


    private void Awake()
    {
        DicomVolumeBuilder.onVolumeBuilt += ApplyRotation;
    }

    private void ApplyRotation(UnityEngine.Transform outerObject)
    {
        if (DicomDataHandler.SelectedSlicesMetadata[0].DicomSliceOrder != DicomSliceOrder.RotatedOrder ||
            DicomDataHandler.SelectedSlicesMetadata[0].DicomSliceOrder != DicomSliceOrder.UnknownOrder)
        {
            Vector4 row1 = DicomDataHandler.SlicesOrientationMatrix.GetRow(0);
            Vector4 row2 = DicomDataHandler.SlicesOrientationMatrix.GetRow(1);
            Vector4 row3 = DicomDataHandler.SlicesOrientationMatrix.GetRow(2);
            if(row1 != LR || row1 != RL)
            {
                if(row1 == SI || row1 == IS)
                {
                    if(row1 == IS)
                    {
                        outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                    }

                    if(row2 == RL)
                    {
                        outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                        outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                    }

                    if(row2 == LR) 
                    {
                        outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
                        outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                    }

                    if(row1 == SI)
                    {
                        if (row3 == RL) 
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                        }

                        if (row3 == LR)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                        }
                    }
                    if(row1 == IS)
                    {
                        if (row3 == LR)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                        }

                        if (row3 == RL)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                        }
                    }
                }

                if (row1 == AP || row1 == PA)
                {
                    if(row1 == AP)
                    {
                        outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                    }

                    if (row2 == RL)
                    {
                        outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                        outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                    }
                    if (row2 == LR)
                    {
                        outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                    }

                    if(row1 == PA)
                    {
                        if (row3 == RL)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                        }
                        if (row3 == LR)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                        }
                    }
                    if (row1 == AP)
                    {
                        if (row3 == LR)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                        }
                        if (row3 == RL)
                        {
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                            outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                        }
                    }
                }
            }
            if (row1 == LR || row1 == RL)
            {
                if (row1 == RL && row2 == AP)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                }

                if (row1 == LR && row2 == PA)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);

                }

                if (row1 == LR && row2 == AP)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 180f);
                }

                if(row1 == RL && row2 == IS)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                }

                if (row1 == RL && row2 == SI)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
                }

                if (row1 == LR && row2 == IS)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                    outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
                }

                if (row1 == LR && row2 == SI)
                {
                    outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                    outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                    outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
                }
            }
        }

        if (DicomDataHandler.SelectedSlicesMetadata[0].DicomSliceOrder == DicomSliceOrder.RotatedOrder)
        {
            Debug.LogError("Rotated scans are not supported in this programm yet. If you want to continue, please, format your scans into " +
                "the non-rotated orthoganal order and upload anew!");
        }
        if (DicomDataHandler.SelectedSlicesMetadata[0].DicomSliceOrder == DicomSliceOrder.UnknownOrder)
        {
            Debug.LogError("The order is broken!");
        }

        outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f); // y <--> z 
        outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f); // LPS -> RAS

        //Check if inverted
        if (DicomDataHandler.SelectedSlicesMetadata[0].ImagePositionPatient.x < -1 ||
            DicomDataHandler.SelectedSlicesMetadata[0].ImagePositionPatient.y < -1 ||
            DicomDataHandler.SelectedSlicesMetadata[0].ImagePositionPatient.z < -1)
        {
            if (DicomDataHandler.SelectedSlicesMetadata[10].ImagePositionPatient.x < -1 ||
                DicomDataHandler.SelectedSlicesMetadata[10].ImagePositionPatient.y < -1 ||
                DicomDataHandler.SelectedSlicesMetadata[10].ImagePositionPatient.z < -1)
            {
                outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                Debug.LogWarning("Image was inverted!");
            }
        }
    }
}
