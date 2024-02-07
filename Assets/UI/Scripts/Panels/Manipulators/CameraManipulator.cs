using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class CameraManipulator : PointerManipulator
    {
        #region Private
        
        private bool enabled { get; set; }
        private Camera _camera;

        #endregion
        
        // Write a constructor to set target and store a reference to the 
        // root of the visual tree.
        public CameraManipulator(Camera targetCamera)
        {
            _camera = targetCamera;
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
            
            //CameraControl.Instance.SetCamera(_camera);
        }

        // This method checks whether a drag is in progress and whether target has captured the pointer. 
        // If both are true, calculates a new position for target within the bounds of the window.
        private void PointerMoveHandler(PointerMoveEvent evt)
        {
            if (enabled && target.HasPointerCapture(evt.pointerId))
            {
                
            }
        }

        // This method checks whether a drag is in progress and whether target has captured the pointer. 
        // If both are true, makes target release the pointer.
        private void PointerUpHandler(PointerUpEvent evt)
        {
            if (enabled && target.HasPointerCapture(evt.pointerId))
            {
                target.ReleasePointer(evt.pointerId);
                
                //CameraControl.Instance.SetCamera(null);
            }
        }
        
        private void PointerCaptureOutHandler(PointerCaptureOutEvent evt)
        {
            if (enabled) enabled = false;
            
            //CameraControl.Instance.SetCamera(null);
        }
    }

}
