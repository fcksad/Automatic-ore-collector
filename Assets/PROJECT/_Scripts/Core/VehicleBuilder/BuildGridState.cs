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

            if (mod == null)
            {
                Debug.LogWarning("[GridState.CanPlace] ❌ ModuleConfig = null");
                return false;
            }

            if (ghost == null)
            {
                Debug.LogWarning("[GridState.CanPlace] ❌ Ghost = null");
                return false;
            }

            var worldPos = ghost.position;
            var cell = Grid.WorldToCell(worldPos);
            cells.Add(cell);

            Debug.Log(
                $"[GridState.CanPlace] ---- CHECK ----\n" +
                $"• Ghost world: {worldPos}\n" +
                $"• Cell: {cell}\n" +
                $"• Rotation: {ghost.rotation.eulerAngles}\n" +
                $"• Model: {mod.name}"
            );

            foreach (var c in cells)
            {
                if (_occupied.ContainsKey(c))
                {
                    Debug.LogWarning(
                        $"[GridState.CanPlace] ❌ CELL BLOCKED!\n" +
                        $"• Cell: {c}\n" +
                        $"• Occupied by: {_occupied[c].name}"
                    );

                    return false;
                }
                else
                {
                    Debug.Log($"[GridState.CanPlace] ✔ Free cell: {c}");
                }
            }

            Debug.Log($"[GridState.CanPlace] ✔ RESULT: Placement OK\n");
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
