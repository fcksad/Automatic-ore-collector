using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Service
{
    public class TooltipService : ITooltipService
    {
        private float _paddingSize = 5;
        private CancellationTokenSource _positionCall;

        private TooltipeView _view;

        public TooltipService(TooltipeView tooltipeView)
        {
            _view = tooltipeView;
        }

        public void Show(string text = null)
        {
            if (text != null)
                UpdateText(text);

            CancelPositionTask();

            _positionCall = new CancellationTokenSource();
            _ = UpdatePositionAsync(_positionCall.Token);
            _view.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _view.gameObject.SetActive(false);
            CancelPositionTask();
            UpdateText("");

        }

        public void UpdateText(string text)
        {
            _view.Text.SetText(text);
        }

        private void CancelPositionTask()
        {
            if (_positionCall != null)
            {
                _positionCall.Cancel();
                _positionCall.Dispose();
                _positionCall = null;
            }
        }

        private async Task UpdatePositionAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Vector2 tooltipPosition = Input.mousePosition;

                    Debug.LogError(tooltipPosition);

                    float tooltipWidth = _view.Rect.sizeDelta.x;
                    float tooltipHeight = _view.Rect.sizeDelta.y;

                    float maxX = Screen.width - tooltipWidth;
                    float maxY = Screen.height - tooltipHeight;

                    tooltipPosition.x = Mathf.Clamp(tooltipPosition.x, 0, maxX - _paddingSize);
                    tooltipPosition.y = Mathf.Clamp(tooltipPosition.y, 0, maxY - _paddingSize);

                    _view.Background.transform.position = tooltipPosition;

                    await Task.Yield();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

}

