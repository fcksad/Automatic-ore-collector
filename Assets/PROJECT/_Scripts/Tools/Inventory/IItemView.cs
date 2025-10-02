
namespace Inventory
{
    public interface IItemView
    {
        void Bind(IInventoryItem item);
        void Unbind();
        void SetSelected(bool selected);
        UnityEngine.GameObject gameObject { get; }
    }

}
