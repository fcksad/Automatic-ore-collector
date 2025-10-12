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
        public LayerMask SurfaceMask;        // слои объектов, в которых МОЖЕТ быть ConnectorSurface
        public LayerMask ObstacleMask;       // слои уже размещённых модулей/рамы
        public LayerMask GhostLayer;

        [Header("Ghost")]
        public Material OkMat, BadMat;

        [Header("Raycast")]
        public LayerMask RaycastMask;

        [Header("Fallback (no ConnectorSurface)")]
        public float FallbackCell = 0.25f;       // шаг снэпа на обычных поверхностях
        public bool AlignToHitObjectAxes = true; // базис u/v брать из transform цели

        [Header("Overlap")]
        public float OverlapPadding = 0.01f;

        // runtime
        private ModuleConfig _mod;
        private GameObject _ghost;
        private Renderer[] _rends;
        private BoxCollider _ghostBox;
        private Quaternion _yaw = Quaternion.identity;
        private ConnectorSurface _surface; // последняя поверхность с сеткой (для fallback)

        // auto-consume from inventory
        private Inventory.IInventoryModel _model;
        private int _fromIndex;

        // external pointer (из WorldPlacementBridge)
        private bool _useExternalPointer;
        private Vector2 _externalPointer;

        public bool IsActive => _mod != null && _ghost != null;

        void Awake() { if (!Cam) Cam = Camera.main; }

        // ===== API =====
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
            // активация от DragContext
            var item = Inventory.DragContext.Item;
            if (_mod == null && item?.Config?.Module) Begin(item.Config.Module);
            if (_mod == null) { if (_ghost) End(); return; }

            var ray = Cam.ScreenPointToRay(GetPointer());

            // есть хит о ЛЮБОЙ коллайдер
            if (Physics.Raycast(ray, out var hit, 9999f, RaycastMask, QueryTriggerInteraction.Ignore))
            {
                var s = hit.collider.GetComponentInParent<ConnectorSurface>();
                ApplyYawByMode(_mod);


                // ConnectorSurface режим
                if (s != null)
                {
                    // режим коннекторов
                    _surface = s;
                    if (!_surface.WorldToCell(hit.point, out var snappedCell)) { SetMat(BadMat); return; }
                    var pos = _surface.CellToWorld(snappedCell) + Vector3.up * _mod.MountHeight;
                    _ghost.transform.SetPositionAndRotation(pos, _yaw);
                    ValidateAndMaybeCommit(snappedCell, true);
                    return;
                }
                else
                {
                    // fallback-плоскость (пол или любая грань без ConnectorSurface)  // (оставь твою логику SnapPointOnPlane — она ок)
                }



                // Обычная поверхность (магнит к плоскости с шагом FallbackCell)
                var n = hit.normal.normalized;
                var (u, v) = BuildPlaneBasis(hit.collider.transform, n, AlignToHitObjectAxes);
                var snapped = SnapPointOnPlane(hit.point, hit.point, u, v, FallbackCell);
                var pos2 = snapped + n * _mod.MountHeight;

                var baseRot = Quaternion.LookRotation(u, n);
                var yawAround = Quaternion.AngleAxis(_yaw.eulerAngles.y, n);
                var rot2 = yawAround * baseRot;

                _ghost.transform.SetPositionAndRotation(pos2, rot2);

                var center = _ghost.transform.TransformPoint(_ghostBox.center);
                var half = _ghostBox.size * 0.5f + Vector3.one * OverlapPadding;
                bool phys = Physics.CheckBox(center, half, rot2, ObstacleMask, QueryTriggerInteraction.Ignore);
                SetMat(!phys ? OkMat : BadMat);

                if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && !phys)
                {
                    var real = Instantiate(_mod.Prefab, _ghost.transform.position, _ghost.transform.rotation, MountRoot);
                    foreach (var c in real.GetComponentsInChildren<Collider>(true)) c.enabled = true;
                    _model?.TryRemoveAt(_fromIndex, 1);
                    End();
                }
                return;
            }

            // никуда не попали — если есть запомнённая _surface, проектируем на её плоскость
            if (_surface == null) { SetMat(BadMat); return; }
            if (!RayToSurfacePlane(_surface, ray, out var planePoint)) { SetMat(BadMat); return; }

            ApplyYawByMode(_mod);
            var fallbackCell = SnapWorldToCell(_surface, planePoint);
            var fallbackInside = fallbackCell.x >= 0 && fallbackCell.x < _surface.width &&
                                 fallbackCell.y >= 0 && fallbackCell.y < _surface.height;

            var posFallback = _surface.CellToWorld(fallbackCell) + Vector3.up * _mod.MountHeight;
            _ghost.transform.SetPositionAndRotation(posFallback, _yaw);
            ValidateAndMaybeCommit(fallbackCell, fallbackInside);
        }

        // ===== validation & commit for ConnectorSurface =====
        void ValidateAndMaybeCommit(Vector2Int cell, bool cellInside)
        {
            var cells2D = TransformFootprint(_mod.Footprint2D, cell, _yaw);
            int overlap = cellInside ? _surface.OverlapCount(cells2D) : 0;
            bool okByConnectors = overlap >= Mathf.Max(1, _mod.RequiredOverlap);

            var center = _ghost.transform.TransformPoint(_ghostBox.center);
            var half = _ghostBox.size * 0.5f + Vector3.one * OverlapPadding;
            bool phys = Physics.CheckBox(center, half, _yaw, ObstacleMask, QueryTriggerInteraction.Ignore);

            bool final = okByConnectors && !phys;
            SetMat(final ? OkMat : BadMat);

            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && final)
            {
                var real = Instantiate(_mod.Prefab, _ghost.transform.position, _ghost.transform.rotation, MountRoot);
                foreach (var c in real.GetComponentsInChildren<Collider>(true)) c.enabled = true;

                _model?.TryRemoveAt(_fromIndex, 1);
                End();
            }
        }

        // ===== helpers =====
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
