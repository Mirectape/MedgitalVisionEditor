using UnityEngine;
using UnityEngine.Events;

namespace FMSolution
{
    [System.Serializable] public class UnityEventFloat : UnityEvent<float> { }
    [System.Serializable] public class UnityEventFloatArray : UnityEvent<float[]> { }

    [System.Serializable] public class UnityEventInt : UnityEvent<int> { }
    [System.Serializable] public class UnityEventIntArray : UnityEvent<int[]> { }

    [System.Serializable] public class UnityEventVector2 : UnityEvent<Vector2> { }
    [System.Serializable] public class UnityEventVector2Array : UnityEvent<Vector2[]> { }

    [System.Serializable] public class UnityEventVector3 : UnityEvent<Vector3> { }
    [System.Serializable] public class UnityEventVector3Array : UnityEvent<Vector3[]> { }

    [System.Serializable] public class UnityEventQuaternion : UnityEvent<Quaternion> { }
    [System.Serializable] public class UnityEventQuaternionArray : UnityEvent<Quaternion[]> { }

    [System.Serializable] public class UnityEventBool : UnityEvent<bool> { }
    [System.Serializable] public class UnityEventBoolArray : UnityEvent<bool[]> { }

    [System.Serializable] public class UnityEventString : UnityEvent<string> { }
    [System.Serializable] public class UnityEventStringArray : UnityEvent<string[]> { }

    [System.Serializable] public class UnityEventTransform : UnityEvent<Transform> { }
    [System.Serializable] public class UnityEventTransformArray : UnityEvent<Transform[]> { }

    [System.Serializable] public class UnityEventColor : UnityEvent<Color> { }
    [System.Serializable] public class UnityEventColorArray : UnityEvent<Color[]> { }

    [System.Serializable] public class UnityEventGameObject : UnityEvent<GameObject> { }
    [System.Serializable] public class UnityEventGameObjectArray : UnityEvent<GameObject[]> { }

    [System.Serializable] public class UnityEventKeyCode : UnityEvent<KeyCode> { }
    [System.Serializable] public class UnityEventKeyCodeArray : UnityEvent<KeyCode[]> { }

    [System.Serializable] public class UnityEventByte : UnityEvent<byte> { }
    [System.Serializable] public class UnityEventByteArray : UnityEvent<byte[]> { }

    [System.Serializable] public class UnityEventObject : UnityEvent<object> { }
    [System.Serializable] public class UnityEventObjectArray : UnityEvent<object[]> { }

    [System.Serializable] public class UnityEventTexture : UnityEvent<Texture> { }
    [System.Serializable] public class UnityEventTextureArray : UnityEvent<Texture[]> { }

    [System.Serializable] public class UnityEventTexture2D : UnityEvent<Texture2D> { }
    [System.Serializable] public class UnityEventTexture2DArray : UnityEvent<Texture2DArray[]> { }

    [System.Serializable] public class UnityEventRenderTexture : UnityEvent<RenderTexture> { }
    [System.Serializable] public class UnityEventRenderTextureArray : UnityEvent<RenderTexture[]> { }

#if !FM_WEBCAM_DISABLED
    [System.Serializable] public class UnityEventWebcamTexture : UnityEvent<WebCamTexture> { }
    [System.Serializable] public class UnityEventWebcamTextureArray : UnityEvent<WebCamTexture[]> { }
#endif

    [System.Serializable] public class UnityEventInputTouch : UnityEvent<Touch[]> { }
    [System.Serializable] public class UnityEventRect : UnityEvent<Rect> { }

    public class UnityEventClass : MonoBehaviour { }
}