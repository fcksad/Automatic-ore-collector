using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Builder
{
    public class ConnectorGridGhostPlacer : MonoBehaviour
    {
        [Header("World")]
        public Camera Cam;
        public Transform MountRoot;
        public LayerMask ObstacleMask;       
        public LayerMask GhostLayer;

        [Header("Ghost")]
        public Material OkMat;
        public Material BadMat;

        [Header("Raycast")]
        public LayerMask RaycastMask;

        [Header("Fallback (no ConnectorSurface)")]
        public float FallbackCell = 0.25f;       
        public bool AlignToHitObjectAxes = true; 

        [Header("Overlap")]
        public float OverlapPadding = 0.01f;

        private ModuleConfig _mod;
        private GameObject _ghost;
        private Renderer[] _rends;
        private BoxCollider _ghostBox;
        private Quaternion _yaw = Quaternion.identity;
        private ConnectorSurface _surface; 

        private Inventory.IInventoryModel _model;
        private int _fromIndex;

        private bool _useExternalPointer;
        private Vector2 _externalPointer;

        public bool IsActive => _mod != null && _ghost != null;

        public void Begin(ModuleConfig mod)
        {
            _mod = mod;
            if (!_mod || !_mod.Prefab) return;

            if (_ghost) Destroy(_ghost);
            _ghost = Instantiate(_mod.Prefab);
            foreach (var c in _ghost.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            SetLayerRecursively(_ghost, LayerMaskToLayer(GhostLayer));

            foreach (var c in _ghost.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            _rends = _ghost.GetComponentsInChildren<Renderer>(true);
            SetMat(BadMat);
            _yaw = Quaternion.identity;

            _ghostBox = _ghost.GetComponent<BoxCollider>();
            if (!_ghostBox) _ghostBox = _ghost.AddComponent<BoxCollider>();
            _ghostBox.isTrigger = true; _ghostBox.enabled = true;
            _ghostBox.center = Vector3.zero;
            _ghostBox.size = (_mod.BoundsSize.sqrMagnitude > 1e-6f) ? _mod.BoundsSize : GuessBounds(_rends);

            _model = Inventory.DragContext.Model;
            _fromIndex = Inventory.DragContext.FromIndex;
        }

        public void End()
        {
            if (_ghost) Destroy(_ghost);
            _ghost = null; _rends = null; _ghostBox = null;
            _mod = null; _surface = null;
            _model = null; _fromIndex = -1;
        }

        public void SetExternalPointer(Vector2 screenPos) { _useExternalPointer = true; _externalPointer = screenPos; }
        public void ReleaseExternalPointer() { _useExternalPointer = false; }

        void Update()
        {
            var item = Inventory.DragContext.Item;
            if (_mod == null && item?.Config?.Module) Begin(item.Config.Module);
            if (_mod == null) { if (_ghost) End(); return; }

            var ray = Cam.ScreenPointToRay(GetPointer());

            var hits = Physics.RaycastAll(ray, 9999f, RaycastMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // 1) ищем ближайший хит с ConnectorSurface
            RaycastHit? csHit = null;
            ConnectorSurface s = null;
            foreach (var h in hits)
            {
                var cand = h.collider.GetComponentInParent<ConnectorSurface>();
                if (cand != null) { csHit = h; s = cand; break; }
            }

            ApplyYawByMode(_mod);

            if (csHit.HasValue)
            {
                var h = csHit.Value;
                _surface = s;
                if (!_surface.WorldToCell(h.point, out var snappedCell)) { SetMat(BadMat); return; }

                var cellPos = _surface.CellToWorld(snappedCell);
                var n = _surface.transform.up;
                var rot = _yaw;

                const float skin = 0.001f;
                float rise = HalfExtentAlong(n, rot, _ghostBox.size);
                var pos = cellPos + n * (rise + skin);
                _ghost.transform.SetPositionAndRotation(pos, rot);

                // footprint + проверка занятости/резерва
                var fp = GetFootprintOrDefault(_mod);
                var cells2D = new List<Vector2Int>(TransformFootprint(fp, snappedCell, _yaw));

                bool okGrid = _surface.CanPlace(cells2D); // и enabled, и не reserved
                                                          // физика: игнорируем опорный коллайдер
                var center = _ghost.transform.TransformPoint(_ghostBox.center);
                var half = _ghostBox.size * 0.5f + Vector3.one * OverlapPadding;
                var rotQ = rot;

                var buf = new Collider[16];
                int cnt = Physics.OverlapBoxNonAlloc(center, half, buf, rotQ, ObstacleMask, QueryTriggerInteraction.Collide);
                bool phys = false;
                for (int i = 0; i < cnt; i++)
                    if (buf[i] && buf[i] != h.collider && buf[i].transform != _ghost.transform) { phys = true; break; }

                bool final = okGrid && !phys;
                SetMat(final ? OkMat : BadMat);

                if (Mouse.current?.leftButton.wasReleasedThisFrame == true && final)
                {
                    var real = Instantiate(_mod.Prefab, _ghost.transform.position, _ghost.transform.rotation, MountRoot);
                    foreach (var c in real.GetComponentsInChildren<Collider>(true)) c.enabled = true;

                    _surface.Reserve(cells2D); // ← тут зарезервируются клетки и «посереют»
                    var occ = real.AddComponent<ConnectorOccupant>();
                    occ.Init(_surface, cells2D);

                    _model?.TryRemoveAt(_fromIndex, 1);
                    End();
                }
            }
            else
            {
                // не нашли ConnectorSurface → fallback (пол/стена)
                var h = hits.Length > 0 ? hits[0] : default;
                var n = h.normal.normalized;
                var (u, v) = BuildPlaneBasis(h.collider ? h.collider.transform : null, n, AlignToHitObjectAxes);
                var planeOrigin = h.collider ? ProjectPointOnPlane(h.collider.transform.position, h.point, n) : h.point;
                var snapped = SnapPointOnPlane(h.point, planeOrigin, u, v, FallbackCell);

                var baseRot = Quaternion.LookRotation(u, n);
                var yawAround = Quaternion.AngleAxis(_yaw.eulerAngles.y, n);
                var rot2 = yawAround * baseRot;

                const float skin2 = 0.001f;
                float rise2 = HalfExtentAlong(n, rot2, _ghostBox.size);
                var pos2 = snapped + n * (rise2 + skin2);
                _ghost.transform.SetPositionAndRotation(pos2, rot2);

                // физика для fallback
                var center2 = _ghost.transform.TransformPoint(_ghostBox.center);
                var half2 = _ghostBox.size * 0.5f + Vector3.one * OverlapPadding;
                var buf2 = new Collider[16];
                int cnt2 = Physics.OverlapBoxNonAlloc(center2, half2, buf2, rot2, ObstacleMask, QueryTriggerInteraction.Collide);
                bool phys2 = false;
                for (int i = 0; i < cnt2; i++)
                    if (buf2[i] && buf2[i].transform != _ghost.transform) { phys2 = true; break; }

                SetMat(!phys2 ? OkMat : BadMat);

                if (Mouse.current?.leftButton.wasReleasedThisFrame == true && !phys2)
                {
                    var real = Instantiate(_mod.Prefab, _ghost.transform.position, _ghost.transform.rotation, MountRoot);
                    foreach (var c in real.GetComponentsInChildren<Collider>(true)) c.enabled = true;
                    _model?.TryRemoveAt(_fromIndex, 1);
                    End();
                }
            }
        }

            static float HalfExtentAlong(Vector3 n, Quaternion rot, Vector3 size)
        {
            // локальные полуоси (в world)
            Vector3 rx = rot * Vector3.right;
            Vector3 ry = rot * Vector3.up;
            Vector3 rz = rot * Vector3.forward;

            float hx = Mathf.Abs(Vector3.Dot(n, rx)) * (size.x * 0.5f);
            float hy = Mathf.Abs(Vector3.Dot(n, ry)) * (size.y * 0.5f);
            float hz = Mathf.Abs(Vector3.Dot(n, rz)) * (size.z * 0.5f);

            return hx + hy + hz;
        }

        static Vector2Int[] GetFootprintOrDefault(ModuleConfig m)
        {
            return (m.Footprint2D != null && m.Footprint2D.Length > 0)
                ? m.Footprint2D
                : new[] { Vector2Int.zero }; // 1×1
        }

        static IEnumerable<Vector2Int> TransformFootprint(Vector2Int[] src, Vector2Int origin, Quaternion yaw)
        {
            if (src == null || src.Length == 0) yield break;
            int i = Mathf.RoundToInt(Quaternion.Angle(Quaternion.identity, yaw) / 90f) % 4;
            foreach (var c in src)
            {
                Vector2Int r = c;
                switch (i)
                {
                    case 1: r = new Vector2Int(+c.y, -c.x); break;
                    case 2: r = new Vector2Int(-c.x, -c.y); break;
                    case 3: r = new Vector2Int(-c.y, +c.x); break;
                }
                yield return origin + r;
            }
        }

        static Vector3 ProjectPointOnPlane(Vector3 point, Vector3 anyPointOnPlane, Vector3 planeNormal)
        {
            var toPoint = point - anyPointOnPlane;
            var dist = Vector3.Dot(toPoint, planeNormal);
            return point - planeNormal * dist;
        }

        static (Vector3 u, Vector3 v) BuildPlaneBasis(Transform target, Vector3 n, bool alignToObjectAxes)
        {
            Vector3 u, v;
            if (alignToObjectAxes && target != null)
            {
                u = Vector3.ProjectOnPlane(target.right, n).normalized;
                if (u.sqrMagnitude < 1e-6f) u = Vector3.ProjectOnPlane(target.forward, n).normalized;
                v = Vector3.Cross(n, u).normalized;
            }
            else
            {
                var any = (Mathf.Abs(Vector3.Dot(n, Vector3.up)) < 0.9f) ? Vector3.up : Vector3.right;
                u = Vector3.Cross(any, n).normalized;
                v = Vector3.Cross(n, u).normalized;
            }
            return (u, v);
        }

        static bool RayToSurfacePlane(ConnectorSurface s, Ray ray, out Vector3 worldPoint)
        {
            s.GetType(); // just to ensure not-null compile
            var n = s.transform.up;
            var planePoint = s.transform.position + n * s.planeOffset;
            var plane = new Plane(n, planePoint);
            if (plane.Raycast(ray, out float dist)) { worldPoint = ray.GetPoint(dist); return true; }
            worldPoint = default; return false;
        }

        Vector2Int SnapWorldToCell(ConnectorSurface s, Vector3 world)
        {
            var o = s.origin ? s.origin : s.transform;
            s.GetType();
            // восстановим u/v/n для корректного расчёта WorldToCell
            s.WorldToCell(world, out var cell); // используем его API
            return cell;
        }

        static Vector3 SnapPointOnPlane(Vector3 p, Vector3 origin, Vector3 u, Vector3 v, float cell)
        {
            var d = p - origin;
            float du = Mathf.Round(Vector3.Dot(d, u) / cell) * cell;
            float dv = Mathf.Round(Vector3.Dot(d, v) / cell) * cell;
            return origin + u * du + v * dv;
        }

        static Vector3 GuessBounds(Renderer[] rends)
        {
            if (rends == null || rends.Length == 0) return new Vector3(0.5f, 0.5f, 0.5f);
            var b = rends[0].bounds; for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.size;
        }

        void SetMat(Material m) { if (_rends == null || m == null) return; foreach (var r in _rends) if (r) r.sharedMaterial = m; }

        void ApplyYawByMode(ModuleConfig m)
        {
            if (Keyboard.current == null) return;
            switch (m.RotationMode)
            {
                case RotationMode.Snap90:
                    if (Keyboard.current.qKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(-90, Vector3.up) * _yaw;
                    if (Keyboard.current.eKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(+90, Vector3.up) * _yaw;
                    _yaw = Quaternion.Euler(0f, _yaw.eulerAngles.y, 0f);
                    break;

                case RotationMode.YawOnly:
                    if (Keyboard.current.qKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(-15f, Vector3.up) * _yaw;
                    if (Keyboard.current.eKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(+15f, Vector3.up) * _yaw;
                    _yaw = Quaternion.Euler(0f, _yaw.eulerAngles.y, 0f);
                    break;

                case RotationMode.Any:
                    if (Keyboard.current.qKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(-15f, Vector3.up) * _yaw;
                    if (Keyboard.current.eKey.wasPressedThisFrame) _yaw = Quaternion.AngleAxis(+15f, Vector3.up) * _yaw;
                    break;
            }
        }

        private Vector2 GetPointer()
        {
            if (_useExternalPointer) return _externalPointer;
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        static int LayerMaskToLayer(LayerMask lm)
        {
            int mask = lm.value; for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) return i;
            return -1;
        }
        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return;
            var stack = new Stack<Transform>(); stack.Push(go.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop(); t.gameObject.layer = layer;
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
        }
    }
}
