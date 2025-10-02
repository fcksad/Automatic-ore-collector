namespace Inventory
{
    public interface IInventoryView
    {
        public void Bind(IInventoryModel model);
        public void Rebuild();
    }

}
