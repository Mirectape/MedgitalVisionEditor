using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FMNetworkCameraController : MonoBehaviour
{

    private Transform _cameraTransform;

    //value set in various functions 
    //used to update the position of the camera base object.
    private Vector3 _targetPosition;

    //used to track and maintain velocity w/o a rigidbody
    private Vector3 _verticalVelocity;
    private Vector3 _lastPosition;

    private void Awake()
    {
        _cameraTransform = this.GetComponentInChildren<Camera>().transform;
    }

    private void OnEnable()
    {
        _cameraTransform.LookAt(this.transform);
    }

    private void UpdatePanMovement(Vector3 inputValue)
    {
        inputValue = inputValue.normalized;

        if (inputValue.sqrMagnitude > 0.1f)
        {
            _targetPosition += inputValue;
        }
    }

    private void UpdateVelocity()
    {
        _verticalVelocity = (this.transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = this.transform.position;

    }
}
