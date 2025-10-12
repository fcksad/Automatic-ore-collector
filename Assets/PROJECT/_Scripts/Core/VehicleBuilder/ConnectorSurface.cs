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

    public float Pitch => cell + gap;

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

    public bool IsEnabled(Vector2Int cc) => enabledCells != null && enabledCells.Contains(cc);

    public int OverlapCount(IEnumerable<Vector2Int> cells)
    {
        if (cells == null || enabledCells == null) return 0;
        int n = 0; foreach (var cc in cells) if (enabledCells.Contains(cc)) n++;
        return n;
    }

    // --- basis u/v/n in local space ---
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
        Undo.RecordObject(this, "Enable All Cells");
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Connectors/Disable All Cells")]
    void DisableAllCells()
    {
        if (enabledCells == null) enabledCells = new List<Vector2Int>();
        enabledCells.Clear();
        Undo.RecordObject(this, "Disable All Cells");
        EditorUtility.SetDirty(this);
    }

    void OnDrawGizmos()
    {
        var o = origin ? origin : transform;
        GetBasis(out var u, out var v, out var n);
        var rot = Quaternion.LookRotation(v, n); // z->v, y->n
        Gizmos.matrix = o.localToWorldMatrix * Matrix4x4.TRS(n * planeOffset, rot, Vector3.one);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + 0.5f + offsetCells.x) * Pitch;
                float pz = (y + 0.5f + offsetCells.y) * Pitch;

                var center = new Vector3(px, 0f, pz);
                var size = new Vector3(cell, 0.001f, cell);

                bool on = enabledCells.Contains(new Vector2Int(x, y));
                Gizmos.color = on ? new Color(0, 1, 0, 0.35f) : new Color(1, 1, 1, 0.08f);
                Gizmos.DrawCube(center, size);

                Gizmos.color = new Color(0, 0, 0, 0.2f);
                Gizmos.DrawWireCube(center, size);
            }
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
