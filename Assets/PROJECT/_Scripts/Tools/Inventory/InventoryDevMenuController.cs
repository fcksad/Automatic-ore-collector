using Service;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    public class InventoryDevMenuController : MonoBehaviour
    {

        [Header("Item to spawn")]
        [field: SerializeField]public List<InventoryItemConfig> Configs {  get; private set; } = new List<InventoryItemConfig>();
        [Min(1)] public int Stack = 1;

        private InventoryGridController _inventoryGridController;
        private IInputService _inputService;

        private void Start()
        {
            _inventoryGridController = SceneServiceLocator.Current.Get<InventoryGridController>();
            _inputService = ServiceLocator.Get<IInputService>();

            SpawnItem();
            SpawnItem();
            SpawnItem();
            SpawnItem();
            SpawnItem();

            _inputService.AddActionListener(CharacterAction.SpawnItem, SpawnItem);
        }

        [ContextMenu("SpawnItem")]
        public void SpawnItem()
        {
            var randomValue = Random.Range(0, Configs.Count);

            _inventoryGridController.AddItem(Configs[randomValue], Stack);
        }

/*        [ContextMenu("Sort by name")]
        public void SortByName()
        {
            if (_model == null) return;
            _model.Sort(new SortByName());
        }*/

/*        [ContextMenu("Clear inventory")]
        public void ClearAll()
        {
            if (_model == null) return;

            _model = new GridInventoryModel(Width, Height);
            Grid.Bind(_model); 
        }*/

    }
}


