using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Builder
{
    public class GridGhostPlacer : MonoBehaviour
    {
        [Header("World")]
        public Camera Cam;
        public Transform MountRoot;
        public LayerMask BuildPlaneMask;

        [Header("Ghost")]
        public Material OkMat, BadMat;

        [Header("Grid")]
        public float DefaultCellSize = 0.25f;
        public BuildGrid Grid;

        [Header("Physics overlap")]
        public LayerMask ObstacleMask;      // слои реальных модулей/рамы
        public float OverlapPadding = 0.01f;

        private ModuleConfig _mod;
        private GameObject _ghost;
        private Renderer[] _ghostR;
        private BoxCollider _ghostBox;
        private Quaternion _yaw = Quaternion.identity;

        private Inventory.IInventoryModel _capturedModel;
        private int _capturedFromIndex = -1;
        private Inventory.IInventoryItem _capturedItem;

        private bool _useExternalPointer;
        private Vector2 _externalPointer;

        public bool IsActive => _mod != null && _ghost != null;

        void Awake() { if (!Cam) Cam = Camera.main; }

        public void Begin(ModuleConfig m)
        {
            _mod = m;
            if (_mod == null || _mod.Prefab == null) return;

            if (_ghost) Destroy(_ghost);
            _ghost = Instantiate(_mod.Prefab);

            // выключаем все реальные коллайдеры у призрака
            foreach (var col in _ghost.GetComponentsInChildren<Collider>(includeInactive: true))
                col.enabled = false;

            // ставим материал "призрака"
            _ghostR = _ghost.GetComponentsInChildren<Renderer>(includeInactive: true);
            SetMat(BadMat);

            // *** создаЄм box-коллайдер дл€ призрака по SizeCells ***
            _ghostBox = _ghost.GetComponent<BoxCollider>();
            if (!_ghostBox) _ghostBox = _ghost.AddComponent<BoxCollider>();
            _ghostBox.isTrigger = true;           // призрак не должен толкать
            _ghostBox.enabled = true;             // но должен давать нам Bounds
            _ghostBox.center = Vector3.zero;

            float cell = _mod.CellSize > 0 ? _mod.CellSize : DefaultCellSize;
            var size = Vector3.Max(Vector3.one * cell, new Vector3(
                Mathf.Max(1, _mod.SizeCells.x) * cell,
                Mathf.Max(1, _mod.SizeCells.y) * cell,
                Mathf.Max(1, _mod.SizeCells.z) * cell));
            _ghostBox.size = size;

            // опционально Ч приподн€ть box так, чтобы "низ" совпадал с низом рендера
            // (если у префаба пивот в центре)
            var rbounds = GetWorldBounds(_ghostR, 0);
            var localBottom = _ghost.transform.InverseTransformPoint(rbounds.min);
            var localCenter = _ghost.transform.InverseTransformPoint(rbounds.center);
            // центр трохи сместим, чтобы низ совпадал с плоскостью
            _ghostBox.center = new Vector3(0, localCenter.y - localBottom.y - (_ghostBox.size.y * 0.5f), 0);

            _yaw = Quaternion.identity;

            Grid ??= new BuildGrid(cell, Vector3.zero);

            // кеш дл€ списани€ 1 предмета
            _capturedModel = Inventory.DragContext.Model;
            _capturedFromIndex = Inventory.DragContext.FromIndex;
            _capturedItem = Inventory.DragContext.Item;
        }

        public void End()
        {
            if (_ghost) Destroy(_ghost);
            _ghost = null; _ghostR = null;
            _mod = null;
            _capturedModel = null; _capturedItem = null; _capturedFromIndex = -1;
        }

        public void SetExternalPointer(Vector2 screenPos) { _useExternalPointer = true; _externalPointer = screenPos; }
        public void ReleaseExternalPointer() => _useExternalPointer = false;

        void Update()
        {
            if (!IsActive) return;

            // 1) куда смотрим
            if (!Physics.Raycast(Cam.ScreenPointToRay(GetPointer()), out var hit, 999f, BuildPlaneMask))
                return;

            // 2) грид, прив€зка по XZ, Y = уровень Origin
            float cell = _mod.CellSize > 0 ? _mod.CellSize : DefaultCellSize;
            Grid ??= new BuildGrid(cell, Vector3.zero);

            var p = hit.point; p.y = Grid.Origin.y;
            var cellPos = Grid.WorldToCell(p); cellPos.y = 0;
            var worldPos = Grid.CellToWorld(cellPos);

            // 3) позици€/поворот призрака
            _ghost.transform.position = worldPos;
            ApplyYawRotation();
            _ghost.transform.rotation = _yaw;

            // 4) поджать низ к поверхности (визуал)
            AlignBottomToSurface(hit.point.y);

            // 5) логика грида (зан€тие клеток и контакт)
            var cells = CollectCells(_mod.SizeCells, cellPos, _yaw);   // см. функцию ниже
            bool collGrid = Grid.WillCollide(cells);
            int contacts = Grid.ContactCount(cells);
            bool okByGrid = !collGrid && contacts >= Mathf.Max(0, _mod.MinContactCells);

            // 6) OverlapBox по Ђреальнымї модул€м (ObstacleMask), с учЄтом поворота
            //   берем center/halfExtents из ghostBox в WORLD:
            var worldCenter = _ghost.transform.TransformPoint(_ghostBox.center);
            var half = (_ghostBox.size * 0.5f) + Vector3.one * OverlapPadding;
            bool physOverlap = Physics.CheckBox(worldCenter, half, _yaw, ObstacleMask, QueryTriggerInteraction.Ignore);

            bool finalOk = okByGrid && !physOverlap;
            SetMat(finalOk ? OkMat : BadMat);

            // 7) коммит
            if (WasRelease() && finalOk)
            {
                var real = Instantiate(_mod.Prefab, _ghost.transform.position, _ghost.transform.rotation, MountRoot);
                foreach (var col in real.GetComponentsInChildren<Collider>(true)) col.enabled = true; // включаем реальные
                Grid.Occupy(cells);
                _capturedModel?.TryRemoveAt(_capturedFromIndex, 1);
                End();
            }
        }

        IEnumerable<Vector3Int> CollectCells(Vector3Int size, Vector3Int origin, Quaternion yaw)
        {
            size = new Vector3Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y), Mathf.Max(1, size.z));
            // только слой y=0 (строим на плоскости)
            int rotIdx = Mathf.RoundToInt(Quaternion.Angle(Quaternion.identity, yaw) / 90f) % 4;
            // пермутируем X/Z при повороте
            int sx = size.x, sz = size.z;
            if (rotIdx == 1 || rotIdx == 3) { sx = size.z; sz = size.x; }

            for (int x = 0; x < sx; x++)
                for (int z = 0; z < sz; z++)
                {
                    // центрируем вокруг origin, чтобы box сто€л Ђпо центру клеткиї
                    int offsX = x - (sx / 2);
                    int offsZ = z - (sz / 2);
                    yield return origin + new Vector3Int(offsX, 0, offsZ);
                }
        }

        void AlignBottomToSurface(float surfaceY)
        {
            var b = GetWorldBounds(_ghostR, 0f);
            if (b.size == Vector3.zero) return;
            float dy = surfaceY - b.min.y;
            var pos = _ghost.transform.position;
            pos.y += dy;
            _ghost.transform.position = pos;
        }

        static int LayerMaskToLayer(LayerMask lm)
        {
            int mask = lm.value;
            for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) return i;
            return -1;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            var stack = new System.Collections.Generic.Stack<Transform>();
            stack.Push(go.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                t.gameObject.layer = layer;
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
        }

        void ApplyYawRotation()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(-90, Vector3.up) * _yaw;
                if (Keyboard.current.eKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(+90, Vector3.up) * _yaw;
            }
        }

        static IEnumerable<Vector3Int> GetWorldCellsY(Vector3Int[] local, Vector3Int origin, Quaternion yaw)
        {
            if (local == null || local.Length == 0) yield break;

            int idx = Mathf.RoundToInt(Quaternion.Angle(Quaternion.identity, yaw) / 90f) % 4;

            for (int i = 0; i < local.Length; i++)
            {
                var c = local[i];
                Vector3Int r = c;
                switch (idx)
                {
                    case 1: r = new Vector3Int(c.z, c.y, -c.x); break;
                    case 2: r = new Vector3Int(-c.x, c.y, -c.z); break;
                    case 3: r = new Vector3Int(-c.z, c.y, c.x); break;
                }
                yield return origin + r;
            }
        }

        void SetMat(Material m)
        {
            if (_ghostR == null || m == null) return;
            foreach (var r in _ghostR) if (r) r.sharedMaterial = m;
        }

        Vector2 GetPointer()
        {
            if (_useExternalPointer) return _externalPointer;
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        bool WasRelease()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        }

        static Bounds GetWorldBounds(Renderer[] rends, float inflate = 0f)
        {
            if (rends == null || rends.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            if (inflate > 0f) b.Expand(inflate * 2f);
            return b;
        }
    }
}
