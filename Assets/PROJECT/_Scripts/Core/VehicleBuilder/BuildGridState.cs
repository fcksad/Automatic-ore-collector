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

        /// <summary>
        /// Можно ли поставить модуль в текущей позе госта.
        /// Пока что считаем, что модуль занимает ровно ОДНУ клетку по своему центру.
        /// Потом сюда можно прикрутить ModuleOccupancyMask.
        /// </summary>
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
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Зафиксировать модуль в гриде.
        /// </summary>
        public void Commit(ModuleConfig mod, Transform tr, List<Vector3Int> cells)
        {
            if (mod == null || tr == null || cells == null) return;

            foreach (var c in cells)
            {
                _occupied[c] = tr;
            }
        }
    }
}
