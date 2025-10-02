using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    public abstract class InventoryModelBase : IInventoryModel
    {
        protected List<IInventoryItem> _items = new();

        public event Action Changed;

        public abstract int Capacity { get; }
        public IReadOnlyList<IInventoryItem> Items => _items;

        protected void OnChanged() => Changed?.Invoke();

        public bool TryAdd(IInventoryItem incoming)
        {
            if (incoming == null) return false;

            int leftover = incoming.Stack;

            for (int i = 0; i < _items.Count && leftover > 0; i++)
            {
                var it = _items[i];
                if (it == null || !it.CanMerge(incoming)) continue;
                leftover = it.AddToStack(leftover);
            }
            if (leftover <= 0) { OnChanged(); return true; }

            while (leftover > 0)
            {
                int empty = FindFirstEmptySlot();
                if (empty == -1) return false; 

                int toPlace = Mathf.Min(leftover, incoming.Config.MaxStack);
                var chunk = incoming.CloneWithStack(toPlace); 
                EnsureSize(empty + 1);
                _items[empty] = chunk;
                leftover -= toPlace;
            }

            OnChanged();
            return true;
        }

        private int FindFirstEmptySlot()
        {
            for (int i = 0; i < Capacity; i++)
                if (i >= _items.Count || _items[i] == null)
                    return i;
            return -1;
        }

        public virtual bool TryRemoveAt(int index, int count = int.MaxValue)
        {
            if (index < 0 || index >= _items.Count) return false;
            var it = _items[index];
            if (it == null) return false;

            if (count >= it.Stack) _items[index] = default;
            else it.RemoveFromStack(count);

            OnChanged();
            return true;
        }

        /// <summary>
        /// ѕеренос / стак / свап между слотами.
        /// </summary>
        public virtual bool MoveOrMerge(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return false;
            if (fromIndex < 0 || fromIndex >= _items.Count) return false;
            if (toIndex < 0 || toIndex >= Capacity) return false;

            var from = _items[fromIndex];
            if (from == null) return false;

            // цель пуста€
            if (toIndex >= _items.Count || _items[toIndex] == null)
            {
                EnsureSize(toIndex + 1);
                _items[toIndex] = from;
                _items[fromIndex] = null;
                OnChanged();
                return true;
            }

            var to = _items[toIndex];

            // пробуем стакнуть
            if (to.CanMerge(from))
            {
                int leftover = to.AddToStack(from.Stack);
                if (leftover <= 0)
                {
                    _items[fromIndex] = null;
                }
                else
                {
                    // часть не влезла Ч вернуть остаток в from
                    int removed = from.RemoveFromStack(from.Stack - leftover); // Ђсн€лиї в to
                }
                OnChanged();
                return true;
            }

            // иначе свап
            _items[toIndex] = from;
            _items[fromIndex] = to;
            OnChanged();
            return true;
        }

        public virtual void Sort(IInventorySorter sorter)
        {
            if (sorter == null) return;

            _items.Sort((a, b) =>
            {
                bool ae = a == null;
                bool be = b == null;
                if (ae && be) return 0;
                if (ae) return 1;  
                if (be) return -1;
                return sorter.Compare(a, b);
            });
            OnChanged();
        }

        protected void EnsureSize(int size)
        {
            while (_items.Count < size && _items.Count < Capacity)
                _items.Add(null);
        }
    }
}