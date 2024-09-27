using FMSolution.FMNetwork;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    //maps for inner control
    private CameraControlActions _cameraActions;
    private InputAction _movement;
    private InputAction _rotation;
    private InputAction _zoom;
    private InputAction _drag;

    //for signals from outside control
    [SerializeField] private FMNetworkManager _fmManager;

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
        ActionEncoderTransformation();
    }

    public void ActionEncoderTransformation()
    {
        if (_fmManager.NetworkType == FMNetworkType.Client)
        {
            return;
        }

        //convert _movementVector2.x, .y, _rotationVector2.x, .y, _zoomVector2.x, .y [total: 6] into byte[]
        int amountOfFloatsFromSignal = 6;
        int floatWeightInBytes = 4;

        byte[] sendBytes = new byte[amountOfFloatsFromSignal * floatWeightInBytes];
        int offset = 0;

        byte[] byte_movement_x = BitConverter.GetBytes(_movement.ReadValue<Vector2>().x);
        byte[] byte_movement_y = BitConverter.GetBytes(_movement.ReadValue<Vector2>().y);
        byte[] byte_rotation_x = BitConverter.GetBytes(_rotation.ReadValue<Vector2>().x);
        byte[] byte_rotation_y = BitConverter.GetBytes(_rotation.ReadValue<Vector2>().y);
        byte[] byte_zoom_x = BitConverter.GetBytes(_zoom.ReadValue<Vector2>().x);
        byte[] byte_zoom_y = BitConverter.GetBytes(_zoom.ReadValue<Vector2>().y);

        //copy each byte[] to SendBytes
        Buffer.BlockCopy(byte_movement_x, 0, sendBytes, offset, 4); offset += 4;
        Buffer.BlockCopy(byte_movement_y, 0, sendBytes, offset, 4); offset += 4;
        Buffer.BlockCopy(byte_rotation_x, 0, sendBytes, offset, 4); offset += 4;
        Buffer.BlockCopy(byte_rotation_y, 0, sendBytes, offset, 4); offset += 4;
        Buffer.BlockCopy(byte_zoom_x, 0, sendBytes, offset, 4); offset += 4;
        Buffer.BlockCopy(byte_zoom_y, 0, sendBytes, offset, 4); offset += 4;

        //send the bytes[]
        if (_fmManager.NetworkType == FMNetworkType.Server)
        {
            _fmManager.SendToOthers(sendBytes);
        }
        
    }

    public void ActionDecodeTransformation(byte[] receivedBytes)
    {
        // make sure id doesn't override server pos
        if (_fmManager.NetworkType == FMNetworkType.Server)
        {
            return;
        }

        //decode received data for each object
        int offset = 0;

        float movement_x = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
        float movement_y = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
        float rotation_x = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
        float rotation_y = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
        float zoom_x = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
        float zoom_y = BitConverter.ToSingle(receivedBytes, offset); offset += 4;

        Vector2 movement = new Vector2(movement_x, movement_y);
        Vector2 rotation = new Vector2(rotation_x, rotation_y);
        Vector2 zoom = new Vector2(zoom_x, zoom_y);

        CameraBrain.Instance.PanCamera(movement);
        CameraBrain.Instance.RotateCamera(rotation);
        CameraBrain.Instance.ZoomCamera(zoom);
    }
}
