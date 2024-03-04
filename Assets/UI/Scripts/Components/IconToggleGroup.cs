using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class IconToggleGroup : VisualElement
    {
        #region Style fields
        
        private static readonly string _elementsGroupStyle = "elements-group"; 
        private static readonly string _toggledStyle = "icon-toggle--toggled";
        
        #endregion
        
        private List<IconButton> _buttons = new List<IconButton>();
        private IconButton _activeButton;

        public IconToggleGroup()
        {
            this.AddToClassList(_elementsGroupStyle);
        }

        public IconToggleGroup AddToggle(Action action, string icon, bool initialState = false)
        {
            IconButton button = new IconButton(action, icon);
            button.clicked += () => ButtonClicked(button);
            _buttons.Add(button);
            this.Add(button);

            // Если initialState == true, устанавливаем эту кнопку активной
            if (initialState && _activeButton == null)
            {
                _activeButton = button;
                button.AddToClassList(_toggledStyle);
            }

            return this;
        }
        
        public void RemoveButton(IconButton button)
        {
            if (_buttons.Contains(button))
            {
                _buttons.Remove(button);
                this.Remove(button);

                if (_activeButton == button)
                {
                    _activeButton = null;
                }
            }
        }
        
        private void ButtonClicked(IconButton clickedButton)
        {
            if (_activeButton != clickedButton)
            {
                if (_activeButton != null)
                {
                    _activeButton.RemoveFromClassList(_toggledStyle);
                }

                _activeButton = clickedButton;
                _activeButton.AddToClassList(_toggledStyle);

                // Отключаем все остальные кнопки
                foreach (var button in _buttons)
                {
                    if (button != _activeButton)
                    {
                        button.RemoveFromClassList(_toggledStyle);
                    }
                }
            }
        }
    }
}