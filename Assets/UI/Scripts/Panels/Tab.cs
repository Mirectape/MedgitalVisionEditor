using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum TabStyle { Regular = 0, Solid = 1 }

public class Tab : Label
{
    #region Syles

    private static readonly string _iconRegularStyle = "fa";
    private static readonly string _iconSolidStyle = "fas";
    private static readonly string _toggledStyle = "toggled";
        
    #endregion
    
    #region Properties

    public TabStyle Style
    {
        get => _tabStyle;
        set
        {
            if(_tabStyle == value) return;
            this.RemoveFromClassList(_tabStyle == TabStyle.Regular ? _iconRegularStyle : _iconSolidStyle);
            _tabStyle = value;
            this.AddToClassList(_tabStyle == TabStyle.Regular ? _iconRegularStyle : _iconSolidStyle);
        }
    }

    public VisualElement ContentParent
    {
        get => _contentParent;
    }

    public bool Value
    {
        get => _value;
        set
        {
            if(_value == value) return;
            _value = value;
            if (value)
            {
                this.AddToClassList(_toggledStyle);
                _contentParent.style.display = DisplayStyle.Flex;
            }
            else
            {
                this.RemoveFromClassList(_toggledStyle);
                _contentParent.style.display = DisplayStyle.None;
            }
        }
    }
        
    #endregion
    
    #region Private fields

    private VisualElement _contentParent;
    private TabStyle _tabStyle;
    private bool _value = false;
    
    #endregion
    
    public Tab(string icon, VisualElement content, TabStyle style = TabStyle.Regular) : base(icon)
    {
        _tabStyle = style;
        _contentParent = content;
        content.style.display = Value ? DisplayStyle.Flex : DisplayStyle.None;
        this.AddToClassList(style == TabStyle.Regular ? _iconRegularStyle : _iconSolidStyle);
    }
}
