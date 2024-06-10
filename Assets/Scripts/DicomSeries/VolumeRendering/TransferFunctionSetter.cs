using itk.simple;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityVolumeRendering;

public class TransferFunctionSetter : MonoBehaviour
{
    private void Awake()
    {
       DicomVolumeBuilder.onVolumeBuilt += AssignVolumeRenderedObject;
    }

    private void AssignVolumeRenderedObject(UnityEngine.Transform volume)
    {
        var volumeRenderedObject = volume.GetComponent<VolumeRenderedObject>();
        if (volumeRenderedObject.dataset != null) Debug.Log("Dataset is present");
        Debug.Log("Max value of HU: " + volumeRenderedObject.dataset.GetMinDataValue());
        Debug.Log("Min value of HU: " + volumeRenderedObject.dataset.GetMaxDataValue());
    }

}
