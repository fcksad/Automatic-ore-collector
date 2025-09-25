using UnityEngine;

namespace Menu
{
    public class QuickMenuView : MonoBehaviour
    {
        [field: SerializeField] public CustomButton ToggleButton { get; private set; }
        [SerializeField] private ToggleWindow _toggleWindow;

        public void Toggle(bool value)
        {
            _toggleWindow.Toggle(value);
        }
    }
}
