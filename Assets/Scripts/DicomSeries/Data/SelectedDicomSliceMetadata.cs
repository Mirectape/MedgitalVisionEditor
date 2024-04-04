using UnityEngine;

/// <summary>
/// Keeps selected metadata that changes every slice
/// </summary>
public class SelectedDicomSliceMetadata
{
    public string SOPInstanceUID { get; set; } //(0008,0018)
    public int InstanceNumber { get; set; } //(0020,0013)
    public Vector3 ImagePositionPatient { get; set; } //(0020,0032)
    public double[] ImageOrientationPatient { get; set; } // (0020,0037)
    public DicomSliceOrder DicomSliceOrder { get; set; }
}

//ImageOrientationPatient:
// 1 0 0 = LR
// -1 0 0 = RL
// 0 1 0 = AP
// 0 -1 0 = PA
// 0 0 1 = IS
// 0 0 -1 = SI