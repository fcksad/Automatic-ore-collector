using UnityEngine;


namespace Service
{
    public interface IPopupService
    {
        public void Show(Sprite sprite = null, string text = null, Vector3? position = null, float size = 1, float duration = 1.5f, float delay = 1f, float moveSpeed = 1f);
        public void ClearAll();
    }

}
