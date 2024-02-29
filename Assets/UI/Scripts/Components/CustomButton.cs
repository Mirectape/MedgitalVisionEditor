using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class AccentButton : CustomButton
    {
        private static readonly string _accentStyle = "custom-button--accent";
        
        public AccentButton(Action clickEvent, string title, string icon = null) : base(clickEvent, title, icon)
        {
            this.AddToClassList(_accentStyle);
        }
    }

    public class CustomButton : VisualElement
    {
        #region Syles

        private static readonly string _iconRegularStyle = "fa";
        private static readonly string _buttonStyle = "custom-button";
        private static readonly string _titleItemStyle = "custom-button__title";
        private static readonly string _iconItemStyle = "custom-button__icon";
        private static readonly string _fontItemStyle = "font-style-medium";

        #endregion

        #region Events & Delegates

        public event Action clicked
        {
            add
            {
                if (this.m_Clickable == null)
                    this.clickable = new Clickable(value);
                else
                    this.m_Clickable.clicked += value;
            }
            remove
            {
                if (this.m_Clickable == null)
                    return;
                this.m_Clickable.clicked -= value;
            }
        }
 
        #endregion
        
        #region Public properties
        
        public Clickable clickable
        {
            get => this.m_Clickable;
            set
            {
                if (this.m_Clickable != null && this.m_Clickable.target == this)
                    this.RemoveManipulator((IManipulator) this.m_Clickable);
                this.m_Clickable = value;
                if (this.m_Clickable == null)
                    return;
                this.AddManipulator((IManipulator) this.m_Clickable);
            }
        }

        public string Icon
        {
            get => _iconLabel?.text;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _iconLabel?.RemoveFromHierarchy();
                    _iconLabel = null;
                }
                else
                {
                    if (_iconLabel == null)
                    {
                        _iconLabel = new Label { pickingMode = PickingMode.Ignore, focusable = false };
                        _iconLabel.AddToClassList(_iconItemStyle);
                        _iconLabel.AddToClassList(_iconRegularStyle);
                        this.Insert(0, _iconLabel);
                    }
                    
                    _iconLabel.text = value;
                }
            }
        }

        #endregion

        #region Private fields

        private Label _iconLabel;
        private Label _titleLabel;

        private Clickable m_Clickable;
        private static readonly string NonEmptyString = " ";
        
        #endregion

        public CustomButton(Action clickEvent, string title, string icon = null)
        {
            this.AddToClassList(_buttonStyle);
            this.clickable = new Clickable(clickEvent);
            this.focusable = true;
            this.tabIndex = 0;

            Icon = icon;
            _titleLabel = new Label(title) { pickingMode = PickingMode.Ignore, focusable = false };
            _titleLabel.AddToClassList(_titleItemStyle);
            _titleLabel.AddToClassList(_fontItemStyle);
            this.Add(_titleLabel);
        }

        public new class UxmlFactory : UnityEngine.UIElements.UxmlFactory<Button, Button.UxmlTraits>
        {
        }

        public new class UxmlTraits : TextElement.UxmlTraits
        {
          public UxmlTraits() => this.focusable.defaultValue = true;
        }
    }
}
