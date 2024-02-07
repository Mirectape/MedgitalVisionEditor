using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class SplitterManipulator : PointerManipulator
    {
        #region Private
        
        private bool enabled { get; set; }

        #endregion
        
        #region Delegates/Events
        
        public delegate void DragHandler();
        public delegate void DragUpdateHandler(Vector2 delta);
        public event DragHandler OnDragStartHandler;
        public event DragHandler OnDragEndHandler;
        public event DragUpdateHandler OnDragUpdateHandler;

        #endregion
        
        // Write a constructor to set target and store a reference to the 
        // root of the visual tree.
        public SplitterManipulator(VisualElement target)
        {
            this.target = target;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // Register the four callbacks on target.
            target.RegisterCallback<PointerDownEvent>(PointerDownHandler);
            target.RegisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.RegisterCallback<PointerUpEvent>(PointerUpHandler);
            target.RegisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            // Un-register the four callbacks from target.
            target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
            target.UnregisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.UnregisterCallback<PointerUpEvent>(PointerUpHandler);
            target.UnregisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
        }

        // This method stores the starting position of target and the pointer, 
        // makes target capture the pointer, and denotes that a drag is now in progress.
        private void PointerDownHandler(PointerDownEvent evt)
        {
            target.CapturePointer(evt.pointerId);
            enabled = true;
            
            OnDragStartHandler?.Invoke();
        }

        // This method checks whether a drag is in progress and whether target has captured the pointer. 
        // If both are true, calculates a new position for target within the bounds of the window.
        private void PointerMoveHandler(PointerMoveEvent evt)
        {
            if (enabled && target.HasPointerCapture(evt.pointerId))
            {
                OnDragUpdateHandler?.Invoke(evt.position);
            }
        }

        // This method checks whether a drag is in progress and whether target has captured the pointer. 
        // If both are true, makes target release the pointer.
        private void PointerUpHandler(PointerUpEvent evt)
        {
            if (enabled && target.HasPointerCapture(evt.pointerId))
            {
                target.ReleasePointer(evt.pointerId);
                            
                OnDragEndHandler?.Invoke();
            }
        }
        
        private void PointerCaptureOutHandler(PointerCaptureOutEvent evt)
        {
            if (enabled) enabled = false;
        }
    }

}
