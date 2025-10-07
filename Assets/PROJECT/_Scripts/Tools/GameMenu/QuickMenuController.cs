using Service;
using UnityEngine;

namespace Menu
{
    public class QuickMenuController : IInitializable, IDisposable
    {
        private QuickMenuView _view;
        private IInputService _input;

        public QuickMenuController(QuickMenuView quickMenuView, IInputService inputService)
        {
            _view = quickMenuView;
            _input = inputService;
        }

        public void Initialize()
        {
            _input.AddActionListener(CharacterAction.Menu, onStarted: OnMenuPressed);

            if (_view.ResumeButton != null)
                _view.ResumeButton.Button.onClick.AddListener(OnResumeClicked);

            if (_view.SettingsButton != null)
                _view.SettingsButton.Button.onClick.AddListener(OnSettingsClicked);
        }

        public void Dispose()
        {
            _input.RemoveActionListener(CharacterAction.Menu, onStarted: OnMenuPressed);

            if (_view.ResumeButton != null)
                _view.ResumeButton.Button.onClick.RemoveListener(OnResumeClicked);

            if (_view.SettingsButton != null)
                _view.SettingsButton.Button.onClick.RemoveListener(OnSettingsClicked);

            _view.CloseAll();
        }

        private void OnMenuPressed()
        {
            if (_view.IsSettingsOpen)
            {
                Time.timeScale = 0f;
                _view.OpenQuick();
                return;
            }

            if (_view.IsQuickOpen)
            {
                Time.timeScale = 1f;
                _view.CloseAll();
                return;
            }

            _view.OpenQuick();
            Time.timeScale = 0f;
        }

        private void OnResumeClicked()
        {
            Time.timeScale = 1f;
            _view.CloseAll();
        }

        private void OnSettingsClicked()
        {
            Time.timeScale = 0f;
            _view.OpenSettings();
        }
    }
}

