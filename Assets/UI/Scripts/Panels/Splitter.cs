using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public enum SplitterDirection {Horizontal = 0, Vertical = 1};
    
    public class Splitter : VisualElement
    {
        #region Styles
        
        private static string splitterHorizontal = "horizontal";
        private static string splitterVertical = "vertical";

        #endregion
        
        #region Properties
    
        public SplitterDirection Direction { get => _direction; set => SetDirection(value); }
        
        #endregion
    
        #region Private fields

        private VisualElement _parent;
        private SplitterDirection _direction;
        private SplitterAnchor _splitterAnchor;
        
        #endregion

        #region Delegates/Events

        public delegate void DragInitiator(Splitter splitter);
        public delegate void DragUpdate(Vector2 delta);
        public event DragInitiator OnDragStartHandler;
        public event DragUpdate OnDragUpdateHandler;
        public event DragInitiator OnDragEndHandler;
        
        #endregion
        
        public Splitter(VisualElement parent, SplitterDirection direction)
        {
            _parent = parent;
            _parent.Add(this);
            _direction = direction;

            _splitterAnchor = new SplitterAnchor(this);
            _splitterAnchor.Manipulator.OnDragStartHandler += OnDragStartSplitter;
            _splitterAnchor.Manipulator.OnDragEndHandler += OnDragEndSplitter;
            _splitterAnchor.Manipulator.OnDragUpdateHandler += OnDragUpdateSplitter;

            var style = GetCurrentStyle();
            this.AddToClassList(style);
            _splitterAnchor.AddToClassList(style);
        }

        public void ResetAnchor() => _splitterAnchor.ResetAnchor();
        
        private void OnDragStartSplitter() => OnDragStartHandler?.Invoke(this);

        private void OnDragEndSplitter() => OnDragEndHandler?.Invoke(this);
        
        private void OnDragUpdateSplitter(Vector2 position) => OnDragUpdateHandler?.Invoke(position);

        public void SetDirection(SplitterDirection direction)
        {
            if (_direction == direction) return;
            this.RemoveFromClassList(GetCurrentStyle());
            _direction = direction;
            this.AddToClassList(GetCurrentStyle());
        }

        private string GetCurrentStyle()
        {
            return _direction switch
            {
                SplitterDirection.Vertical => splitterVertical,
                SplitterDirection.Horizontal => splitterHorizontal,
                _ => ""
            };
        }

        public new void SetEnabled(bool value)
        {
            base.SetEnabled(value);
            _splitterAnchor.SetEnabled(value);
        }
    }
}
