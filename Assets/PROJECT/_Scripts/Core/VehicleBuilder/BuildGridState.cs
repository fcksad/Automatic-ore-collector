using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class BuildGridState : MonoBehaviour
    {
        [Header("Grid")]
        public float CellSize = 0.25f;
        public Transform Origin;

        public BuildGrid Grid { get; private set; }

        private readonly Dictionary<Vector3Int, Transform> _occupied = new();

        [Header("Debug Gizmos")]
        public bool DrawDebugGrid = true;
        public int DebugRadius = 5;

        private void Awake()
        {
            if (!Origin) Origin = transform;
            Grid = new BuildGrid(CellSize, Origin.position);
        }

        private void GetCellsForModule(ModuleConfig mod, Transform ghost, List<Vector3Int> result)
        {
            result.Clear();
            if (mod == null || ghost == null || Grid == null) return;

            float cell = Grid.CellSize;
            Vector3Int gridSize = mod.GridSize;

            Vector3 gridOriginLocal =
                new Vector3(
                    -(gridSize.x - 1) * cell * 0.5f,
                    -(gridSize.y - 1) * cell * 0.5f,
                    -(gridSize.z - 1) * cell * 0.5f
                );

            var mask = (mod.Occupancy != null &&
                        mod.Occupancy.LocalCells != null &&
                        mod.Occupancy.LocalCells.Length > 0)
                ? mod.Occupancy.LocalCells
                : new[] { Vector3Int.zero };

            foreach (var localCell in mask)
            {
                Vector3 localCenter =
                    gridOriginLocal +
                    new Vector3(
                        localCell.x * cell,
                        localCell.y * cell,
                        localCell.z * cell
                    );
                Vector3 worldCenter = ghost.TransformPoint(localCenter);

                Vector3Int worldCell = Grid.WorldToCell(worldCenter);

                if (!result.Contains(worldCell))
                    result.Add(worldCell);
            }
        }

        public bool CanPlace(ModuleConfig mod, Transform ghost, out List<Vector3Int> cells)
        {
            cells = new List<Vector3Int>();

            if (mod == null || ghost == null || Grid == null)
                return false;

            GetCellsForModule(mod, ghost, cells);

            foreach (var c in cells)
            {
                if (_occupied.ContainsKey(c))
                {
                    return false;
                }
            }

            return true;
        }

        public void Commit(ModuleConfig mod, Transform tr, List<Vector3Int> cells)
        {
            if (mod == null || tr == null || cells == null) return;

            foreach (var c in cells)
            {
                _occupied[c] = tr;
            }
        }

        public void Remove(BuildModuleRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.OccupiedCells == null) return;

            foreach (var cell in runtime.OccupiedCells)
            {
                if (_occupied.TryGetValue(cell, out var tr) && tr == runtime.transform)
                {
                    _occupied.Remove(cell);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!DrawDebugGrid) return;

            if (!Origin) Origin = transform;
            if (Grid == null)
                Grid = new BuildGrid(CellSize, Origin.transform.position);

            float s = CellSize;

            Gizmos.color = new Color(0f, 1f, 1f, 0.1f);

            Gizmos.color = new Color(1f, 0f, 0f, 0.45f);

            foreach (var kvp in _occupied)
            {
                Vector3 center = Grid.CellToWorld(kvp.Key);
                Gizmos.DrawCube(center, Vector3.one * (s * 0.9f));
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Origin.position, s * 0.15f);
        }
#endif
    }
}
