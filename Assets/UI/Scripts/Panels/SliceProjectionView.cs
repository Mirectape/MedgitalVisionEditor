using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

public class SliceProjectionView : ViewportPanel
{
    #region Style fields

    #endregion
    
    #region Delegates and Events

    public delegate void SliceProjectionViewCreated(SliceProjectionView view);
    public static SliceProjectionViewCreated OnSliceProjectionViewCreated;

    #endregion

    #region Public fields

    public SliceProjectionAxis ProjectionAxis => _axis;

    #endregion

    #region Private fields

    private SliceProjectionAxis _axis;
    private int _layer;
    private MeasureDotGrid _measureDotGrid;
    private GridRuler _gridRuler;
    private SliceSlider _slicePositionSlider;

    #endregion
    
    public SliceProjectionView(int layer, SliceProjectionAxis axis)
    {
        // Initialize
        _axis = axis;
        _layer = layer;
        
        // Camera settings
        _viewCamera.orthographic = true;
        _viewCamera.name = $"Camera_{axis.ToString()}_{layer}";
        _viewCamera.cullingMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5) | (1 << _layer);
        
        // Grid setup
        _measureDotGrid = _viewCamera.gameObject.AddComponent<MeasureDotGrid>();
        _measureDotGrid.LayerCulling = layer;
        _gridRuler = new GridRuler(_measureDotGrid);
        _content.Add(_gridRuler);
        
        // Slider setup
        _slicePositionSlider = new SliceSlider(-0.5f, 0.5f, SliderDirection.Vertical);
        _content.Add(_slicePositionSlider);
        _slicePositionSlider.RegisterValueChangedCallback(OnSliderPositionValueChange);
    }

    private void OnSliderPositionValueChange(ChangeEvent<float> evt)
    {

    }
}
