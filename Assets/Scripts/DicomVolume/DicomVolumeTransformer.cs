using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DicomVolumeTransformer : MonoBehaviour
{
    private Vector3 _rotationAxis;
    private float _rotationAngle;

    private void Awake()
    {
        DicomVolumeBuilder.onVolumeBuilt += ApplyRotation;
    }

    private void ApplyRotation(UnityEngine.Transform outerObject)
    {
        

    }
}
