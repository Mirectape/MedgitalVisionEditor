using UnityEngine;

/// <summary>
/// Keeps selected metadata that changes every slice
/// </summary>
public class SelectedDicomSliceMetadata
{
    public string SOPInstanceUID { get; set; } //(0008,0018)
    public int InstanceNumber { get; set; } //(0020,0013)
    public Vector3 ImagePositionPatient { get; set; } //(0020,0032)
}
