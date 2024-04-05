using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DicomVolumeElastix : MonoBehaviour
{
    private List<DicomSeries> _dicomSeries;

    private void Start()
    {
        _dicomSeries = DicomDataHandler.DicomSeriesList;
    }

    public void PrintDicomSeriesData()
    {
        foreach (var series in _dicomSeries)
        {
            Debug.Log(series);
        }
    }
}
