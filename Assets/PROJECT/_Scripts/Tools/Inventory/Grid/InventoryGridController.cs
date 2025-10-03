using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    public class InventoryGridController : MonoBehaviour
    {
        [field: SerializeField] public List<GridEntry> InventoryEntries { get; private set; } = new();
        private readonly Dictionary<ItemType, GridInventoryModel> _models = new();

        [Header("Grid Settings")]
        private int _width = 9;
        private int _height = 12;

        private void Awake()
        {
            foreach (var entry in InventoryEntries)
            {
                entry.Model = new GridInventoryModel(_width, _height);
                entry.View.Bind(entry.Model);

                _models.Add(entry.Type, entry.Model);
            }
        }

        public void AddItem(InventoryItemConfig inventoryItemConfig, int count = 1)
        {
            GetModelByType(inventoryItemConfig.ItemType).TryAdd(new InventoryItemBase(inventoryItemConfig, count));
        }

        private GridInventoryModel GetModelByType(ItemType type)
        {
            return _models[type];
        }
    }

    [Serializable]
    public class GridEntry
    {
        public ItemType Type;
        public GridInventoryView View;
        [HideInInspector] public GridInventoryModel Model;
    }
}


