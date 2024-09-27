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

    private void Awake()
    {
        _cameraActions = new CameraControlActions();
    }

    private void OnEnable()
    {
        _movement = _cameraActions.Camera.Movement;
        _rotation = _cameraActions.Camera.Rotate;
        _zoom = _cameraActions.Camera.Zoom;
        _cameraActions.Camera.Enable();
    }

    private void OnDisable()
    {
        _cameraActions.Disable();
    }

    private void Update()
    {
    }
}
