using UnityEngine;

namespace Inventory
{
    public static class DragContext
    {
        public static IInventoryModel Model;
        public static int FromIndex = -1;
        public static IInventoryItem Item;

        public static RectTransform Ghost;
        public static Canvas GhostCanvas;
        public static CanvasGroup GhostGroup;

        public static Object Owner;

        public static void Clear()
        {
            if (Ghost != null)
                Object.Destroy(Ghost.gameObject);

            Model = null;
            FromIndex = -1;
            Item = null;

            Ghost = null;
            GhostCanvas = null;
            GhostGroup = null;
            Owner = null;
        }

        public static void ClearIfOwner(Object o)
        {
            if (Owner == o) Clear();
        }
    }
}
