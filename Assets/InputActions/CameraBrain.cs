using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraBrain : MonoBehaviour
{
    private static CameraBrain _instance;
    public static CameraBrain Instance {  get { return _instance; } }

    public Camera camera;
    private Transform _cameraTransform;

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

        _cameraTransform = camera.transform;
        _planeNormal = Vector3.forward;
    }

    private void OnEnable()
    {
        _zoomDistance = _cameraTransform.localPosition.z;
        _cameraTransform.LookAt(this.transform);
        _lastPosition = this.transform.position;
    }

    /// <summary>
    /// Use in fixed updates with other methods to make those methods active
    /// </summary>
    /// <param name="inputValue"></param>
    public void UseMovetracking()
    {
        UpdateVelocity();
        UpdateCameraPosition();
        UpdateBasePosition();
    }

    /// <summary>
    /// Use alnogside with UseMovetracking in fixed updates if necessary. 
    /// Use this function to pun around with buttons or joystick
    /// </summary>
    /// <param name="inputValue"></param>
    public void PanCamera(Vector2 inputValue)
    {
        Vector3 movementValue = inputValue.x * GetCameraRight() +
            inputValue.y * GetCameraUp();

        movementValue = movementValue.normalized;

        if (inputValue.sqrMagnitude > 0.1f)
        {
            _targetPosition += movementValue;
        }
    }

    /// <summary>
    /// Use alnogside with UseMovetracking in fixed updates if necessary. 
    /// Use this func to pan around with mouse or alternative 
    /// </summary>
    public void DragCamera(InputAction movement)
    {
        if (!movement.IsPressed())
        {
            return;
        }

        Vector2 mouseValue = movement.ReadValue<Vector2>();
        Plane plane = new Plane(_planeNormal, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(mouseValue);

        if (plane.Raycast(ray, out float distance))
        {
            if (movement.WasPressedThisFrame())
            {
                _startDrag = ray.GetPoint(distance);
            }
            else
            {
                _targetPosition += _startDrag - ray.GetPoint(distance);
            }
        }
    }

    /// <summary>
    /// Use alnogside with UseMovetracking in fixed updates if necessary. 
    /// </summary>
    /// <param name="inputValue"></param>
    public void RotateCamera(Vector2 inputValue)
    {
        float value_x = inputValue.x;
        float value_y = inputValue.y;
        transform.rotation = Quaternion.Euler(value_y * _maxRotationSpeed + transform.rotation.eulerAngles.x,
                                                value_x * _maxRotationSpeed + transform.rotation.eulerAngles.y,
                                                0f);
        _planeNormal = transform.rotation * Vector3.forward;
    }

    /// <summary>
    /// Use alnogside with UseMovetracking in updates if necessary. 
    /// </summary>
    /// <param name="inputValue"></param>
    public void ZoomCamera(Vector2 inputValue)
    {
        float valueStrength = 0.01f;
        float value = -inputValue.y * valueStrength;
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

    private void UpdateVelocity()
    {
        _verticalVelocity = (this.transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = this.transform.position;
    }

    private void UpdateCameraPosition()
    {
        Vector3 zoomTarget = new Vector3(_cameraTransform.localPosition.x,
                                        _cameraTransform.localPosition.y,
                                        _zoomDistance);
        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, zoomTarget, Time.deltaTime * _zoomDampening);
        _cameraTransform.LookAt(this.transform);
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
