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
#endif
}