using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    public class InventoryItemView : MonoBehaviour, IItemView
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _stack;

        public Sprite IconSprite => _icon ? _icon.sprite : null;

        private IInventoryItem _item;

        public void Bind(IInventoryItem item)
        {
            _item = item;
            Refresh();
        }

        public void Unbind() => _item = null;
        public void SetSelected(bool selected) { }

        private void Refresh()
        {
            if (_item == null) return;

            var config = _item.Config;
            if (_icon) _icon.sprite = config.Icon;
            if (_title) _title.SetText(config.DisplayName);

            if (_stack)
            {
                if (config.MaxStack > 1)
                {
                    _stack.SetText(_item.Stack.ToString());
                    _stack.gameObject.SetActive(_item.Stack > 1); 
                }
                else
                {
                    _stack.SetText("");
                    _stack.gameObject.SetActive(false);
                }
            }
        }
    }
}
