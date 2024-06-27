using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestingInputSys : MonoBehaviour
{
    private UserInputActions _userInputActions;

    [SerializeField] private Camera _camera;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoomDistance = 1f;
    [SerializeField] private float _maxZoomDistance = 20f;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private float _targetZoomDistance;

    private void Awake()
    {
        _userInputActions = new UserInputActions();
        _userInputActions.User.Enable();
        _userInputActions.User.TestAction.performed += TestAction;
    }

    private void Update()
    {
        // Handle camera movement
        HandleMovement();

        // Handle camera rotation
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector2 dir = _userInputActions.User.Movement.ReadValue<Vector2>();
        _targetPosition = _camera.transform.localPosition + (_camera.transform.right * dir.x * _moveSpeed) +
            (_camera.transform.forward * dir.y * _moveSpeed);
        _camera.transform.localPosition = Vector3.Lerp(_camera.transform.position, _targetPosition, Time.deltaTime * _moveSpeed);
    }

    void HandleRotation()
    {
        Vector2 rot = _userInputActions.User.Rotation.ReadValue<Vector2>();
        _targetRotation = _camera.transform.localRotation * Quaternion.Euler(-rot.y * _rotationSpeed, +rot.x * _rotationSpeed, 0f);
        _camera.transform.localRotation = Quaternion.Lerp(_camera.transform.localRotation, _targetRotation, Time.deltaTime * _rotationSpeed);
    }

    private void TestAction(InputAction.CallbackContext callback)
    {
        Debug.Log(callback.action);
    }
}
