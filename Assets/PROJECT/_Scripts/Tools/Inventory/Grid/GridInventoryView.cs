using System.Collections.Generic;
using UnityEngine;
namespace Inventory
{
    public class GridInventoryView : MonoBehaviour, IInventoryView
    {
        [Header("Prefabs & Parents")]
        [SerializeField] private SlotView _slotPrefab;
        [SerializeField] private DraggableItemView _itemViewPrefab;
        [SerializeField] private Transform _contentParent;

        private IInventoryModel _model;
        private readonly List<SlotView> _slots = new();
        [SerializeField] private Canvas _rootCanvas;

        public void Bind(IInventoryModel model)
        {
            if (_model != null) _model.Changed -= Rebuild;
            _model = model;
            if (_model != null)
            {
                _model.Changed += Rebuild;
                BuildSlots();
                Rebuild();
            }
            else
            {
                Clear();
            }
        }

        private void BuildSlots()
        {
            Clear();
            int n = _model.Capacity;
            for (int i = 0; i < n; i++)
            {
                var slot = Instantiate(_slotPrefab, _contentParent);
                slot.Setup(_model, i);
                _slots.Add(slot);
            }
        }

        public void Rebuild()
        {
            foreach (var slot in _slots)
                slot.ClearItem();

            var items = _model.Items;
            for (int i = 0; i < _slots.Count; i++)
            {
                var item = (i < items.Count) ? items[i] : null;
                if (item == null) continue;

                var go = Instantiate(_itemViewPrefab, _slots[i].ContentRoot);
                var card = go.GetComponent<InventoryItemView>();
                if (card) card.Bind(item);

                var drag = go.GetComponent<DraggableItemView>();
                if (drag) drag.Bind(_model, item, i, _rootCanvas, card);
            }
        }

        private void Clear()
        {
            foreach (var s in _slots) if (s) Destroy(s.gameObject);
            _slots.Clear();
        }

        private void OnDestroy()
        {
            if (_model != null) _model.Changed -= Rebuild;
        }
    }
}
