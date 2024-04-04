using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains patient's unique info
/// </summary>
public class SeriesInfo
{
    public string seriesUID;
    public string patientName;
    public string seriesDescription;
    public string dateStudy;
    public int dimX, dimY, dimZ;
    public bool missingFiles;
    public string dirPath;
}
