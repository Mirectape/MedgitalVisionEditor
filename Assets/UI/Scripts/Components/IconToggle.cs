using System;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class IconToggle : Button
    {
        #region Styles

        private static readonly string _iconRegularStyle = "fa";
        private static readonly string _iconSolidStyle = "fas";
        private static readonly string _iconToggleStyle = "icon-toggle";

        #endregion

        #region Private fields
        
        private Action _actionEnable;
        private Action _actionDisable;
        private string _iconEnable;
        private string _iconDisable;

        private bool _isToggled = false;

        #endregion
        
        public IconToggle(string iconEnable, Action actionEnable, string iconDisable, Action actionDisable, bool value = false)
        {
            _isToggled = value;
            
            this._iconEnable = iconEnable;
            this._iconDisable = iconDisable;
            this._actionEnable = actionEnable;
            this._actionDisable = actionDisable;
            
            this.AddToClassList(_iconRegularStyle);
            this.AddToClassList(_iconToggleStyle);
            
            this.text = value ? iconEnable : iconDisable;
            this.clicked += ToggleValue;
        }

        private void ToggleValue()
        {
            _isToggled = !_isToggled;
            
            if (_isToggled)
            {
                _actionEnable?.Invoke();
                this.text = _iconEnable;  
            }
            else
            {
                _actionDisable?.Invoke();
                this.text = _iconDisable;      
            }
        }

        public void SetValueWithoutNotify()
        {
            _isToggled = !_isToggled;

            this.text = _isToggled ? _iconEnable : _iconDisable;
        }
    }
}