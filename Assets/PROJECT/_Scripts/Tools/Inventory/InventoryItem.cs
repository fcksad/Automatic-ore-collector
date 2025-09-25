using System;
using UnityEngine;

namespace Inventory
{
    [Serializable]
    public class InventoryItem
    {
        [field: SerializeField] public string ID { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }

        // [field: SerializeField] public Vector2Int Size { get; private set; } = Vector2Int.one;

        /*    [field: SerializeField] public int MaxStack { get; private set; } = 1;
            [field: SerializeField] public int CurrentStack { get; private set; } = 1;*/

        [field: SerializeField] public float Weight { get; private set; }
        [field: SerializeField] public string Description { get; private set; }

        public InventoryItem(string id, string displayName, Sprite icon, Vector2Int size, int maxStack = 1)
        {
            ID = id;
            DisplayName = displayName;
            Icon = icon;
            /*        Size = size;
                    MaxStack = maxStack;
                    CurrentStack = 1;*/
        }

        /* public bool CanStackWith(InventoryItem other)
         {
             return ID == other.ID && MaxStack > 1;
         }

         public bool AddToStack(int amount)
         {
             if (CurrentStack + amount > MaxStack)
                 return false;

             CurrentStack += amount;
             return true;
         }

         public bool RemoveFromStack(int amount)
         {
             if (CurrentStack - amount < 0)
                 return false;

             CurrentStack -= amount;
             return true;
         }

         public InventoryItem Clone()
         {
             return new InventoryItem(ID, DisplayName, Icon, Size, MaxStack)
             {
                 CurrentStack = CurrentStack,
                 Weight = Weight,
                 Description = Description
             };
         }*/
    }

}
