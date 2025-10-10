using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class BuildGrid
    {
        public readonly float CellSize;
        public readonly Vector3 Origin;
        private readonly HashSet<Vector3Int> _occ = new();

        public BuildGrid(float cellSize, Vector3 origin) { CellSize = cellSize; Origin = origin; }

        public Vector3Int WorldToCell(Vector3 w)
        {
            var p = (w - Origin) / CellSize;
            return new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
        }
        public Vector3 CellToWorld(Vector3Int c) => Origin + (Vector3)c * CellSize;

        public bool WillCollide(IEnumerable<Vector3Int> cells)
        { foreach (var c in cells) if (_occ.Contains(c)) return true; return false; }

        public int ContactCount(IEnumerable<Vector3Int> cells)
        {
            int cnt = 0;
            foreach (var c in cells)
            {
                if (_occ.Contains(c + Vector3Int.right) ||
                    _occ.Contains(c + Vector3Int.left) ||
                    _occ.Contains(c + Vector3Int.up) ||
                    _occ.Contains(c + Vector3Int.down) ||
                    _occ.Contains(c + new Vector3Int(0, 0, 1)) ||
                    _occ.Contains(c + new Vector3Int(0, 0, -1))) { cnt++; break; }
            }
            return cnt;
        }

        public void Occupy(IEnumerable<Vector3Int> cells) { foreach (var c in cells) _occ.Add(c); }
        public void Free(IEnumerable<Vector3Int> cells) { foreach (var c in cells) _occ.Remove(c); }
    }
}
