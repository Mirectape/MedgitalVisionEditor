using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Localization.LocalizationTableCollection;

public class DicomVolumeTransformer : MonoBehaviour
{
    // 1 0 0 = LR
    // -1 0 0 = RL
    // 0 1 0 = AP
    // 0 -1 0 = PA
    // 0 0 1 = IS
    // 0 0 -1 = SI

    private UnityEngine.Transform _outerObject;
    private Vector3 _focalPoint = new Vector3(0, 0, 0);
    private bool _isAtCenter = true;

    private Vector4 LR = new Vector4(1, 0, 0, 0);
    private Vector4 RL = new Vector4(-1, 0, 0, 0);
    private Vector4 AP = new Vector4(0, 1, 0, 0);
    private Vector4 PA = new Vector4(0, -1, 0, 0);
    private Vector4 IS = new Vector4(0, 0, 1, 0);
    private Vector4 SI = new Vector4(0, 0, -1, 0);

    // Inversion -> Rotation(reflection included) -> Scale -> Translation(if needed)
    private void Awake()
    {
        DicomVolumeBuilder.onVolumeBuilt += ApplyTransformations;
    }

    public void GetOuterObject(UnityEngine.Transform outerObject) => _outerObject = outerObject;

    public void ApplyTransformations(UnityEngine.Transform outerObject)
    {
        GetOuterObject(outerObject);
        ApplyInversion();
        ApplyRotation();
        ApplyScale();
    }

    private void ApplyInversion()
    {
        Vector4 row1 = DicomDataHandler.SlicesOrientationMatrix.GetRow(0);
        Vector4 row2 = DicomDataHandler.SlicesOrientationMatrix.GetRow(1);
        Vector4 row3 = DicomDataHandler.SlicesOrientationMatrix.GetRow(2);

        if (row1 == LR || row1 == RL)
        {
            _outerObject.localScale = new Vector3(-1 * _outerObject.transform.localScale.x,
                _outerObject.transform.localScale.y, _outerObject.transform.localScale.z);
        }
        if (row2 == LR || row2 == RL)
        {
            _outerObject.localScale = new Vector3(_outerObject.transform.localScale.x,
                -1 * _outerObject.transform.localScale.y, _outerObject.transform.localScale.z);
        }
        if (row3 == LR || row3 == RL)
        {
            _outerObject.localScale = new Vector3(_outerObject.transform.localScale.x,
                _outerObject.transform.localScale.y, -1 * _outerObject.transform.localScale.z);
        }
    }

    private void ApplyRotation()
    {
        if (DicomDataHandler.DicomSliceOrder != DicomSliceOrder.RotatedOrder ||
            DicomDataHandler.DicomSliceOrder != DicomSliceOrder.UnknownOrder)
        {
            ApplyRotationToRightOrder(DicomDataHandler.SlicesOrientationMatrix);
        }

        if (DicomDataHandler.DicomSliceOrder == DicomSliceOrder.RotatedOrder)
        {
            ApplyRotationToRotatedOrder(DicomDataHandler.SlicesOrientationMatrix);
        }
        if (DicomDataHandler.DicomSliceOrder == DicomSliceOrder.UnknownOrder)
        {
            Debug.LogError("The order is broken!");
        }

        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f); // y <--> z 
        _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f); // LPS -> RAS

        //Check if rotated
        //if (DicomDataHandler.SelectedSlicesMetadata[0].ImagePositionPatient.x < -1 ||
        //    DicomDataHandler.SelectedSlicesMetadata[0].ImagePositionPatient.y < -1 ||
        //    DicomDataHandler.SelectedSlicesMetadata[0].ImagePositionPatient.z < -1)
        //{
        //    if (DicomDataHandler.SelectedSlicesMetadata[10].ImagePositionPatient.x < -1 ||
        //        DicomDataHandler.SelectedSlicesMetadata[10].ImagePositionPatient.y < -1 ||
        //        DicomDataHandler.SelectedSlicesMetadata[10].ImagePositionPatient.z < -1)
        //    {
        //        Debug.LogWarning("Image was rotated!");
        //    }
        //}
    }

    private void ApplyRotationToRightOrder(Matrix4x4 orientationMatrix)
    {
        Vector4 row1 = orientationMatrix.GetRow(0);
        Vector4 row2 = orientationMatrix.GetRow(1);
        Vector4 row3 = orientationMatrix.GetRow(2);
        if (row1 != LR || row1 != RL)
        {
            if (row1 == SI || row1 == IS)
            {
                if (row1 == IS)
                {
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);

                }

                if (row2 == RL)
                {
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                }

                if (row2 == LR)
                {
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                }

                if (row1 == SI)
                {
                    if (row3 == RL)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                    }

                    if (row3 == LR)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                    }
                }
                if (row1 == IS)
                {
                    if (row3 == LR)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                    }

                    if (row3 == RL)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 90f);
                    }
                }
            }

            if (row1 == AP || row1 == PA)
            {
                if (row1 == AP)
                {
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                }

                if (row2 == RL)
                {
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                }
                if (row2 == LR)
                {
                    _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                }

                if (row1 == PA)
                {
                    if (row3 == RL)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                    }
                    if (row3 == LR)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                    }
                }
                if (row1 == AP)
                {
                    if (row3 == LR)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                    }
                    if (row3 == RL)
                    {
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
                        _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 90f);
                    }
                }
            }
        }
        if (row1 == LR || row1 == RL)
        {
            if (row1 == RL && row2 == AP)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
            }

            if (row1 == LR && row2 == PA)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
            }

            if (row1 == LR && row2 == AP)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.forward, 180f);
            }

            if (row1 == RL && row2 == IS)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
            }

            if (row1 == RL && row2 == SI)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
            }

            if (row1 == LR && row2 == IS)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 90f);
            }

            if (row1 == LR && row2 == SI)
            {
                _outerObject.transform.RotateAround(_focalPoint, Vector3.right, 180f);
                _outerObject.transform.RotateAround(_focalPoint, Vector3.up, 180f);
                _outerObject.transform.RotateAround(_focalPoint, Vector3.right, -90f);
            }
        }
    }

    private void ApplyRotationToRotatedOrder(Matrix4x4 orientationMatrix)
    {
        Vector4[] rows = new Vector4[3];
        Matrix4x4 rightOrderOrientationMatrix = Matrix4x4.identity;

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = orientationMatrix.GetRow(i);
            if (rows[i].x > 0.5f) { rightOrderOrientationMatrix.SetRow(i, LR); }
            if (rows[i].x < -0.5f) { rightOrderOrientationMatrix.SetRow(i, RL); }
            if (rows[i].y > 0.5f) { rightOrderOrientationMatrix.SetRow(i, AP); }
            if (rows[i].y < -0.5f) { rightOrderOrientationMatrix.SetRow(i, PA); }
            if (rows[i].z > 0.5f) { rightOrderOrientationMatrix.SetRow(i, IS); }
            if (rows[i].z < -0.5f) { rightOrderOrientationMatrix.SetRow(i, SI); }
        }
        ApplyRotationToRightOrder(rightOrderOrientationMatrix);
    }

    private void ApplyScale()
    {
        var imageDimensions = DicomDataHandler.MainImage.GetSize();
        var imageSpacing = DicomDataHandler.MainImage.GetSpacing();

        uint rows = imageDimensions[0];
        uint columns = imageDimensions[1];
        uint numberOfSlices = imageDimensions[2];

        double pixelSpacingRow = imageSpacing[0];
        double pixelSpacingColumn = imageSpacing[1];
        double spacingBetweenSlices = imageSpacing[2];

        _outerObject.transform.localScale = new Vector3((float)(_outerObject.transform.localScale.x * rows * pixelSpacingRow / 1000),
                                                        (float)(_outerObject.transform.localScale.y * columns * pixelSpacingColumn / 1000),
                                                        (float)(_outerObject.transform.localScale.z * numberOfSlices * spacingBetweenSlices / 1000));
    }

    public void ApplyTranslation()
    {
        if(_outerObject != null)
        {
            Vector3 imageOriginSigns = new Vector3(-1, 1, -1);

            var imageOrigin = new Vector3((float)DicomDataHandler.MainImage.GetOrigin()[0] * imageOriginSigns.x/1000,
                                        (float)DicomDataHandler.MainImage.GetOrigin()[2] * imageOriginSigns.y/1000,
                                        (float)DicomDataHandler.MainImage.GetOrigin()[1] * imageOriginSigns.z/ 1000);

            if (_isAtCenter)
            {
                _outerObject.transform.localPosition = new Vector3(_outerObject.localPosition.x - DicomVolumeBuilder.InitialBuildingPoint.x + imageOrigin.x,
                                                                   _outerObject.localPosition.y - DicomVolumeBuilder.InitialBuildingPoint.y + imageOrigin.y,
                                                                   _outerObject.localPosition.z - DicomVolumeBuilder.InitialBuildingPoint.z + imageOrigin.z);
                _isAtCenter = false;
            }
            else
            {
                _outerObject.transform.localPosition = new Vector3(_outerObject.localPosition.x + DicomVolumeBuilder.InitialBuildingPoint.x - imageOrigin.x,
                                                                   _outerObject.localPosition.y + DicomVolumeBuilder.InitialBuildingPoint.y - imageOrigin.y,
                                                                   _outerObject.localPosition.z + DicomVolumeBuilder.InitialBuildingPoint.z - imageOrigin.z);
                _isAtCenter = true;
            }
        }
    } 
}
