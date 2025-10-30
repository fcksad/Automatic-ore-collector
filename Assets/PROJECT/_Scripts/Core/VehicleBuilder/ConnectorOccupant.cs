using System.Collections.Generic;
using UnityEngine;

public class ConnectorOccupant : MonoBehaviour
{
    private ConnectorSurface _surface;
    private List<Vector2Int> _cells;

    public void Init(ConnectorSurface surface, IEnumerable<Vector2Int> cells)
    {
        _surface = surface;
        _cells = new List<Vector2Int>(cells);
    }

    private void OnDestroy()
    {
        if (_surface != null && _cells != null)
            _surface.Release(_cells);
    }
}
