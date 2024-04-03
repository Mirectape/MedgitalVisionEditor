using System.Collections.Generic;
using UnityEngine;

public class DicomSeries
{
    public itk.simple.Image MainImage => _mainImage;
    public List<SeriesInfo> SeriesInfos => _seriesInfos;
    public List<SelectedDicomSliceMetadata> SelectedSlicesMetadata => _selectedSlicesMetadata;
    public Matrix4x4 SlicesOrientationMatrix => _slicesOrientationMatrix;

    private itk.simple.Image _mainImage;
    private List<SeriesInfo> _seriesInfos = new List<SeriesInfo>();
    private List<SelectedDicomSliceMetadata> _selectedSlicesMetadata;
    private Matrix4x4 _slicesOrientationMatrix;

    public DicomSeries(itk.simple.Image mainImage, List<SeriesInfo> seriesInfos, 
        List<SelectedDicomSliceMetadata> selectedSlicesMetadata, Matrix4x4 slicesOrientationMatrix)
    {
        _mainImage = mainImage;
        _seriesInfos = seriesInfos;
        _selectedSlicesMetadata = selectedSlicesMetadata;
        _slicesOrientationMatrix = slicesOrientationMatrix;
    }
}