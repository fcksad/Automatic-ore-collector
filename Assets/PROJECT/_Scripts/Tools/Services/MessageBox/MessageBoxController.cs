using Service.Tweener;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Service
{
    public enum MessageBoxType
    {
        Ok,
        YesNo,
    }

    public class MessageBoxController
    {
        private MessageBoxView _view;
        private UnityAction _onCancel;
        private UnityAction _onClose;
        private bool _isAccepted;
        private ITweener _autoCloseTween;
        private readonly List<ITweener> _activeTweens = new();

        public MessageBoxController(MessageBoxView view)
        {
            _view = view;
        }

        public void ShowOk(string message, UnityAction onOk = null, UnityAction onClose = null, float autoCloseDelay = -1f)
        {
            SetupUI(MessageBoxType.Ok, message);

            _onCancel = null;
            _onClose = onClose;
            _isAccepted = false;

            _view.OkButton.onClick.AddListener(() =>
            {
                _isAccepted = true;
                onOk?.Invoke();
                Close();
            });

            _view.Background.onClick.AddListener(Close);
            Open();

            if (autoCloseDelay > 0)
                StartAutoClose(autoCloseDelay);
        }

        public void ShowYesNo(string message, UnityAction onYes, UnityAction onCancel = null, UnityAction onClose = null, float autoCloseDelay = -1f)
        {
            SetupUI(MessageBoxType.YesNo, message);

            _onCancel = onCancel;
            _onClose = onClose;
            _isAccepted = false;

            _view.YesButton.onClick.AddListener(() =>
            {
                _isAccepted = true;
                onYes?.Invoke();
                Close();
            });

            _view.NoButton.onClick.AddListener(Close);
            _view.Background.onClick.AddListener(Close);
            Open();

            if (autoCloseDelay > 0)
                StartAutoClose(autoCloseDelay);
        }

        private void SetupUI(MessageBoxType type, string message)
        {
            CancelAutoClose();

            _view.YesButton.gameObject.SetActive(false);
            _view.NoButton.gameObject.SetActive(false);
            _view.OkButton.gameObject.SetActive(false);
            _view.Text.gameObject.SetActive(true);
            _view.Text.text = message;

            _view.YesButton.onClick.RemoveAllListeners();
            _view.NoButton.onClick.RemoveAllListeners();
            _view.OkButton.onClick.RemoveAllListeners();
            _view.Background.onClick.RemoveAllListeners();

            if (type == MessageBoxType.Ok)
            {
                _view.OkButton.gameObject.SetActive(true);
                _view.OkButton.interactable = true;
            }
            else if (type == MessageBoxType.YesNo)
            {
                _view.YesButton.gameObject.SetActive(true);
                _view.NoButton.gameObject.SetActive(true);
                _view.YesButton.interactable = true;
            }
        }

        private void Open()
        {
            _view.RectTransform.gameObject.SetActive(true);
            FadeGraphics(1f, 0.5f);
            FadeImageAlpha(_view.Background.GetComponent<Image>(), 0.5f, 0.5f);
        }

        private void Close()
        {
            CancelAutoClose();

            if (!_isAccepted)
                _onCancel?.Invoke();

            _onClose?.Invoke();

            _view.YesButton.onClick.RemoveAllListeners();
            _view.NoButton.onClick.RemoveAllListeners();
            _view.OkButton.onClick.RemoveAllListeners();
            _view.Background.onClick.RemoveAllListeners();

            FadeGraphics(0f, 0.5f);
            FadeImageAlpha(_view.Background.GetComponent<Image>(), 0f, 0.5f);

            _activeTweens.Add(
                TW.Delay(0.5f, () =>
                {
                    _view.RectTransform.gameObject.SetActive(false);
                    KillActiveTweens();
                })
            );
        }

        private void FadeGraphics(float endAlpha, float duration)
        {
            if (_view.FadeGrafic == null) return;

            foreach (var g in _view.FadeGrafic)
            {
                if (g == null) continue;

                var start = g.color;
                var to = new Color(start.r, start.g, start.b, endAlpha);

                var tw = TW.To(() => g.color, c => { if (g) g.color = c; }, to, duration)
                           .SetEase(Ease.OutQuad);
                _activeTweens.Add(tw);
            }
        }

        private void FadeImageAlpha(Image img, float endAlpha, float duration)
        {
            if (img == null) return;

            var c0 = img.color;
            var c1 = new Color(c0.r, c0.g, c0.b, endAlpha);

            var tw = TW.To(() => img.color, c => { if (img) img.color = c; }, c1, duration)
                       .SetEase(Ease.OutQuad);
            _activeTweens.Add(tw);
        }

        private void StartAutoClose(float delay)
        {
            _autoCloseTween?.Kill();
            _autoCloseTween = TW.Delay(delay, () =>
            {
                if (!_isAccepted)
                    Close();
            });
        }

        private void CancelAutoClose()
        {
            _autoCloseTween?.Kill();
            _autoCloseTween = null;
        }

        private void KillActiveTweens()
        {
            if (_activeTweens.Count == 0) return;
            for (int i = 0; i < _activeTweens.Count; i++)
                _activeTweens[i]?.Kill();
            _activeTweens.Clear();
        }
    }
}



