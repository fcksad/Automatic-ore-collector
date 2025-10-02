namespace Inventory
{
    public interface IInventoryItem
    {
        string Id { get; }
        InventoryItemConfig Config { get; }
        int Stack { get; }
        bool CanMerge(IInventoryItem other);
        int AddToStack(int amount);
        int RemoveFromStack(int amount);
        IInventoryItem CloneWithStack(int stack);
    }
}
