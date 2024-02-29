using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ControlInputAction { Cursor = 0, Pan = 1, Rotate = 2, Zoom = 3 }

public class CameraTransformController : MonoBehaviour
{
    public static CameraTransformController Instance { get; private set; }
    
    public InputActionAsset mapActions;
    
    private Camera _controlledCamera;
    private Transform _cameraOrbitCenter;
    
    private InputAction _panAction;
    private InputAction _zoomAction;
    private InputAction _rotateAction;
    private InputAction _generalAction;
    
    private ControlInputAction _currentControlAction = ControlInputAction.Cursor;
    
    private void Awake()
    {
        Instance = this;
        
        if (mapActions == null) Debug.LogError("MapAction is null");

        _panAction = mapActions.FindAction("Camera/Pan");
        _zoomAction = mapActions.FindAction("Camera/Zoom");
        _rotateAction = mapActions.FindAction("Camera/Orbit");
        _generalAction = mapActions.FindAction("Camera/General");
        
        if (_panAction == null || _zoomAction == null || _rotateAction == null || _generalAction == null) 
            Debug.LogError("Actions is null");

        SetControlAction(ControlInputAction.Cursor);
        
        ViewportPanel.OnCameraFocusChange += OnCameraFocusChange;
    }
    
    private void OnEnable()
    {
        if (_controlledCamera == null) return;

        _panAction.started += OnPanStarted;
        _panAction.performed += OnPanPerformed;
        _zoomAction.performed += OnZoomPerformed;
        _rotateAction.performed += OnRotatePerformed;
    
        _panAction.Enable();
        _zoomAction.Enable();
        
        _rotateAction.Enable();
        _generalAction.Enable();
    }
    
    private void OnDisable()
    {
        _panAction.started -= OnPanStarted;
        _panAction.performed -= OnPanPerformed;
        _zoomAction.performed -= OnZoomPerformed;
        _rotateAction.performed -= OnRotatePerformed;

        _panAction.Disable();
        _zoomAction.Disable();
        _rotateAction.Disable();
        _generalAction.Disable();
    }

    public void SetControlAction(ControlInputAction action)
    {
        if (_currentControlAction == action) return;
        _currentControlAction = action;
        
        switch (action)
        {
            case ControlInputAction.Cursor:
                _generalAction.started -= OnPanStarted;
                _generalAction.performed -= OnPanPerformed;
                _generalAction.performed -= OnRotatePerformed;
                _generalAction.performed -= OnGeneralZoomPerformed;
                _generalAction.Disable();
                break;
            case ControlInputAction.Pan:
                _generalAction.started += OnPanStarted;
                _generalAction.performed += OnPanPerformed;
                _generalAction.performed -= OnRotatePerformed;
                _generalAction.performed -= OnGeneralZoomPerformed;
                _generalAction.Enable();
                break;
            case ControlInputAction.Rotate:
                _generalAction.started -= OnPanStarted;
                _generalAction.performed -= OnPanPerformed;
                _generalAction.performed += OnRotatePerformed;
                _generalAction.performed -= OnGeneralZoomPerformed;
                _generalAction.Enable();
                break;
            case ControlInputAction.Zoom:
                _generalAction.started -= OnPanStarted;
                _generalAction.performed -= OnPanPerformed;
                _generalAction.performed -= OnRotatePerformed;
                _generalAction.performed += OnGeneralZoomPerformed;
                _generalAction.Enable();
                break;
        }
    }
    
    private void OnCameraFocusChange(Camera camera)
    {
        _controlledCamera = camera;

        if (camera == null)
        {
            OnDisable();
        }
        else
        {
            _cameraOrbitCenter = camera.transform.Find("OrbitCenter");
            if (_cameraOrbitCenter == null) Debug.LogError("Viewport Camera has not orbit center");
            
            OnEnable();
        }
    }

    
    #region Pan logic

    private Vector2 _startScreenToWorldPoint;
    
    private void OnPanStarted(InputAction.CallbackContext context)
    {
        if (context.control.displayName != "Position" && context.control.displayName != "Delta")
        {
            _startScreenToWorldPoint = context.ReadValue<Vector2>();
        }
    }

    private void OnPanPerformed(InputAction.CallbackContext context)
    {
        if (context.control.displayName == "Position")
        {
            Vector2 panInput = context.ReadValue<Vector2>();
        
            Vector3 worldToScreenPoint = _controlledCamera.WorldToScreenPoint(_controlledCamera.transform.forward);
            Vector3 newScreenPoint = worldToScreenPoint - (Vector3)(_startScreenToWorldPoint - panInput);
    
            _controlledCamera.transform.position -= 
                _controlledCamera.ScreenToWorldPoint(newScreenPoint) - _controlledCamera.transform.forward;

            _startScreenToWorldPoint = panInput;
        }
    }

    #endregion

    
    #region Zoom logic

    private float _zoomSpeed = 0.2f; // Скорость масштабирования
    private float _minZoom = 0.01f, _maxZoom = 10f; // Минимальное и максимальное значение масштабирования

    private void OnZoomPerformed(InputAction.CallbackContext context)
        => ZoomCamera(context.ReadValue<float>());

    private void OnGeneralZoomPerformed(InputAction.CallbackContext context)
    {
        if(context.control.displayName == "Delta") 
            ZoomCamera(context.ReadValue<Vector2>().y);
    }

    private void ZoomCamera(float value)
    {
        if (Keyboard.current.shiftKey.isPressed) return;

        if (_controlledCamera.orthographic)
        {
            float orthographicSize = _controlledCamera.orthographicSize;
            float newSize = orthographicSize - (value * _zoomSpeed * Time.deltaTime * orthographicSize);
            
            _controlledCamera.orthographicSize = Mathf.Clamp(newSize, _minZoom, _maxZoom);
        }
        else // Для перспективной камеры
        {
            // Расчет нового расстояния от камеры до _cameraOrbitCenter
            float distance = Vector3.Distance(_controlledCamera.transform.position, _cameraOrbitCenter.position);
            distance = Mathf.Clamp(distance - (value * _zoomSpeed * Time.deltaTime * distance), _minZoom, _maxZoom);

            Vector3 savePosition = _cameraOrbitCenter.position;
            // Позиционирование камеры
            _controlledCamera.transform.position = _cameraOrbitCenter.position - (_controlledCamera.transform.forward * distance);
            _cameraOrbitCenter.position = savePosition;
        }
    }

    #endregion

    
    #region Orbit logic

    private float _rotateSpeed = 0.1f;
    private float _minVerticalAngle = -89f, _maxVerticalAngle = 89f;
    
    private void OnRotatePerformed(InputAction.CallbackContext context)
    {
        if (!_controlledCamera.orthographic && context.control.displayName == "Delta")
        {
            Vector2 rotateInput = context.ReadValue<Vector2>();

            // Вычисляем углы вращения
            float angleY = rotateInput.x * _rotateSpeed; // Вращение вокруг оси Y
            float angleX = -rotateInput.y * _rotateSpeed; // Вращение вокруг горизонтальной оси

            // Вращение вокруг оси Y
            _controlledCamera.transform.RotateAround(_cameraOrbitCenter.position, Vector3.up, angleY);

            // Получаем текущий вертикальный угол
            float currentVerticalAngle = _controlledCamera.transform.localEulerAngles.x;
            // Преобразование угла в диапазон [-180, 180]
            currentVerticalAngle = (currentVerticalAngle > 180) ? currentVerticalAngle - 360 : currentVerticalAngle;
            // Ограничение вертикального угла и вращение вокруг горизонтальной оси
            float newVerticalAngle = 
                Mathf.Clamp(currentVerticalAngle + angleX, _minVerticalAngle, _maxVerticalAngle);
            
            _controlledCamera.transform.RotateAround(
                _cameraOrbitCenter.position, 
                _controlledCamera.transform.right, 
                newVerticalAngle - currentVerticalAngle);
        }
    }

    #endregion
    

    private void OnDestroy() {
        ViewportPanel.OnCameraFocusChange -= OnCameraFocusChange;
        // Дополнительно очистить ресурсы или подписки, если необходимо
    }
}
