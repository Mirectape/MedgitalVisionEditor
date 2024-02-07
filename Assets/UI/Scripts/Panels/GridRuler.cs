using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public class GridRuler : VisualElement
{
    #region Style fields
    
    private static readonly string _gridRulerStyle = "grid-ruler";
    private static readonly string _rulerStyle = "ruler";
    private static readonly string _rulerLineStyle = "line";
    private static readonly string _gridRulerLabelStyle = "grid-ruler-label";
    
    #endregion

    #region Private fields
    
    private MeasureDotGrid _measureDotGrid;
    private LocalizedString _measureUnitString = LocalizationManager.GetLocalizedString("lenM");

    private VisualElement _ruler;
    private Label _measureText;
    
    #endregion
    
    private static readonly (int, string)[] units = {
        (1, "lenNm"), (10, "lenNm"), (100, "lenNm"),
        (1, "lenUm"), (10, "lenUm"), (100, "lenUm"),
        (1, "lenMm"),
        (1, "lenCm"), (10, "lenCm"),
        (1, "lenM"), (10, "lenM"), (100, "lenM"),
        (1, "lenKm"), (10, "lenKm"), (100, "lenKm"),
    };
    
    public GridRuler(MeasureDotGrid grid)
    {
        if (grid == null) Debug.LogError("Grid is null");

        _measureDotGrid = grid;
        _measureDotGrid.OnMeterInPixel += GridChange;

        // Build element
        _measureText = new Label();
        _measureText.AddToClassList(_gridRulerLabelStyle);
        this.Add(_measureText);
        
        _ruler = new VisualElement();
        _ruler.AddToClassList(_rulerStyle);
        this.Add(_ruler);

        VisualElement line = new VisualElement();
        line.AddToClassList(_rulerLineStyle);
        _ruler.Add(line);

        this.AddToClassList(_gridRulerStyle);
    }

    private void GridChange(float measures)
    {
        int unitIndex  = 9;
        
        while (measures < 25)
        {
            measures *= 10;
            unitIndex ++;
        }

        while (measures > 125)
        {
            measures /= 10;
            unitIndex --;
        }

        (int, string) unit;
        
        if (unitIndex >= 0 && unitIndex < units.Length)
        {
            unit = units[unitIndex];
            _measureUnitString.TableEntryReference = unit.Item2;
            _measureText.text = $"{unit.Item1} {_measureUnitString.GetLocalizedString()}";
        }
        else
        {
            _measureText.text = $"{unitIndex} error";
        }

        _ruler.style.width = measures;
    }
}
