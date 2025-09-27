using Service;
using UnityEngine;

public class AudioLoopAwakeTrigger : MonoBehaviour
{
    [SerializeField] private AudioConfig _audioConfig;

    private IAudioService _audioService;

    private void Start()
    {
        _audioService = ServiceLocator.Get<IAudioService>();
        PlayLoop();
    }

    private void PlayLoop()
    {
        _audioService.Play(_audioConfig);
    }

    private void OnDestroy()
    {
        _audioService.Stop(_audioConfig);
    }
}
