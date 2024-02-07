using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public abstract class PanelBase : VisualElement
    {
        public static PanelBase FocusedPanel => _focusPanel;
        public static PanelBase HoveredPanel => _hoverPanel;
        
        private static PanelBase _focusPanel;
        private static PanelBase _hoverPanel;
        
        protected PanelBase()
        {
            this.focusable = true;
        }
        
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            // Call the base function.
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.eventTypeId == PointerOverEvent.TypeId())
            {
                _hoverPanel = this;
            }
            else if (evt.eventTypeId == PointerDownEvent.TypeId())
            {
                if (_focusPanel != this)
                {
                    _focusPanel?.SetUnfocused(); // Сброс стиля для предыдущей фокусированной панели
                    _focusPanel = this;
                    _focusPanel.SetFocused(); // Установка стиля для новой фокусированной панели
                }
            }
            // More event types
        }

        protected virtual void SetFocused() { }

        protected virtual void SetUnfocused() { }

        public virtual void SetPanelEnabled(bool value)
        {
            this.SetEnabled(value);
            if (!value) this.Blur();
        }
    }
}

