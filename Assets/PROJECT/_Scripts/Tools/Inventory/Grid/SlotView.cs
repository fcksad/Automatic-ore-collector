using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory
{
    public class SlotView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _highlight;
        [SerializeField] private Transform _contentRoot;
        public int Index { get; private set; }
        private IInventoryModel _model;

        public Transform ContentRoot => _contentRoot != null ? _contentRoot : transform;

        public void Setup(IInventoryModel model, int index)
        {
            _model = model;
            Index = index;
            SetHighlight(Color.black);
        }

        public void ClearItem()
        {
            var root = ContentRoot;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (DragContext.Model == null || DragContext.Item == null) return;
            if (_model != DragContext.Model) return;

            _model.MoveOrMerge(DragContext.FromIndex, Index);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlight(Color.white);
        public void OnPointerExit(PointerEventData eventData) => SetHighlight(Color.black);

        private void SetHighlight(Color color)
        {
            if (_highlight != null) _highlight.color = color;
        }
    }
}
