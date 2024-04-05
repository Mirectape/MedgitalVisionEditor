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
    public int Rows { get; set; } //(0028,0010)
    public int Columns { get; set; } //(0028,0011)
    // number of slices can be counted through Count() method applies to an instance of this class

    public double[] PixelSpacing {  get; set; } //(0028, 0030) 
    public double SliceThickness {  get; set; } //(0018, 0050) 
}

//ImageOrientationPatient:
// 1 0 0 = LR
// -1 0 0 = RL
// 0 1 0 = AP
// 0 -1 0 = PA
// 0 0 1 = IS
// 0 0 -1 = SI