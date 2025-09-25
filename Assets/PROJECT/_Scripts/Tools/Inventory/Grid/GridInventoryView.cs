using UnityEngine;
namespace Inventory
{
    public class GridInventoryView : MonoBehaviour, IInventoryView<ListInventoryModel>
    {
        public void Initialize(ListInventoryModel model)
        {
            throw new System.NotImplementedException();
        }

        public void OnItemAdded(InventoryItem item)
        {
            throw new System.NotImplementedException();
        }

        public void OnItemRemoved(InventoryItem item)
        {
            throw new System.NotImplementedException();
        }

        public void Refresh()
        {
            throw new System.NotImplementedException();
        }
    }
}
