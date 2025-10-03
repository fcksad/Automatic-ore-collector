using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{

    [CreateAssetMenu(fileName = "ItemViewRegistry", menuName = "Configs/Inventory/ItemViewRegistry")]
    public class ItemViewRegistry : ScriptableObject //use in u have different item prefabs
    {
        [System.Serializable]
        public struct Entry { public ItemType Type; public DraggableItemView Prefab; }

        [SerializeField] private Entry[] _entries;
        [SerializeField] private DraggableItemView _defaultPrefab;

        private Dictionary<ItemType, DraggableItemView> _map;

        private void OnEnable()
        {
            _map = new Dictionary<ItemType, DraggableItemView>(_entries.Length);
            foreach (var e in _entries) _map[e.Type] = e.Prefab;
        }

        public DraggableItemView CreateView(Transform parent, ItemType type)
        {
            var prefab = (_map != null && _map.TryGetValue(type, out var p)) ? p : _defaultPrefab;
            return Instantiate(prefab, parent);
        }
    }

}

