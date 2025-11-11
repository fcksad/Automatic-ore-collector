using System.Collections.Generic;
using UnityEngine;
using Inventory;

namespace Builder
{
    public class BuildModuleRuntime : MonoBehaviour
    {
        public InventoryItemConfig SourceConfig;   
        public BuildGridState GridState;         
        public List<Vector3Int> OccupiedCells = new(); 
    }
}
