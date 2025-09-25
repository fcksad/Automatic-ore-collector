using System.Collections.Generic;

namespace Inventory
{
    public abstract class InventoryModelBase : IInventoryModel
    {
        protected List<InventoryItem> _items = new();

        public abstract int Capacity { get; }
        public IReadOnlyList<InventoryItem> Items => _items;

        public abstract bool CanAdd(InventoryItem item);
        public abstract bool AddItem(InventoryItem item);
        public abstract bool RemoveItem(InventoryItem item);
    }
}