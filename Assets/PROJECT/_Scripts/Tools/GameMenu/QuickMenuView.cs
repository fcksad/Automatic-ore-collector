using UnityEngine;

namespace Menu
{
    public class QuickMenuView : MonoBehaviour
    {
        [field: SerializeField] public CustomButton ResumeButton { get; private set; }
        [field: SerializeField] public CustomButton SettingsButton { get; private set; }

        [SerializeField] private ToggleWindow _quickMenu;
        [SerializeField] private ToggleWindow _settingsMenu;

        public bool IsQuickOpen => _quickMenu != null && _quickMenu.isActiveAndEnabled;
        public bool IsSettingsOpen => _settingsMenu != null && _settingsMenu.isActiveAndEnabled;
        public bool AnyOpen => IsQuickOpen || IsSettingsOpen;

        public void OpenQuick()
        {
            if (_settingsMenu != null) _settingsMenu.Toggle(false);
            if (_quickMenu != null) _quickMenu.Toggle(true);
        }

        public void OpenSettings()
        {
            if (_quickMenu != null) _quickMenu.Toggle(false);
            if (_settingsMenu != null) _settingsMenu.Toggle(true);
        }

        public void CloseAll()
        {
            if (_quickMenu != null) _quickMenu.Toggle(false);
            if (_settingsMenu != null) _settingsMenu.Toggle(false);
        }
    }
}
