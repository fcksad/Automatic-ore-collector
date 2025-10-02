using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private IInventoryModel _model;
        private IInventoryItem _item;
        private int _slotIndex;

        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private bool _isDragging;

        private InventoryItemView _card;

        public void Bind(IInventoryModel model, IInventoryItem item, int slotIndex, Canvas rootCanvas, InventoryItemView card)
        {
            _model = model;
            _item = item;
            _slotIndex = slotIndex;
            _rootCanvas = rootCanvas;
            _canvasGroup = GetComponent<CanvasGroup>();
            _card = card;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_item == null || _isDragging) return;
            _isDragging = true;

            DragContext.Model = _model;
            DragContext.FromIndex = _slotIndex;
            DragContext.Item = _item;
            DragContext.Owner = this;

            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));
            DragContext.Ghost = go.GetComponent<RectTransform>();
            DragContext.GhostCanvas = go.GetComponent<Canvas>();
            DragContext.GhostGroup = go.GetComponent<CanvasGroup>();
            var img = go.GetComponent<Image>();

            DragContext.Ghost.SetParent(_rootCanvas.transform, false);
            DragContext.GhostCanvas.overrideSorting = true;
            DragContext.GhostCanvas.sortingOrder = 999;
            DragContext.GhostGroup.blocksRaycasts = false;
            img.raycastTarget = false;

            img.sprite = _card != null ? _card.IconSprite : null;
            DragContext.Ghost.sizeDelta = (_card != null && _card.GetComponentInChildren<Image>() != null)? (_card.GetComponentInChildren<Image>().rectTransform.sizeDelta): new Vector2(64, 64);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.5f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (DragContext.Ghost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootCanvas.transform as RectTransform, eventData.position, _rootCanvas.worldCamera, out var local);
            DragContext.Ghost.anchoredPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
            _isDragging = false;
            DragContext.ClearIfOwner(this);
        }

        private void OnDisable() { _isDragging = false; if (_canvasGroup) { _canvasGroup.blocksRaycasts = true; _canvasGroup.alpha = 1f; } DragContext.ClearIfOwner(this); }
        private void OnDestroy() { _isDragging = false; DragContext.ClearIfOwner(this); }

    }
}
