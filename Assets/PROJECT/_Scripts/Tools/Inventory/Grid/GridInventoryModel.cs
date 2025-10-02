using System;

namespace Inventory
{
    public class GridInventoryModel : InventoryModelBase
    {
        private readonly int _capacity;
        public GridInventoryModel(int width, int height) => _capacity = Math.Max(0, width * height);
        public override int Capacity => _capacity;
    }
}
