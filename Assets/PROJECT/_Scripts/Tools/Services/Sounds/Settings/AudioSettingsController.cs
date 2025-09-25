using Service;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Settings
{
    [System.Serializable]
    public class AudioSettings
    {
        [field: SerializeField] public AudioType Type { get; private set; }
        [field: SerializeField] public Slider Slider { get; private set; }
    }

    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField] private List<AudioSettings> _audioSettings;

        private IAudioService _audioService;

        private void Awake()
        {
            _audioService = ServiceLocator.Get<IAudioService>();

            foreach (var audioSettings in _audioSettings)
            {
                audioSettings.Slider.value = _audioService.GetVolume(audioSettings.Type);
                audioSettings.Slider.onValueChanged.AddListener((value) => SetVolume(audioSettings.Type, value));
            }
        }

        private void SetVolume(AudioType type, float value)
        {
            _audioService.SetVolume(type, value);
        }

        private void OnDestroy()
        {
            foreach (var setting in _audioSettings)
            {
                setting.Slider.onValueChanged.RemoveAllListeners();
            }
        }
    }
}

