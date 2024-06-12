using itk.simple;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityVolumeRendering;

public static class TransferFunctionSetter
{
    /// <summary>
    /// Returns recalibrated transfer function in the range of ~ -1000 to 4000 HU for color control points
    /// </summary>
    /// <param name="volumeRenderedObject"></param>
    /// <returns></returns>
    public static UnityVolumeRendering.TransferFunction GetRecalibratedTransferFunction(VolumeRenderedObject volumeRenderedObject)
    {
        UnityVolumeRendering.TransferFunction transferFunction = volumeRenderedObject.transferFunction;

        float minValueHounsfieldHU = volumeRenderedObject.dataset.GetMinDataValue();
        float maxValueHounsfieldHU = volumeRenderedObject.dataset.GetMaxDataValue();
        var colourControlPoints = volumeRenderedObject.transferFunction.colourControlPoints; // get only data values
        float differenceHU = Math.Abs(maxValueHounsfieldHU) - Math.Abs(minValueHounsfieldHU);


        //everything that exceeds 5000f needs recalibration
        if (differenceHU >= 5000)
        {
            float ratio = 5000 / (maxValueHounsfieldHU - minValueHounsfieldHU);
            var newColourControlPoints = new List<TFColourControlPoint>();
            for(int i = 0; i < colourControlPoints.Count; i++)
            {
                newColourControlPoints.Add(new TFColourControlPoint(colourControlPoints[i].dataValue * ratio, colourControlPoints[i].colourValue));
            }
            volumeRenderedObject.transferFunction.colourControlPoints = newColourControlPoints;
            Debug.Log("Transfer function was recalibrated");
        }
        return transferFunction;
    }
}
