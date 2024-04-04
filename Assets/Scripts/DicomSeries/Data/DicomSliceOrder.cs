
public enum DicomSliceOrder
{
    UnknownOrder = 0,
    IS = 1, SI = 2, //Axial
    LR = 3, RL = 4, //Sagittal
    PA = 5, AP = 6, //Coronal
    RotatedOrder = 7,
}