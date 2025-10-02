using System.Collections.Generic;

namespace Inventory
{
    public interface IInventoryModel
    {
        int Capacity { get; }
        IReadOnlyList<IInventoryItem> Items { get; }
        event System.Action Changed;

        bool MoveOrMerge(int fromIndex, int index);
        void Sort(IInventorySorter sort);
        bool TryAdd(IInventoryItem item);
        bool TryRemoveAt(int index, int count = int.MaxValue);
    }
}
