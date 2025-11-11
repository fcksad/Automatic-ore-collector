using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    /// <summary>
    /// Хранит состояние грид-а: какие клетки заняты какими модулями.
    /// </summary>
    public class BuildGridState : MonoBehaviour
    {
        [Header("Грид")]
        public float CellSize = 0.25f;
        public Transform Root; 

        public BuildGrid Grid { get; private set; }

        private readonly Dictionary<Vector3Int, ModuleInstance> _cells = new();

        private static readonly Vector3Int[] NeighbourDirs =
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        private void Awake()
        {
            if (!Root) Root = transform;
            Grid = new BuildGrid(CellSize, Root.position);
        }

        public struct ModuleInstance
        {
            public ModuleConfig Config;
            public Transform RootTransform;
        }

        /// <summary>
        /// Проверяем, можно ли поставить модуль в текущем положении госта.
        /// </summary>
        public bool CanPlace(ModuleConfig config, Transform ghostTransform, out List<Vector3Int> occupiedCells)
        {
            occupiedCells = new List<Vector3Int>();

            if (config == null)
                return false;

            var occupancy = config.Occupancy;
            if (occupancy == null || occupancy.LocalCells == null || occupancy.LocalCells.Length == 0)
            {
                // Фолбек: считаем 1x1x1
                var pivotCell = Grid.WorldToCell(ghostTransform.position);
                if (_cells.ContainsKey(pivotCell)) return false;
                occupiedCells.Add(pivotCell);
                return CheckAdjacency(occupiedCells);
            }

            // 1) Считаем, какие клетки займёт модуль с учётом его трансформа
            foreach (var localCell in occupancy.LocalCells)
            {
                var localOffset = (Vector3)localCell * CellSize;
                var worldPos = ghostTransform.TransformPoint(localOffset);
                var cell = Grid.WorldToCell(worldPos);

                if (_cells.ContainsKey(cell))
                {
                    // столкновение с уже занятым
                    return false;
                }

                if (!occupiedCells.Contains(cell))
                    occupiedCells.Add(cell);
            }

            // 2) Если это не первый модуль, проверяем, что есть контакт хотя бы по одной грани
            if (_cells.Count > 0)
            {
                if (!CheckAdjacency(occupiedCells))
                    return false;
            }

            return true;
        }

        private bool CheckAdjacency(List<Vector3Int> newCells)
        {
            foreach (var cell in newCells)
            {
                foreach (var dir in NeighbourDirs)
                {
                    if (_cells.ContainsKey(cell + dir))
                        return true;
                }
            }

            // разрешаем первый модуль без соседей
            return _cells.Count == 0;
        }

        public void Commit(ModuleConfig config, Transform instanceRoot, List<Vector3Int> occupiedCells)
        {
            var inst = new ModuleInstance
            {
                Config = config,
                RootTransform = instanceRoot
            };

            foreach (var cell in occupiedCells)
                _cells[cell] = inst;
        }

#if UNITY_EDITOR
        // Визуализация занимаемых клеток в сцене
        private void OnDrawGizmos()
        {
            if (Grid == null)
            {
                var origin = Root ? Root.position : transform.position;
                Grid = new BuildGrid(CellSize, origin);
            }

            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            foreach (var kvp in _cells)
            {
                var world = Grid.CellToWorld(kvp.Key);
                Gizmos.DrawWireCube(world, Vector3.one * CellSize * 0.9f);
            }
        }
#endif
    }
}
