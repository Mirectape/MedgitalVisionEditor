using System.Collections;
using System.Collections.Generic;
using UI;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class TopBar : VisualElement
    {
        #region Style fields
    
        private static readonly string _logoStyle = "logo";
        private static readonly string _controlsGroupStyle = "topbar-controls-group";
        private static readonly string _elementsGroupStyle = "elements-group"; 

        #endregion
        
        public TopBar()
        {
            GroupLayout controls = new GroupLayout(FlexDirection.Row);
            controls.AddToClassList(_controlsGroupStyle);
            this.Add(controls);
            
            // Undo/Redo
            VisualElement undoRedoGroup = new VisualElement();
            undoRedoGroup.AddToClassList(_elementsGroupStyle);
            controls.Add(undoRedoGroup);
            IconButton undoBtn = new IconButton("\uf060", () => Debug.Log("Undo"));
            undoRedoGroup.Add(undoBtn);
            IconButton redoBtn = new IconButton("\uf061", () => Debug.Log("Redo"));
            undoRedoGroup.Add(redoBtn);
            undoRedoGroup.style.marginRight = 15;
            
            // Camera controls
            IconToggleGroup cameraControlGroup = new IconToggleGroup();
            controls.Add(cameraControlGroup);
            cameraControlGroup
                .AddToggle("\uf245", () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Cursor), true)
                .AddToggle("\uf047", () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Pan))
                .AddToggle("\uf021", () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Rotate))
                .AddToggle("\uf002", () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Zoom));

            //
            VisualElement group = new VisualElement();
            controls.Add(group);
            
            //Logo
            Image logo = new Image();
            logo.AddToClassList(_logoStyle);
            this.Add(logo);
        }
    }
}
