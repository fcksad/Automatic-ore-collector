namespace Inventory
{
    public interface IInventoryView<TModel> where TModel : IInventoryModel
    {
        void Initialize(TModel model);
        void Refresh();
        void OnItemAdded(InventoryItem item);
        void OnItemRemoved(InventoryItem item);
    }

}
