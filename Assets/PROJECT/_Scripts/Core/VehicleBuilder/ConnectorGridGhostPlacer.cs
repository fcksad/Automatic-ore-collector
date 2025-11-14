using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Service;

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

        [Header("Grid State")]
        public BuildGridState GridState;

        private ModuleConfig _mod;
        private GameObject _ghost;

        private Inventory.IInventoryModel _model;
        private Inventory.IInventoryItem _item;
        private int _fromIndex;
        public bool IsActive => _mod != null && _ghost != null;

        private List<Vector3Int> _currentCells = new();
        public bool HasValidPlacement => _hasValidPlacement;
        private bool _hasValidPlacement;

        private Vector3 _lastValidPos;
        private Quaternion _lastValidRot;

        private IInputService _input;

        private bool _prevHForward;
        private bool _prevHBackward;
        private bool _prevVForward;
        private bool _prevVBackward;

        private ConnectorSurface _currentTargetSurface;   
        private ConnectorSurface _currentGhostSurface;    

        private void Awake()
        {
            if (!Cam) Cam = Camera.main;

            if (!GridState)
                GridState = FindObjectOfType<BuildGridState>();

            if (GridState)
                CellSize = GridState.CellSize;

            _input = ServiceLocator.Get<IInputService>();
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
            _item = Inventory.DragContext.Item;
            _fromIndex = Inventory.DragContext.FromIndex;

            _currentCells.Clear();
            _hasValidPlacement = false;

            _currentTargetSurface = null;
            _currentGhostSurface = null;
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

                var rt = real.AddComponent<BuildModuleRuntime>();
                rt.GridState = GridState;
                if (_currentCells != null && _currentCells.Count > 0)
                    rt.OccupiedCells.AddRange(_currentCells);

                var invItem = _item;
                if (invItem != null)
                {
                    rt.SourceConfig = invItem.Config;
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

            _currentTargetSurface = null;
            _currentGhostSurface = null;
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

            HandleRotationInput();

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

                _currentTargetSurface = null;
                _currentGhostSurface = null;
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

        private void HandleRotationInput()
        {
            if (_input == null || _mod == null) return;

            bool hF = _input.IsPressed(CharacterAction.RotateHorizontalForward);
            bool hB = _input.IsPressed(CharacterAction.RotateHorizontalBackwards);
            bool vF = _input.IsPressed(CharacterAction.RotateVerticalForward);
            bool vB = _input.IsPressed(CharacterAction.RotateVerticalBackwards);

            if (hF && !_prevHForward)
                RotateHorizontal(+1);

            if (hB && !_prevHBackward)
                RotateHorizontal(-1);

            if (vF && !_prevVForward)
                RotateVertical(+1);

            if (vB && !_prevVBackward)
                RotateVertical(-1);

            _prevHForward = hF;
            _prevHBackward = hB;
            _prevVForward = vF;
            _prevVBackward = vB;
        }

        private void RotateHorizontal(int dir)
        {
            if (!_ghost) return;

            if (_mod.RotationMode != RotationMode.Any &&
                _mod.RotationMode != RotationMode.HorizontalOnly)
                return;

            if (_currentTargetSurface == null || _currentGhostSurface == null)
            {
                _ghost.transform.Rotate(Vector3.up, 90f * dir, Space.World);
                return;
            }

            Vector3 pivot = _currentTargetSurface.WorldPosition;
            Vector3 normal = _currentTargetSurface.WorldNormal;

            _ghost.transform.RotateAround(pivot, normal, 90f * dir);

            SnapGhostToCurrentPair();
        }

        private void RotateVertical(int dir)
        {
            if (!_ghost) return;

            if (_mod.RotationMode != RotationMode.Any &&
                _mod.RotationMode != RotationMode.VerticalOnly)
                return;

            if (_currentTargetSurface == null || _currentGhostSurface == null)
            {
                _ghost.transform.Rotate(Vector3.right, 90f * dir, Space.World);
                return;
            }

            Vector3 pivot = _currentTargetSurface.WorldPosition;
            Vector3 normal = _currentTargetSurface.WorldNormal;

            Vector3 axis = Vector3.Cross(normal, Vector3.up);
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.Cross(normal, Vector3.forward);
            axis.Normalize();

            _ghost.transform.RotateAround(pivot, axis, 90f * dir);

            SnapGhostToCurrentPair();
        }


        private FaceDirection GetOpposite(FaceDirection d)
        {
            switch (d)
            {
                case FaceDirection.Up: return FaceDirection.Down;
                case FaceDirection.Down: return FaceDirection.Up;
                case FaceDirection.Left: return FaceDirection.Right;
                case FaceDirection.Right: return FaceDirection.Left;
                case FaceDirection.Forward: return FaceDirection.Back;
                case FaceDirection.Back: return FaceDirection.Forward;
            }
            return d;
        }

        private void AlignToConnector(ConnectorSurface target)
        {
            if (!_ghost || target == null) return;

            bool targetChanged = target != _currentTargetSurface;
            _currentTargetSurface = target;

            if (_currentGhostSurface == null || targetChanged)
            {
                FaceDirection desiredDir = GetOpposite(target.Direction);

                _currentGhostSurface = FindGhostSurfaceOfTypeAndDir(target.Type, desiredDir);
                if (_currentGhostSurface == null)
                {
                    _currentGhostSurface = FindGhostSurfaceOfType(target.Type);
                    if (_currentGhostSurface == null) return;
                }
            }

            SnapGhostToCurrentPair();
        }

        private ConnectorSurface FindGhostSurfaceOfType(ConnectorType type)
        {
            var all = _ghost.GetComponentsInChildren<ConnectorSurface>();
            foreach (var s in all)
                if (s.Type == type)
                    return s;
            return null;
        }

        private ConnectorSurface FindGhostSurfaceOfTypeAndDir(ConnectorType type, FaceDirection dir)
        {
            var all = _ghost.GetComponentsInChildren<ConnectorSurface>();
            foreach (var s in all)
                if (s.Type == type && s.Direction == dir)
                    return s;
            return null;
        }



        private void SnapGhostToCurrentPair()
        {
            if (_currentGhostSurface == null || _currentTargetSurface == null || _ghost == null)
                return;

            var ghostTr = _ghost.transform;

            var myPos = _currentGhostSurface.WorldPosition;
            var myNormal = _currentGhostSurface.WorldNormal;

            var targetPos = _currentTargetSurface.WorldPosition;
            var targetNormal = _currentTargetSurface.WorldNormal;

            var rot = Quaternion.FromToRotation(myNormal, -targetNormal);
            ghostTr.rotation = rot * ghostTr.rotation;

            var delta = targetPos - myPos;
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
