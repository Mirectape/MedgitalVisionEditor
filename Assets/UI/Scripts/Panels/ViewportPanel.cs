using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class ViewportPanel : PanelBase, IResizible
    {
        #region Style fields
    
        private static readonly string _contentStyle = "viewport-panel-content";
        private static readonly string _elementsGroupStyle = "elements-group";
        private static readonly string _columnStyle = "elements-group--column";
        private static readonly string _centerButtonStyle = "viewport-button__center";
        private static readonly string _expandButtonStyle = "viewport-button__expand";
        private static readonly string _groupZoomStyle = "viewport-group__zoom";

        #endregion
        
        #region Delegates and events

        public delegate void CameraFocusChangeHandler(Camera camera);
        public static CameraFocusChangeHandler OnCameraFocusChange;

        #endregion
        
        #region Properties
        public static Camera FocusedCamera => _focusedCamera;
        
        #endregion
        
        #region Private fields

        private static Camera _focusedCamera;
        private CameraManipulator _cameraManipulator;
        
        protected Camera _viewCamera;
        protected VisualElement _content;

        #endregion
        
        public ViewportPanel()
        {
            _viewCamera = new GameObject("Camera").AddComponent<Camera>();
            _viewCamera.clearFlags = CameraClearFlags.SolidColor;
            _viewCamera.backgroundColor = new Color(0.1294118f, 0.1254902f, 0.1529412f);
            _viewCamera.orthographicSize = 0.6f;
            _viewCamera.cullingMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5) | (1 << 11);
            _viewCamera.transform.localPosition = Vector3.forward;
            _viewCamera.transform.LookAt(Vector3.zero);

            GameObject cameraOrbitCenter = new GameObject("OrbitCenter");
            cameraOrbitCenter.transform.position = Vector3.zero;
            cameraOrbitCenter.transform.SetParent(_viewCamera.transform, true);
            
            this.RegisterCallback<GeometryChangedEvent>(ResizeCamera);

            _cameraManipulator = new CameraManipulator(_viewCamera);
            this.AddManipulator(_cameraManipulator);

            // UI builder
            _content = new VisualElement();
            _content.pickingMode = PickingMode.Ignore;
            _content.focusable = false;
            _content.AddToClassList(_contentStyle);
            this.Add(_content);

            
            VisualElement groupCenter = new VisualElement();
            groupCenter.AddToClassList(_elementsGroupStyle);
            groupCenter.AddToClassList(_columnStyle);
            groupCenter.AddToClassList(_centerButtonStyle);
            _content.Add(groupCenter);
            
            IconButton centerBtn = new IconButton("\ue4be", () => Debug.Log("CenterButton"));
            groupCenter.Add(centerBtn);
            
            
            VisualElement groupExpand = new VisualElement();
            groupExpand.AddToClassList(_elementsGroupStyle);
            groupExpand.AddToClassList(_columnStyle);
            groupExpand.AddToClassList(_expandButtonStyle);
            _content.Add(groupExpand);
            
            IconToggle expandBtn = new IconToggle(
                "\uf066", () => Workspace.Instance.MaximizePanel(this),
                "\uf065", () => Workspace.Instance.ResetMaximizePanel()
            );
            groupExpand.Add(expandBtn);

            
            VisualElement groupZoom = new VisualElement();
            groupZoom.AddToClassList(_elementsGroupStyle);
            groupZoom.AddToClassList(_columnStyle);
            groupZoom.AddToClassList(_groupZoomStyle);
            _content.Add(groupZoom);
            
            IconButton plusBtn = new IconButton("\u002b", () => Debug.Log("ZoomUp"));
            groupZoom.Add(plusBtn);
            
            IconButton minusBtn = new IconButton("\uf068", () => Debug.Log("ZoomDown"));
            groupZoom.Add(minusBtn);
        }

        private void ResizeCamera(GeometryChangedEvent evt)
        {
            Rect newRect = this.worldBound;
            float normalizedX = newRect.x / Screen.width;
            float normalizedY = (Screen.height - newRect.y - newRect.height) / Screen.height;
            float normalizedWidth = newRect.width / Screen.width;
            float normalizedHeight = newRect.height / Screen.height;

            _viewCamera.rect = new Rect(normalizedX, normalizedY, normalizedWidth, normalizedHeight);
            
            if (_viewCamera.orthographic)
            {
                if (evt.oldRect.height > 0 && evt.newRect.height > 0)
                {
                    float heightRatio = evt.newRect.height / evt.oldRect.height;
                    _viewCamera.orthographicSize *= heightRatio;
                }
            }
        }
        
        protected override void SetFocused()
        {
            base.SetFocused();
            _focusedCamera = _viewCamera;
            OnCameraFocusChange?.Invoke(_focusedCamera);
        }

        protected override void SetUnfocused()
        {
            base.SetUnfocused();
            _focusedCamera = null;
            OnCameraFocusChange?.Invoke(_focusedCamera);
        }

        public override void SetPanelEnabled(bool value)
        {
            base.SetPanelEnabled(value);
            _viewCamera.gameObject.SetActive(value);
        }
    }
}

