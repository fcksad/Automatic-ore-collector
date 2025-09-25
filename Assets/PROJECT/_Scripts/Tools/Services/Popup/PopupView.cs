using UnityEngine;


namespace Service
{
    public class PopupView : MonoBehaviour
    {
        [field: SerializeField] public Transform Parent;

        [SerializeField] private PopupElement _imagePrefab;
        [SerializeField] private PopupElement _textPrefab;
        [SerializeField] private AudioConfig _popupAudio;

        public PopupElement ImagePrefab => _imagePrefab;
        public PopupElement TextPrefab => _textPrefab;
        public AudioConfig PopupAudio => _popupAudio;

    }

}

