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
                if(row1 == SI)
                {
                    if(row2 == RL)
                    {
                        //
                        if (row3 == AP)
                        {

                        }
                        if (row3 == PA)
                        {

                        }
                    }
                    if(row2 == LR)
                    {
                        //
                        if (row3 == AP)
                        {

                        }
                        if (row3 == PA)
                        {

                        }
                    }
                    if(row3 == RL)
                    {
                        _rotationAxis = Vector3.up;
                        _rotationAngle = 90f;
                        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.AngleAxis(_rotationAngle, _rotationAxis));
                        outerObject.transform.rotation = rotationMatrix.rotation * transform.rotation;
                        if(row2 == AP)
                        {

                        }
                        if(row2 == PA)
                        {

                        }
                    }
                    if(row3 == LR)
                    {
                        //
                        if (row2 == AP)
                        {

                        }
                        if (row2 == PA)
                        {

                        }
                    }
                }

                if(row1 == IS)
                {
                    if (row2 == RL)
                    {
                        //
                        if (row3 == AP)
                        {

                        }
                        if (row3 == PA)
                        {

                        }
                    }
                    if (row2 == LR)
                    {
                        //
                        if (row3 == AP)
                        {

                        }
                        if (row3 == PA)
                        {

                        }
                    }
                    if (row3 == RL)
                    {
                        _rotationAxis = Vector3.up;
                        _rotationAngle = 90f;
                        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.AngleAxis(_rotationAngle, _rotationAxis));
                        outerObject.transform.rotation = rotationMatrix.rotation * transform.rotation;
                        if (row2 == AP)
                        {

                        }
                        if (row2 == PA)
                        {

                        }
                    }
                    if (row3 == LR)
                    {
                        //
                        if (row2 == AP)
                        {

                        }
                        if (row2 == PA)
                        {

                        }
                    }
                }

                if (row1 == AP)
                {
                    if (row2 == RL)
                    {
                        //
                        if (row3 == SI)
                        {

                        }
                        if (row3 == IS)
                        {

                        }
                    }
                    if (row2 == LR)
                    {
                        //
                        if (row3 == SI)
                        {

                        }
                        if (row3 == IS)
                        {

                        }
                    }
                    if (row3 == RL)
                    {
                        _rotationAxis = Vector3.up;
                        _rotationAngle = 90f;
                        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.AngleAxis(_rotationAngle, _rotationAxis));
                        outerObject.transform.rotation = rotationMatrix.rotation * transform.rotation;
                        if (row2 == SI)
                        {

                        }
                        if (row2 == IS)
                        {

                        }
                    }
                    if (row3 == LR)
                    {
                        //
                        if (row2 == SI)
                        {

                        }
                        if (row2 == IS)
                        {

                        }
                    }
                }

                if(row1 == PA)
                {
                    if (row2 == RL)
                    {
                        //
                        if (row3 == SI)
                        {

                        }
                        if (row3 == IS)
                        {

                        }
                    }
                    if (row2 == LR)
                    {
                        //
                        if (row3 == SI)
                        {

                        }
                        if (row3 == IS)
                        {

                        }
                    }
                    if (row3 == RL)
                    {
                        _rotationAxis = Vector3.up;
                        _rotationAngle = 90f;
                        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.AngleAxis(_rotationAngle, _rotationAxis));
                        outerObject.transform.rotation = rotationMatrix.rotation * transform.rotation;
                        if (row2 == SI)
                        {

                        }
                        if (row2 == IS)
                        {

                        }
                    }
                    if (row3 == LR)
                    {
                        //
                        if (row2 == SI)
                        {

                        }
                        if (row2 == IS)
                        {

                        }
                    }
                }
            }
            else if (row1 == LR || row1 == RL)
            {
                if(row1 == RL)
                {

                }

                if(row2 == AP)
                {
                    if(row3 == SI)
                    {

                    }
                    if (row3 == IS)
                    {

                    }
                }
                if(row2 == PA)
                {
                    if (row3 == SI)
                    {

                    }
                    if (row3 == IS)
                    {

                    }
                }
                if(row2 == SI)
                {
                    if (row3 == PA)
                    {

                    }
                    if (row3 == AP)
                    {

                    }
                }
                if(row2 == IS)
                {
                    if (row3 == PA)
                    {

                    }
                    if (row3 == AP)
                    {

                    }
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
    }
}
