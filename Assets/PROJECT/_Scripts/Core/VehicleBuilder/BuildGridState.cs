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

        private void Awake()
        {
            if (!Origin) Origin = transform;
            Grid = new BuildGrid(CellSize, Origin.position);
        }


        public bool CanPlace(ModuleConfig mod, Transform ghost, out List<Vector3Int> cells)
        {
            cells = new List<Vector3Int>();

            if (mod == null || ghost == null)
                return false;

            var cell = Grid.WorldToCell(ghost.position);
            cells.Add(cell);

            foreach (var c in cells)
            {
                if (_occupied.ContainsKey(c))
                    return false;
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
    }
}
