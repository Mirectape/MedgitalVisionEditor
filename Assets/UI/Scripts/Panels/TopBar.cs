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
            IconButton undoBtn = new IconButton(() => Debug.Log("Undo"), "\uf060");
            undoRedoGroup.Add(undoBtn);
            IconButton redoBtn = new IconButton(() => Debug.Log("Redo"), "\uf061");
            undoRedoGroup.Add(redoBtn);
            undoRedoGroup.style.marginRight = 15;
            
            // Camera controls
            IconToggleGroup cameraControlGroup = new IconToggleGroup();
            controls.Add(cameraControlGroup);
            cameraControlGroup
                .AddToggle(
                    () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Cursor),
                    "\uf245", true)
                .AddToggle( 
                    () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Pan),
                    "\uf047")
                .AddToggle(
                    () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Rotate),
                    "\uf021")
                .AddToggle(
                    () => CameraTransformController.Instance.SetControlAction(ControlInputAction.Zoom),
                    "\uf002");

            Loader ase = new Loader();
            
            //
            VisualElement group = new VisualElement();
            group.AddToClassList(_elementsGroupStyle);
            AccentButton cb = new AccentButton(() => ase.Show(), "Кнопочка", "\uf007");
            IconButton cb1 = new IconButton(() => ase.Hide(),"\uf078");
            group.Add(cb);
            group.Add(cb1);
            controls.Add(group);
            
            ase.Show();
            controls.Add(ase);
            
            //Logo
            Image logo = new Image();
            logo.AddToClassList(_logoStyle);
            this.Add(logo);
        }
    }
}
