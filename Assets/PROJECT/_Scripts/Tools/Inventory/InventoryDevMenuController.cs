using UnityEngine;
namespace Inventory
{
    public class InventoryDevMenuController : MonoBehaviour
    {

        [Header("Item to spawn")]
        public InventoryItemConfig Config;
        [Min(1)] public int Stack = 1;

        [Header("Grid")]
        public GridInventoryView Grid;
        [Min(1)] public int Width = 5;
        [Min(1)] public int Height = 4;

        private GridInventoryModel _model;

        private void Awake()
        {
            _model = new GridInventoryModel(Width, Height);
            if (Grid != null) Grid.Bind(_model);
        }

        private void Start()
        {
            SpawnItem();
            SpawnItem();
            SpawnItem();
            SpawnItem();
            SpawnItem();
        }

        [ContextMenu("SpawnItem")]
        public void SpawnItem()
        {
            if (Config == null || _model == null) { Debug.LogWarning("No Config or model"); return; }


            var item = new InventoryItemBase(Config, Mathf.Max(1, Stack));
            var ok = _model.TryAdd(item);

            if (!ok)
                Debug.LogWarning("Inventory is full or item couldn't be added");
        }

        [ContextMenu("Sort by name")]
        public void SortByName()
        {
            if (_model == null) return;
            _model.Sort(new SortByName());
        }

        [ContextMenu("Clear inventory")]
        public void ClearAll()
        {
            if (_model == null) return;

            _model = new GridInventoryModel(Width, Height);
            Grid.Bind(_model); 
        }

    }
}


