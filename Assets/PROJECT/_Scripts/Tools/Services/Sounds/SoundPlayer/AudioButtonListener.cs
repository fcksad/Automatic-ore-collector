using Service;
using UnityEngine;

public class AudioButtonListener : CustomButton
{
    [SerializeField] private AudioConfig _soundConfig;

    private IAudioService _audioService;

    private void Start()
    {
        _audioService = ServiceLocator.Get<IAudioService>();
        Button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        _audioService.Play(_soundConfig);
    }
}
