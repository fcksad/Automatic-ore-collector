using Service;
using UnityEngine;

namespace Menu
{
    public class QuickMenuController : IInitializable, IDisposable
    {
        private QuickMenuView _quickMenuView;
        private IInputService _inputService;

        public QuickMenuController(QuickMenuView quickMenuView, IInputService inputService)
        {
            _quickMenuView = quickMenuView;
            _inputService = inputService;
        }

        public void Initialize()
        {
            _inputService.AddActionListener(CharacterAction.Menu, onStarted: EnableMenu);
            _quickMenuView.ToggleButton.Button.onClick.AddListener(DisableMenu);
        }

        public void Dispose()
        {
            _inputService.RemoveActionListener(CharacterAction.Menu, onStarted: EnableMenu);
            _quickMenuView.ToggleButton.Button.onClick.RemoveListener(DisableMenu);
        }
        public void EnableMenu()
        {
            _inputService.RemoveActionListener(CharacterAction.Menu, onStarted: EnableMenu);

            _quickMenuView.Toggle(true);
            _inputService.ChangeInputMap(InputMapType.UI);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void DisableMenu()
        {
            _quickMenuView.Toggle(false);
            _inputService.ChangeInputMap(InputMapType.Player);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _inputService.AddActionListener(CharacterAction.Menu, onStarted: EnableMenu);
        }
    }
}

