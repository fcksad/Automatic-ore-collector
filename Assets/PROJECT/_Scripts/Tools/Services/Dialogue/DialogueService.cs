using System;
using System.Collections.Generic;
namespace Service
{
    public class DialogueService : IDialogueService
    {
        private DialogueView _dialogueView;
        private IInputService _inputService;

        private DialogueConfig _currentConfig;
        private int _currentIndex;
        private Action _onComplete;
        private List<string> _localizedLines;

        public DialogueService(DialogueView dialogueView, IInputService inputService)
        {
            _dialogueView = dialogueView;
            _inputService = inputService;
        }

        public async void Show(DialogueConfig config, Action onCompleted)
        {
            _currentConfig = config;
            _currentIndex = 0;
            _onComplete = onCompleted;


            _dialogueView.OnLineFullyShownEvent += HandleAutoContinue;
            _inputService.AddActionListener(CharacterAction.Any, HandleSkip);

            _dialogueView.ShowView();

            _localizedLines = await _currentConfig.GetLocalizedLinesAsync();


            ShowNext();

        }
        private void ShowNext()
        {
            if (_currentIndex >= _localizedLines.Count)
            {
                EndDialogue();
                return;
            }

            var line = _localizedLines[_currentIndex];
            _currentIndex++;

            _dialogueView.PlayLine(line);
        }

        private void HandleSkip()
        {
            if (_dialogueView.IsPrinting)
                _dialogueView.ForceCompleteLine();
            else
                ShowNext();
        }

        private void HandleAutoContinue()
        {
            ShowNext();
        }

        private void EndDialogue()
        {
            _inputService.RemoveActionListener(CharacterAction.Any, HandleSkip);
            _dialogueView.OnLineFullyShownEvent -= HandleAutoContinue;
            _dialogueView.HideView();
            _onComplete?.Invoke();
        }

        public void Stop()
        {
            _dialogueView.OnLineFullyShownEvent -= HandleAutoContinue;
            _inputService.RemoveActionListener(CharacterAction.Any, HandleSkip);
            _dialogueView.HideView();
        }
    }
}
