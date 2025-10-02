using UnityEngine;

namespace Inventory
{

    [CreateAssetMenu(fileName = "ItemViewRegistry", menuName = "Configs/Inventory/ItemViewRegistry")]
    public class ItemViewRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry { public ItemType Type; public InventoryItemView Prefab; }

        [SerializeField] private Entry[] _entries;

        public IItemView CreateView(Transform parent, ItemType type)
        {
            foreach (var entry in _entries)
                if (entry.Type == type)
                    return Instantiate(entry.Prefab, parent);

            return Instantiate(_entries[0].Prefab, parent);
        }
    }

}

