using UnityEngine;

namespace Inventory
{

    public enum ItemType { Body = 0, Wheeles = 1, Turret = 2, Pilot = 3, Bamper = 4 }

    [CreateAssetMenu(fileName = "InventoryItemConfig", menuName = "Configs/Inventory/InventoryItemConfig")]
    public class InventoryItemConfig : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public ItemType ItemType { get; private set; }
        [field: SerializeField, SpritePreview(allowUpscale: false, layout: SpritePreviewLayout.StackedBelow)] public Sprite Icon { get; private set; }
        [field: SerializeField] public float Weight { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public int MaxStack { get; private set; } = 1;
    }

}
