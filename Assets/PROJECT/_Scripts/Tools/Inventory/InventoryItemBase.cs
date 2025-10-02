using UnityEngine;



namespace Inventory
{
    public class InventoryItemBase : IInventoryItem
    {
        public InventoryItemConfig Config { get; }
        public string Id => Config.Id;
        public int Stack { get; private set; }

        public InventoryItemBase(InventoryItemConfig config, int stack = 1)
        {
            Config = config;
            Stack = Mathf.Clamp(stack, 1, Mathf.Max(1, config.MaxStack));
        }

        public bool CanMerge(IInventoryItem other)
        {
            if (other == null) return false;
            if (other.Id != Id) return false;
            if (Config.MaxStack <= 1) return false;

            // Если у тебя есть динамика (например, разная Durability) —
            // тут можно запретить merge при несовпадении важных полей.
            if (other is InventoryItemBase oi)
            {
 /*               // пример: не сливаем, если разный RollLevel
                if (RollLevel.HasValue || oi.RollLevel.HasValue)
                    return RollLevel == oi.RollLevel;*/
            }
            return true;
        }

        public int AddToStack(int amount)
        {
            if (Config.MaxStack <= 1) return amount;
            if (amount <= 0) return 0;

            int free = Config.MaxStack - Stack;
            int toAdd = Mathf.Clamp(amount, 0, free);
            Stack += toAdd;
            return amount - toAdd; // остаток
        }

        public int RemoveFromStack(int amount)
        {
            if (amount <= 0) return 0;
            int toRemove = Mathf.Clamp(amount, 0, Stack);
            Stack -= toRemove;
            return toRemove;
        }

        public IInventoryItem CloneWithStack(int stack)
        {
            var clone = new InventoryItemBase(Config, Mathf.Clamp(stack, 1, Config.MaxStack));
/*            clone.Durability = Durability;
            clone.RollLevel = RollLevel;*/
            return clone;
        }

        public ItemData ToSave() => new ItemData
        {
            Id = Id,
            Stack = Stack,
/*            Durability = Durability,
            RollLevel = RollLevel*/
        };
        public static InventoryItemBase FromSave(ItemData d, System.Func<string, InventoryItemConfig> getById)
        {
            var cfg = getById(d.Id);
            var it = new InventoryItemBase(cfg, d.Stack);
/*            it.Durability = d.Durability;
            it.RollLevel = d.RollLevel;*/
            return it;
        }
    }

}
