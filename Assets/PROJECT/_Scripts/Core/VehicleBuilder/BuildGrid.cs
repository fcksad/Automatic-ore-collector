using UnityEngine;

namespace Builder
{
    public class BuildGrid
    {
        public readonly float CellSize;
        public readonly Vector3 Origin;

        public BuildGrid(float cellSize, Vector3 origin)
        {
            CellSize = cellSize;
            Origin = origin;
        }

        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            Vector3 p = (worldPos - Origin) / CellSize;

            return new Vector3Int(
                Mathf.RoundToInt(p.x),
                Mathf.RoundToInt(p.y),
                Mathf.RoundToInt(p.z));
        }

        public Vector3 CellToWorld(Vector3Int cell)
        {
            return Origin + (Vector3)cell * CellSize;
        }
    }
}
