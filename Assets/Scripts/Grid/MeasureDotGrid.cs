using MeasureGrid;
using UnityEngine;

public class MeasureDotGrid : MonoBehaviour
{
    #region Delegate and events
    
    public delegate void MeterToPixelHandler(float measures);
    public event MeterToPixelHandler OnMeterInPixel;

    #endregion
    
    #region Private fields
    
    private Camera _targetCamera;
    
    // Settings
    private int _layerCulling = 12;
    private float _graduationScale = 1;
    private float _scale = 1;
    private bool _isVisible = true;
    private Color _lineColor = Color.white;
    
    // Grid
    private MaterialPropertyBlock _materialPropertyBlock;
    private static Mesh _meshGrid;
    private Material _material;
    
    private float _meterInPixel = 0;

    #endregion

    #region Properties
    
    public int LayerCulling { get => _layerCulling; set => _layerCulling = value; }
    public bool IsVisible { get => _isVisible; set => _isVisible = value; }
    public Color LineColor { get => _lineColor; set => _lineColor = value; }
    public float MeterInPixel { get => _meterInPixel; set => _meterInPixel = value; }
    
    #endregion
    
    private void Awake()
    {
        _targetCamera = GetComponent<Camera>();
        
        if (_meshGrid == null)
        {
            _meshGrid = QuadMesh.CreateQuadXZ(1.0f, 1.0f, Color.white);
            _meshGrid.MarkDynamic();
        }
        
        _material = new Material(Shader.Find("Ogxd/Grid"));
        _material.SetFloat("_GraduationScale", _graduationScale);
        _materialPropertyBlock = new MaterialPropertyBlock();
    }
    
    private void OnRenderObject()
    {
        if (_targetCamera == null) return;
        if (!IsVisible) return;
        if (!_targetCamera.orthographic) return;

        Quaternion rotationAngles =
            Quaternion.LookRotation(_targetCamera.transform.up, -_targetCamera.transform.forward);

        float orthoHeight = _targetCamera.orthographicSize * 2.0f;
        float orthoWidth = orthoHeight * _targetCamera.aspect;
        
        Transform camTransform = _targetCamera.transform;
        Vector3 nearMidPt = camTransform.position + camTransform.forward * _targetCamera.farClipPlane / 2;
        Vector3 nearTopLeft = nearMidPt - camTransform.right * orthoWidth * 0.5f + camTransform.up * orthoHeight * 0.5f;
        Vector3 nearTopRight = nearTopLeft + camTransform.right * orthoWidth;
        Vector3 nearBottomRight = nearTopRight - camTransform.up * orthoHeight;
        Vector3 nearBottomLeft = nearBottomRight - camTransform.right * orthoWidth;
        
        float width = (nearTopRight - nearTopLeft).magnitude;
        float height = (nearTopLeft - nearBottomLeft).magnitude;
        float maxSize = Mathf.Max(width, height);
        Vector3 gridPlaneScale = new Vector3(maxSize, 1, maxSize);
        
        Matrix4x4 transformMatrix = Matrix4x4.TRS(nearMidPt, rotationAngles, gridPlaneScale);
        
        Vector3 cameraPosition = _targetCamera.transform.position;
        float distanceX = Vector3.Dot(cameraPosition, _targetCamera.transform.right.normalized);
        float distanceY = Vector3.Dot(cameraPosition, _targetCamera.transform.up.normalized);

        _material.SetColor("_Color", _lineColor);
        _material.SetColor("_SecondaryColor", _lineColor);
        _material.SetVector("_GridOffset", new Vector4(distanceX / maxSize, distanceY / maxSize, 0 , 0));
        _material.SetFloat("_Scale", _scale / maxSize);
        
        // Render
        Graphics.DrawMesh(_meshGrid, transformMatrix, _material, _layerCulling, _targetCamera, 0,
            _materialPropertyBlock);
        
        var p1 = _targetCamera.WorldToScreenPoint(nearTopLeft);
        var p2 = _targetCamera.WorldToScreenPoint(nearTopLeft + camTransform.right);

        _meterInPixel = Mathf.Abs(p2.x - p1.x);
        OnMeterInPixel?.Invoke(_meterInPixel);
    }
}