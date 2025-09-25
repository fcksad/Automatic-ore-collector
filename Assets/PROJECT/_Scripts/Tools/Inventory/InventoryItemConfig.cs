using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName = "InventoryItemConfig", menuName = "Configs/Inventory/InventoryItemConfig")]
    public class InventoryItemConfig : ScriptableObject
    {
        [field: SerializeField] public string ID { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public float Weight { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
    }

}
