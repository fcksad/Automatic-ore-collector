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
        public float CellSize = 0.25f;

        [Header("Ghost")]
        public Material OkMat;
        public Material BadMat;

        [Header("Raycast")]
        public LayerMask RaycastMask; 

        [Header("Input")]
        public Key RotateKey = Key.R;

        [Header("Grid State")]
        public BuildGridState GridState;

        private ModuleConfig _mod;
        private GameObject _ghost;

        private Inventory.IInventoryModel _model;
        private int _fromIndex;
        public bool IsActive => _mod != null && _ghost != null;

        private List<Vector3Int> _currentCells = new();
        private bool _hasValidPlacement;
        private Vector3 _lastValidPos;
        private Quaternion _lastValidRot;

        private void Awake()
        {
            if (!Cam) Cam = Camera.main;

            if (!GridState)
                GridState = FindObjectOfType<BuildGridState>();

            if (GridState)
                CellSize = GridState.CellSize;
        }

        public void Begin(ModuleConfig mod)
        {
            _mod = mod;
            if (!_mod || !_mod.Prefab) return;

            if (_ghost) Destroy(_ghost);

            _ghost = Instantiate(_mod.Prefab, MountRoot);
            foreach (var c in _ghost.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            SetGhostMaterial(BadMat);

            _model = Inventory.DragContext.Model;
            _fromIndex = Inventory.DragContext.FromIndex;

            _currentCells.Clear();
            _hasValidPlacement = false;
        }

        public void End(bool commit = false)
        {
            if (commit && _hasValidPlacement && _mod != null)
            {
                var real = Instantiate(_mod.Prefab, _lastValidPos, _lastValidRot, MountRoot);
                foreach (var c in real.GetComponentsInChildren<Collider>(true))
                    c.enabled = true;

                if (GridState != null && _currentCells != null && _currentCells.Count > 0)
                {
                    GridState.Commit(_mod, real.transform, _currentCells);
                }

                if (_model != null && _fromIndex >= 0)
                {
                    _model.TryRemoveAt(_fromIndex);
                }

                Inventory.DragContext.Clear();
            }

            if (_ghost) Destroy(_ghost);
            _ghost = null;
            _mod = null;
            _model = null;
            _fromIndex = -1;
            _currentCells.Clear();
            _hasValidPlacement = false;
        }

        private void Update()
        {
            if (!IsActive) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            var screenPos = mouse.position.ReadValue();
            var ray = Cam.ScreenPointToRay(screenPos);

            if (!Physics.Raycast(ray, out var hit, 5000f, RaycastMask))
            {
                _ghost.SetActive(false);
                return;
            }

            _ghost.SetActive(true);

            if (Keyboard.current[RotateKey].wasPressedThisFrame)
            {
                RotateGhost90();
            }

            var targetSurface = hit.collider.GetComponentInParent<ConnectorSurface>();
            if (targetSurface != null)
            {
                AlignToConnector(targetSurface);
            }
            else if (GridState && GridState.Grid != null)
            {
                var cell = GridState.Grid.WorldToCell(hit.point);
                var worldPos = GridState.Grid.CellToWorld(cell);
                _ghost.transform.position = worldPos;
            }

            bool valid = true;
            _currentCells.Clear();

            if (GridState)
            {
                valid = GridState.CanPlace(_mod, _ghost.transform, out _currentCells);
            }

            SetGhostMaterial(valid ? OkMat : BadMat);

            _hasValidPlacement = valid;
            if (valid)
            {
                _lastValidPos = _ghost.transform.position;
                _lastValidRot = _ghost.transform.rotation;
            }
        }

        private void RotateGhost90()
        {
            if (!_ghost) return;

            switch (_mod.RotationMode)
            {
                case RotationMode.Any:
                case RotationMode.Snap90:
                    _ghost.transform.Rotate(Vector3.up, 90f, Space.Self);
                    break;
                case RotationMode.YawOnly:
                    _ghost.transform.Rotate(Vector3.up, 90f, Space.World);
                    break;
            }
        }

        private void AlignToConnector(ConnectorSurface target)
        {
            if (!_ghost) return;

            var mySurfaces = _ghost.GetComponentsInChildren<ConnectorSurface>();
            if (mySurfaces.Length == 0) return;

            ConnectorSurface my = null;
            foreach (var s in mySurfaces)
            {
                if (s.Type == target.Type)
                {
                    my = s;
                    break;
                }
            }
            if (my == null) my = mySurfaces[0];

            var ghostTr = _ghost.transform;

            var targetNormal = target.WorldNormal;
            var myNormal = my.WorldNormal;

            var rot = Quaternion.FromToRotation(myNormal, -targetNormal);
            ghostTr.rotation = rot * ghostTr.rotation;

            var delta = target.WorldPosition - my.WorldPosition;
            ghostTr.position += delta;
        }

        private void SetGhostMaterial(Material mat)
        {
            if (!_ghost || !mat) return;

            foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
            }
        }
    }
}
