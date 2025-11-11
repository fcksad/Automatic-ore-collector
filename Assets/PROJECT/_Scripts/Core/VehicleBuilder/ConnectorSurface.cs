using UnityEngine;

public enum ConnectorType
{
    Structure,
    Movement,
    Weapon,
    Decor
}

public enum FaceDirection
{
    Up,
    Down,
    Left,
    Right,
    Forward,
    Back
}

[ExecuteAlways]
public class ConnectorSurface : MonoBehaviour
{
    [Header("Тип коннектора")]
    public ConnectorType Type = ConnectorType.Structure;

    [Header("Направление грани")]
    public FaceDirection Direction = FaceDirection.Up;

    [Header("Слой")]
    public LayerMask _connectorLayer;

    [Header("Авто-настройка")]
    [Tooltip("Автоматически ставить коннектор на грань BoxCollider родителя.")]
    public bool AutoAlignToParent = true;

    [Tooltip("Автоматически создавать триггер-коллайдер для Raycast.")]
    public bool AutoCollider = true;

    [Header("Параметры")]
    public float FaceThickness = 0.01f;

    [Header("Логические штуки")]
    public int GroupId = 0;

    [SerializeField, HideInInspector]
    private BoxCollider _faceCollider;

    private readonly Vector3[] DirectionVectors =
    {
        Vector3.up,     
        Vector3.down,   
        Vector3.left,   
        Vector3.right,  
        Vector3.forward,
        Vector3.back    
    };


    public Vector3 WorldPosition => transform.position;
    public Vector3 WorldNormal => transform.TransformDirection(DirectionVectors[(int)Direction]);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var pos = WorldPosition;
        var normal = WorldNormal;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(pos, 0.02f);
        Gizmos.DrawLine(pos, pos + normal * 0.3f);

        UnityEditor.Handles.Label(pos + normal * 0.32f, $"{Type} ({Direction})");
    }

    public void OnValidate()
    {
        if (name != Direction.ToString())
        {
            name = Direction.ToString();
        }

        if (gameObject.layer != _connectorLayer)
        {
            gameObject.layer = _connectorLayer;
        }

        if (AutoAlignToParent)
            AlignToParentCollider();

        if (AutoCollider)
            UpdateFaceCollider();
    }

    private BoxCollider GetParentCollider()
    {
        var parent = transform.parent;
        if (!parent) return null;

        var cols = parent.GetComponents<BoxCollider>();
        foreach (var c in cols)
        {
            if (!c.isTrigger)
                return c; 
        }

        return null;
    }

    [ContextMenu("AlignToParentCollider")]
    private void AlignToParentCollider()
    {
        var baseCol = GetParentCollider();
        if (!baseCol) return;

        var half = baseCol.size * 0.5f;
        var dir = DirectionVectors[(int)Direction];

        Vector3 localPos = baseCol.center;

        if (dir.x > 0.5f) localPos += new Vector3(+half.x, 0, 0);
        else if (dir.x < -0.5f) localPos += new Vector3(-half.x, 0, 0);
        else if (dir.y > 0.5f) localPos += new Vector3(0, +half.y, 0);
        else if (dir.y < -0.5f) localPos += new Vector3(0, -half.y, 0);
        else if (dir.z > 0.5f) localPos += new Vector3(0, 0, +half.z);
        else localPos += new Vector3(0, 0, -half.z);

        transform.localPosition = localPos;
        transform.localRotation = Quaternion.identity;
    }

    private void UpdateFaceCollider()
    {
        var baseCol = GetParentCollider();
        if (!baseCol) return;

        if (_faceCollider == null)
        {
            var all = GetComponents<BoxCollider>();
            foreach (var c in all)
            {
                if (c.isTrigger)
                {
                    _faceCollider = c;
                    break;
                }
            }

            if (_faceCollider == null)
                _faceCollider = gameObject.AddComponent<BoxCollider>();
        }

        _faceCollider.isTrigger = true;

        var size = baseCol.size;
        var dir = DirectionVectors[(int)Direction];

        Vector3 center = Vector3.zero;
        Vector3 faceSize = size;

        if (dir == Vector3.up || dir == Vector3.down)
        {
            center = new Vector3(0, dir.y * -FaceThickness * 0.5f, 0);
            faceSize = new Vector3(size.x, FaceThickness, size.z);
        }
        else if (dir == Vector3.left || dir == Vector3.right)
        {
            center = new Vector3(dir.x * -FaceThickness * 0.5f, 0, 0);
            faceSize = new Vector3(FaceThickness, size.y, size.z);
        }
        else // forward/back
        {
            center = new Vector3(0, 0, dir.z * -FaceThickness * 0.5f);
            faceSize = new Vector3(size.x, size.y, FaceThickness);
        }

        _faceCollider.center = center;
        _faceCollider.size = faceSize;
    }

#endif
}