using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class ConnectorSurface : MonoBehaviour
{
    public enum SurfaceFace { Top, Bottom, Left, Right, Front, Back }

    [Header("Face & Rotation")]
    public SurfaceFace face = SurfaceFace.Top;
    [Range(0, 3)] public int rotationSteps = 0;

    [Header("Grid")]
    public float cell = 0.24f;
    public float gap = 0.01f;
    public int width = 8;
    public int height = 4;
    public Transform origin;

    [Header("Offsets")]
    public float planeOffset = 0f;
    public Vector2 offsetCells = Vector2.zero;

    [Tooltip("Разрешённые клетки в локальных координатах сетки.")]
    public List<Vector2Int> enabledCells = new();

    [SerializeField, HideInInspector] private List<Vector2Int> _reservedList = new();
    private readonly HashSet<Vector2Int> _reserved = new();

#if UNITY_EDITOR
    [SerializeField] private Color reservedGizmoColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);
#endif


    public float Pitch => cell + gap;

    public bool IsEnabled(Vector2Int cc) => enabledCells != null && enabledCells.Contains(cc);

    public bool IsReserved(Vector2Int c) => _reserved.Contains(c);

    public void Reserve(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells) _reserved.Add(c);
#if UNITY_EDITOR
        Debug.Log($"[Surface {name}] Reserved: [{string.Join(", ", cells)}]");
#endif
    }

    public void Release(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells) _reserved.Remove(c);
#if UNITY_EDITOR
        Debug.Log($"[Surface {name}] Released: [{string.Join(", ", cells)}]");
#endif
    }

    public void ClearReserved()
    {
        _reserved?.Clear();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public bool CanPlace(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            if (!IsEnabled(c))
            {
#if UNITY_EDITOR
                Debug.Log($"[Surface {name}] cell {c} disabled");
#endif
                return false; 
            }
            if (IsReserved(c))
            {
#if UNITY_EDITOR
                Debug.Log($"[Surface {name}] cell {c} RESERVED");
#endif
                return false;
            }
        }
        return true;
    }






    public int OverlapCount(IEnumerable<Vector2Int> cells)
    {
        if (cells == null || enabledCells == null) return 0;
        int n = 0; foreach (var cc in cells) if (enabledCells.Contains(cc)) n++;
        return n;
    }

    // ---------- преобразования ----------
    public Vector3 CellToWorld(Vector2Int c)
    {
        var o = origin ? origin : transform;
        GetBasis(out Vector3 u, out Vector3 v, out Vector3 n);
        float px = (c.x + 0.5f + offsetCells.x) * Pitch;
        float pz = (c.y + 0.5f + offsetCells.y) * Pitch;
        var local = n * planeOffset + u * px + v * pz;
        return o.TransformPoint(local);
    }

    public bool WorldToCell(Vector3 world, out Vector2Int c)
    {
        var o = origin ? origin : transform;
        GetBasis(out Vector3 u, out Vector3 v, out Vector3 n);
        var local = o.InverseTransformPoint(world) - n * planeOffset;

        float ux = Vector3.Dot(local, u) - offsetCells.x * Pitch;
        float vy = Vector3.Dot(local, v) - offsetCells.y * Pitch;

        int cx = Mathf.RoundToInt(ux / Pitch - 0.5f);
        int cy = Mathf.RoundToInt(vy / Pitch - 0.5f);

        c = new Vector2Int(cx, cy);
        return cx >= 0 && cx < width && cy >= 0 && cy < height;
    }

    void GetBasis(out Vector3 u, out Vector3 v, out Vector3 n)
    {
        switch (face)
        {
            case SurfaceFace.Top: n = Vector3.up; u = Vector3.right; v = Vector3.forward; break;
            case SurfaceFace.Bottom: n = Vector3.down; u = Vector3.right; v = Vector3.back; break;
            case SurfaceFace.Left: n = Vector3.left; u = Vector3.forward; v = Vector3.up; break;
            case SurfaceFace.Right: n = Vector3.right; u = Vector3.back; v = Vector3.up; break;
            case SurfaceFace.Front: n = Vector3.forward; u = Vector3.right; v = Vector3.up; break;
            case SurfaceFace.Back: n = Vector3.back; u = Vector3.left; v = Vector3.up; break;
            default: n = Vector3.up; u = Vector3.right; v = Vector3.forward; break;
        }
        if (rotationSteps != 0)
        {
            var q = Quaternion.AngleAxis(90f * rotationSteps, n);
            u = q * u; v = q * v;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Connectors/Enable All Cells")]
    void EnableAllCells()
    {
        if (enabledCells == null) enabledCells = new List<Vector2Int>();
        enabledCells.Clear();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                enabledCells.Add(new Vector2Int(x, y));
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Connectors/Disable All Cells")]
    void DisableAllCells()
    {
        if (enabledCells == null) enabledCells = new List<Vector2Int>();
        enabledCells.Clear();
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Connectors/Clear Reserved")]
    void CtxClearReserved() => ClearReserved();

    void OnDrawGizmos()
    {
        var o = origin ? origin : transform;

        // локальный базис u,v,n (как у тебя)
        GetBasis(out var u, out var v, out var n);

        // матрица для рисования в системе (u,v,n) с учётом planeOffset
        var rot = Quaternion.LookRotation(v, n); // z->v, y->n
        Gizmos.matrix = o.localToWorldMatrix * Matrix4x4.TRS(n * planeOffset, rot, Vector3.one);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var center = new Vector3((x + 0.5f + offsetCells.x) * Pitch, 0f, (y + 0.5f + offsetCells.y) * Pitch);
                var size = new Vector3(cell, 0.001f, cell);

                var cellId = new Vector2Int(x, y);
                bool enabled = enabledCells.Contains(cellId);
                bool busy = _reserved.Contains(cellId);

                // приоритет: занято (серый) > доступно (зелёный) > выключено (белый прозрачный)
                Gizmos.color = busy
                    ? new Color(0.5f, 0.5f, 0.5f, 0.50f)   // серый — зарезервировано
                    : (enabled ? new Color(0f, 1f, 0f, 0.35f)  // зелёный — можно
                               : new Color(1f, 1f, 1f, 0.08f)); // светлый — выключено
                Gizmos.DrawCube(center, size);

                Gizmos.color = new Color(0f, 0f, 0f, 0.2f);
                Gizmos.DrawWireCube(center, size);
            }

        Gizmos.matrix = Matrix4x4.identity; // ← вот эту строку ты оборвал
    }
#endif
}
