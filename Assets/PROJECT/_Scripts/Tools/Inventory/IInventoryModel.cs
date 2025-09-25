using System.Collections.Generic;

namespace Inventory
{
    public interface IInventoryModel
    {
        int Capacity { get; }
        IReadOnlyList<InventoryItem> Items { get; }

        bool CanAdd(InventoryItem item);
        bool AddItem(InventoryItem item);
        bool RemoveItem(InventoryItem item);
    }
}
