using Service.Tweener;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Service
{
    public class PopupService : IPopupService, IInitializable
    {
        private List<PopupElement> _activePopups = new List<PopupElement>();

        private PopupView _popupView;
        private IAudioService _audioService;
        private ISceneService _sceneSerivce;
        private IInstantiateFactoryService _instantiateFactoryService;

        public PopupService(IAudioService audioService, PopupView popupView, ISceneService sceneSerivce, IInstantiateFactoryService instantiateFactoryService)
        {
            _audioService = audioService;
            _popupView = popupView;
            _sceneSerivce = sceneSerivce;
            _instantiateFactoryService = instantiateFactoryService;
        }

        public void Initialize()
        {
            _sceneSerivce.OnSceneUnloadEvent += ClearAll;
        }

        public void Show(Sprite sprite = null, string text = null, Vector3? position = null, float size = 1, float duration = 1.5f, float delay = 1f, float moveSpeed = 1f) //todo for text => have rect transform is problem :(
        {
            PopupElement prefab = sprite != null ? _popupView.ImagePrefab : _popupView.TextPrefab;

            PopupElement popupInstance = _instantiateFactoryService.Create(prefab, _popupView.Parent, position, Quaternion.identity);
            _activePopups.Add(popupInstance);

            if (sprite != null)
            {
                popupInstance.SpriteRenderer.sprite = sprite;
                popupInstance.transform.localScale = Vector3.one * size;
            }
            else if (text != null)
            {
                popupInstance.Text.SetText(text);
                popupInstance.Text.fontSize = size;
                if (position.HasValue)
                    popupInstance.TextRect.anchoredPosition3D = position.Value;
                else
                    popupInstance.TextRect.anchoredPosition3D = Vector3.zero;
            }

            _audioService.Play(_popupView.PopupAudio);
            MoveAndFade(popupInstance, duration, delay, moveSpeed);
        }

        public void ClearAll()
        {
            _activePopups.RemoveAll(p => p == null);

            foreach (var popup in _activePopups)
            {
                _instantiateFactoryService.Release(popup);
            }
            _activePopups.Clear();
        }

        private void MoveAndFade(PopupElement popup, float duration, float delay, float moveSpeed)
        {
            float totalLifetime = duration + delay;
            float moveDistance = moveSpeed * totalLifetime;
            Vector3 startPos = popup.transform.position;
            Vector3 targetPos = new(startPos.x, startPos.y + moveDistance, startPos.z);

            ITweener moveTween = popup.transform
                .MoveTo(targetPos, totalLifetime)
                .SetEase(Ease.OutQuad);

            ITweener fadeTween = null;

            var canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup == null && popup.GetComponentInChildren<TextMeshProUGUI>() != null)
                canvasGroup = popup.gameObject.AddComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                fadeTween = TW.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0f, duration)
                              .SetDelay(delay)
                              .SetEase(Ease.OutQuad);
            }
            else
            {
                var sr = popup.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var start = sr.color;
                    var end = new Color(start.r, start.g, start.b, 0f);
                    fadeTween = TW.To(() => sr.color, c => sr.color = c, end, duration)
                                  .SetDelay(delay)
                                  .SetEase(Ease.OutQuad);
                }
            }
        }
    }
}
