using FMSolution.FMNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    //maps
    private CameraControlActions _cameraActions;
    private InputAction _movement;
    private InputAction _rotation;
    private InputAction _zoom;
    private InputAction _drag;

    private bool _cameraIsDragged; // in need to avoid overlapping between PanCamera and DragCamera functions

    private void Awake()
    {
        _cameraActions = new CameraControlActions();
    }

    private void OnEnable()
    {
        _movement = _cameraActions.Camera.Movement;
        _rotation = _cameraActions.Camera.Rotate;
        _zoom = _cameraActions.Camera.Zoom;
        _drag = _cameraActions.Camera.Drag;
        _cameraActions.Camera.Enable();
    }

    private void OnDisable()
    {
        _cameraActions.Disable();
    }

    private void Update()
    {
        CameraBrain.Instance.PanCamera(_movement.ReadValue<Vector2>());
        CameraBrain.Instance.DragCamera(_drag);
        CameraBrain.Instance.RotateCamera(_rotation.ReadValue<Vector2>());
        CameraBrain.Instance.ZoomCamera(_zoom.ReadValue<Vector2>());
        CameraBrain.Instance.UseMovetracking();
    }


}
