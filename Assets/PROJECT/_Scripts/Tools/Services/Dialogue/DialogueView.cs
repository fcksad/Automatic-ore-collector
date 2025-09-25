using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Service
{
    public class DialogueView : MonoBehaviour
    {
        [field: SerializeField] public TextMeshProUGUI Text { get; private set; }
        [SerializeField] private float _charDelay = 0.05f;

        [SerializeField] private bool _autoContinue = true;
        [SerializeField] private float _autoContinueDelay = 2f;

        [SerializeField] private AudioConfig _keyAudio;

        public event Action OnLineFullyShownEvent;

        public bool IsPrinting => _printingCoroutine != null;

        private Coroutine _printingCoroutine;

        private IAudioService _audioService;

        public void Start()
        {
            _audioService = ServiceLocator.Get<IAudioService>();
        }

        public void ShowView()
        {
            Text.gameObject.SetActive(true);
        }

        public void HideView()
        {
            if (_printingCoroutine != null)
            {
                StopCoroutine(_printingCoroutine);
                _printingCoroutine = null;
            }

            Text.text = "";
            Text.gameObject.SetActive(false);
        }

        public void PlayLine(string line)
        {
            if (_printingCoroutine != null)
                StopCoroutine(_printingCoroutine);

            Text.text = "";
            _printingCoroutine = StartCoroutine(PrintCoroutine(line));
        }

        public void ForceCompleteLine()
        {
            _skipRequested = true;
        }

        private bool _skipRequested = false;

        private IEnumerator PrintCoroutine(string line)
        {
            var op = line;
            yield return op;

            string text = op;
            Text.text = "";
            _skipRequested = false;

            for (int i = 0; i < text.Length; i++)
            {
                if (_skipRequested)
                {
                    Text.text = text;
                    break;
                }

                Text.text += text[i];
                _audioService.Play(_keyAudio);
                yield return new WaitForSeconds(_charDelay);
            }

            _printingCoroutine = null;
            _skipRequested = false;

            if (_autoContinue)
            {
                yield return new WaitForSeconds(_autoContinueDelay);

                OnLineFullyShownEvent?.Invoke();
            }
        }
    }

}
