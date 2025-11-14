#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Builder;

[ExecuteAlways]
public class ConnectorGridGenerator : MonoBehaviour
{
    [Header("Config")]
    public ModuleConfig Module;          
    public BoxCollider SourceCollider;   

    [Header("Connectors")]
    public ConnectorType DefaultType = ConnectorType.Structure;
    public float FaceThickness = 0.01f;
    [Header("Connectors")]
    public int ConnectorLayer = 12;

    [Header("Which faces")]
    public bool GenerateUp = true;
    public bool GenerateDown = true;
    public bool GenerateLeft = true;
    public bool GenerateRight = true;
    public bool GenerateForward = true;
    public bool GenerateBack = true;

#if UNITY_EDITOR
    [ContextMenu("Generate connectors from GridSize")]
    private void GenerateFromGrid()
    {
        if (Module == null)
        {
            Debug.LogError("[ConnectorGridGenerator] ModuleConfig is null", this);
            return;
        }

        if (SourceCollider == null)
            SourceCollider = GetComponent<BoxCollider>();

        if (SourceCollider == null)
        {
            Debug.LogError("[ConnectorGridGenerator] No BoxCollider on object", this);
            return;
        }

        float cell = Module.CellSize;
        if (cell <= 0f)
        {
            Debug.LogError("[ConnectorGridGenerator] Module.CellSize must be > 0", this);
            return;
        }

        var existing = GetComponentsInChildren<ConnectorSurface>(true);
        foreach (var cs in existing)
        {
            if (cs == null) continue;
            Undo.DestroyObjectImmediate(cs.gameObject);
        }

        if (GenerateUp) GenerateFace(FaceDirection.Up, cell);
        if (GenerateDown) GenerateFace(FaceDirection.Down, cell);
        if (GenerateLeft) GenerateFace(FaceDirection.Left, cell);
        if (GenerateRight) GenerateFace(FaceDirection.Right, cell);
        if (GenerateForward) GenerateFace(FaceDirection.Forward, cell);
        if (GenerateBack) GenerateFace(FaceDirection.Back, cell);

        EditorUtility.SetDirty(gameObject);
        Debug.Log("[ConnectorGridGenerator] Connectors generated", this);
    }

    private void GenerateFace(FaceDirection dir, float cell)
    {
        Vector3Int grid = Module.GridSize;

        var col = SourceCollider;
        Vector3 size = col.size;
        Vector3 center = col.center;

        int countA = 1;
        int countB = 1;

        Vector3 axisA = Vector3.right;
        Vector3 axisB = Vector3.forward;
        Vector3 normal = Vector3.up;

        switch (dir)
        {
            case FaceDirection.Up:
            case FaceDirection.Down:
                normal = (dir == FaceDirection.Up) ? Vector3.up : Vector3.down;
                axisA = Vector3.right;   
                axisB = Vector3.forward; 
                countA = Mathf.Max(1, grid.x);
                countB = Mathf.Max(1, grid.z);
                break;

            case FaceDirection.Left:
            case FaceDirection.Right:
                normal = (dir == FaceDirection.Right) ? Vector3.right : Vector3.left;
                axisA = Vector3.up;     
                axisB = Vector3.forward;
                countA = Mathf.Max(1, grid.y);
                countB = Mathf.Max(1, grid.z);
                break;

            case FaceDirection.Forward:
            case FaceDirection.Back:
                normal = (dir == FaceDirection.Forward) ? Vector3.forward : Vector3.back;
                axisA = Vector3.right;   
                axisB = Vector3.up;     
                countA = Mathf.Max(1, grid.x);
                countB = Mathf.Max(1, grid.y);
                break;
        }

        float startA = -(countA - 1) * cell * 0.5f;
        float startB = -(countB - 1) * cell * 0.5f;

        Vector3 faceOffset = Vector3.zero;
        if (dir == FaceDirection.Up) faceOffset = Vector3.up * (size.y * 0.5f);
        else if (dir == FaceDirection.Down) faceOffset = Vector3.down * (size.y * 0.5f);
        else if (dir == FaceDirection.Right) faceOffset = Vector3.right * (size.x * 0.5f);
        else if (dir == FaceDirection.Left) faceOffset = Vector3.left * (size.x * 0.5f);
        else if (dir == FaceDirection.Forward) faceOffset = Vector3.forward * (size.z * 0.5f);
        else if (dir == FaceDirection.Back) faceOffset = Vector3.back * (size.z * 0.5f);

        for (int ia = 0; ia < countA; ia++)
        {
            for (int ib = 0; ib < countB; ib++)
            {
                float offsA = startA + ia * cell;
                float offsB = startB + ib * cell;

                Vector3 localPos =
                    center +
                    faceOffset +
                    axisA * offsA +
                    axisB * offsB;

                string coordSuffix;
                switch (dir)
                {
                    case FaceDirection.Up:
                    case FaceDirection.Down:
                        coordSuffix = $"x{ia}_z{ib}";
                        break;

                    case FaceDirection.Left:
                    case FaceDirection.Right:
                        coordSuffix = $"y{ia}_z{ib}";
                        break;

                    case FaceDirection.Forward:
                    case FaceDirection.Back:
                    default:
                        coordSuffix = $"x{ia}_y{ib}";
                        break;
                }

                string name = $"{dir}_{coordSuffix}";

                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create connector");

                go.transform.SetParent(transform, false);
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.layer = ConnectorLayer;

                var cs = go.AddComponent<ConnectorSurface>();
                cs.Type = DefaultType;
                cs.Direction = dir;
                cs.FaceThickness = FaceThickness;

                var trigger = go.AddComponent<BoxCollider>();
                trigger.isTrigger = true;

                Vector3 colSize;
                if (dir == FaceDirection.Up || dir == FaceDirection.Down)
                    colSize = new Vector3(cell, FaceThickness, cell);
                else if (dir == FaceDirection.Left || dir == FaceDirection.Right)
                    colSize = new Vector3(FaceThickness, cell, cell);
                else
                    colSize = new Vector3(cell, cell, FaceThickness);

                trigger.size = colSize;
                trigger.center = normal * (-FaceThickness * 0.5f);
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (Module == null) return;

        if (SourceCollider == null)
            SourceCollider = GetComponent<BoxCollider>();
        if (SourceCollider == null) return;

        float cell = Module.CellSize;
        if (cell <= 0f) return;

        var mask = Module.Occupancy != null ? Module.Occupancy.LocalCells : null;
        if (mask == null || mask.Length == 0) return;

        // ћаксимальный размер сетки по конфигу Ц нужен только чтобы
        // правильно вычислить "ноль" (левый-нижний-задний угол)
        Vector3Int grid = Module.GridSize;
        if (grid.x <= 0 || grid.y <= 0 || grid.z <= 0) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        // Ћокальный центр "€чейки (0,0,0)" Ч как и раньше в генераторе
        Vector3 origin =
            SourceCollider.center -
            new Vector3(
                (grid.x - 1) * cell * 0.5f,
                (grid.y - 1) * cell * 0.5f,
                (grid.z - 1) * cell * 0.5f
            );

        Gizmos.color = Color.red;

        foreach (var c in mask)
        {
            // предполагаем, что LocalCells заданы в тех же координатах,
            // что и GridSize: (0..grid.x-1, 0..grid.y-1, 0..grid.z-1)
            Vector3 localCenter = origin + new Vector3(c.x * cell, c.y * cell, c.z * cell);
            Gizmos.DrawWireCube(localCenter, Vector3.one * cell);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
