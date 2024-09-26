using FMSolution.FMNetwork;
using System;
using System.Globalization;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private static CameraController _instance;
    public static CameraController Instance => _instance;

    private CameraControlActions _cameraActions;
    private InputAction _movement;
    private Transform _cameraTransform;

    [SerializeField]
    private FMNetworkManager _fmManager;

    //Horizontal motion
    [SerializeField]
    private float _maxSpeed = 5f;
    private float _speed;
    [SerializeField]
    private float _acceleration = 10f;
    [SerializeField]
    private float _damping = 5f;

    //Vertical motion - zooming
    [SerializeField]
    private float _stepSize = 0.3f;
    [SerializeField]
    private float _zoomDampening = 7.5f;
    [SerializeField]
    private float _minDistance = 0.5f;
    [SerializeField]
    private float _maxDistance = 50f;

    //Rotation
    [SerializeField]
    private float _maxRotationSpeed = 0.3f;

    //value set in various functions 
    //used to update the position of the camera base object.
    private Vector3 _targetPosition;

    private float _zoomDistance;

    //used to track and maintain velocity w/o a rigidbody
    private Vector3 _verticalVelocity;
    private Vector3 _lastPosition;

    //tracks where the dragging action started
    private Vector3 _startDrag;
    private Vector3 _planeNormal;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);


        _cameraActions = new CameraControlActions();
        _cameraTransform = this.GetComponentInChildren<Camera>().transform;
        _planeNormal = Vector3.forward;
    }

    private void OnEnable()
    {
        _zoomDistance = _cameraTransform.localPosition.z;
        _cameraTransform.LookAt(this.transform);

        _lastPosition = this.transform.position;
        _movement = _cameraActions.Camera.Movement;
        _cameraActions.Camera.Enable();
        _cameraActions.Camera.Rotate.performed += RotateCamera;
        _cameraActions.Camera.Zoom.performed += ZoomCamera;
    }

    private void OnDisable()
    {
        _cameraActions.Camera.Rotate.performed -= RotateCamera;
        _cameraActions.Camera.Zoom.performed -= ZoomCamera;
        _cameraActions.Disable();
    }

    private void Update()
    {
        UpdatePanMovement();
        DragCamera();

        UpdateVelocity();
        UpdateCameraPosition();
        UpdateBasePosition();
    }

    private void UpdateVelocity()
    {
        _verticalVelocity = (this.transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = this.transform.position;

    }

    private void UpdatePanMovement()
    {
        Vector3 inputValue = _movement.ReadValue<Vector2>().x * GetCameraRight() +
            _movement.ReadValue<Vector2>().y * GetCameraUp();
        inputValue = inputValue.normalized;

        if (inputValue.sqrMagnitude > 0.1f)
        {
            _targetPosition += inputValue;
        }
    }

    private void UpdateBasePosition()
    {
        if (_targetPosition.sqrMagnitude > 0.1f)
        {
            _speed = Mathf.Lerp(_speed, _maxSpeed, Time.deltaTime * _acceleration);
            transform.position += _targetPosition * _speed * Time.deltaTime;
        }
        else
        {
            _verticalVelocity = Vector3.Lerp(_verticalVelocity, Vector3.zero, Time.deltaTime * _damping);
            transform.position += _verticalVelocity * Time.deltaTime;
        }

        _targetPosition = Vector3.zero;
    }

    private void RotateCamera(InputAction.CallbackContext inputValue)
    {
        float value_x = inputValue.ReadValue<Vector2>().x;
        float value_y = inputValue.ReadValue<Vector2>().y;
        transform.rotation = Quaternion.Euler(value_y * _maxRotationSpeed + transform.rotation.eulerAngles.x,
                                                value_x * _maxRotationSpeed + transform.rotation.eulerAngles.y,
                                                0f);
        _planeNormal = transform.rotation * Vector3.forward;
    }

    private void RotateCameraReceive(float value_x)
    {
        transform.rotation = Quaternion.Euler(0f, value_x * _maxRotationSpeed + transform.rotation.eulerAngles.y, 0f);
    }

    private void ZoomCamera(InputAction.CallbackContext inputValue)
    {
        float valueStrength = 0.01f;
        float value = -inputValue.ReadValue<Vector2>().y * valueStrength;
        if (Math.Abs(value) > 0.1f)
        {
            _zoomDistance += value * _stepSize;
            Debug.Log(_cameraTransform.localPosition.z);
            if (_zoomDistance < _minDistance)
            {
                _zoomDistance = _minDistance;
            }
            else if (_zoomDistance > _maxDistance)
            {
                _zoomDistance = _maxDistance;
            }
        }
    }

    private void ZoomCameraReceive(float value)
    {
        if (Math.Abs(value) > 0.1f)
        {
            _zoomDistance = _cameraTransform.localPosition.y + value * _stepSize;
            if (_zoomDistance < _minDistance)
            {
                _zoomDistance = _minDistance;
            }
            else if (_zoomDistance > _maxDistance)
            {
                _zoomDistance = _maxDistance;
            }
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 zoomTarget = new Vector3(_cameraTransform.localPosition.x,
                                        _cameraTransform.localPosition.y,
                                        _zoomDistance);
        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, zoomTarget, Time.deltaTime * _zoomDampening);
        _cameraTransform.LookAt(this.transform);
    }


    /// <summary>
    /// Use this func to pan around
    /// </summary>
    private void DragCamera()
    {
        if (!Mouse.current.middleButton.isPressed)
        {
            return;
        }
        Vector2 mouseValue = Mouse.current.position.ReadValue();
        Plane plane = new Plane(_planeNormal, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(mouseValue);

        if (plane.Raycast(ray, out float distance))
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                _startDrag = ray.GetPoint(distance);
            }
            else
            {
                _targetPosition += _startDrag - ray.GetPoint(distance);
            }
        }
    }

    private void DragCameraReceive(Vector2 mousePosition)
    {
        Debug.Log(mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (plane.Raycast(ray, out float distance))
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                _startDrag = ray.GetPoint(distance);
            }
            else
            {
                _targetPosition += _startDrag - ray.GetPoint(distance);
            }
        }
    }

    public void ReceiveCommonCommand(string command)
    {
        Debug.Log(command);
        if (command.Contains("r_"))
        {
            var valueStringArray = command.Replace("r_", "");
            float value = float.Parse(valueStringArray, CultureInfo.InvariantCulture);
            RotateCameraReceive(value);
        }
        else if (command.Contains("z_"))
        {
            var valueStringArray = command.Replace("z_", "");
            float value = float.Parse(valueStringArray, CultureInfo.InvariantCulture);
            ZoomCameraReceive(value);
        }
        else if (command.Contains("p_"))
        {
            var valueStringArray = command.Replace("p_", "").Split(";");
            Debug.Log($"{valueStringArray[0]} + {valueStringArray[1]}");
            float value_x = float.Parse(valueStringArray[0], CultureInfo.InvariantCulture);
            float value_y = float.Parse(valueStringArray[1], CultureInfo.InvariantCulture);
            Vector2 value = new Vector2(value_x, value_y);
            DragCameraReceive(value);
        }
    }

    private Vector3 GetCameraRight()
    {
        Vector3 right = _cameraTransform.right;
        right.y = 0f;
        return right;
    }

    private Vector3 GetCameraForward()
    {
        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;
        return forward;
    }

    private Vector3 GetCameraUp()
    {
        Vector3 up = _cameraTransform.up;
        up.z = 0f;
        return up;
    }
}
