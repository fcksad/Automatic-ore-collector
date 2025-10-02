using UnityEngine;

namespace Inventory
{
    public interface IInventorySorter
    {
        int Compare(IInventoryItem a, IInventoryItem b);
    }
}