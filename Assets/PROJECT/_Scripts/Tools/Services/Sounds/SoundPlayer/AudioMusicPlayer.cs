using Service;
using System.Threading.Tasks;
using UnityEngine;


public class AudioMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioConfig _musicConfig;
    private bool _isPlaying = true;
    private IAudioService _audioService;

    public void Start()
    {
        _audioService = ServiceLocator.Get<IAudioService>();
        _ = PlayLoop();
    }

    private async Task PlayLoop()
    {
        while (_isPlaying)
        {
            var source = _audioService.Play(_musicConfig, loop: false, fadeDuration: 1);

            if (source == null || source.clip == null)
                break;

            float duration = source.clip.length;
            float timer = 0f;

            while (_isPlaying && timer < duration)
            {
                await Task.Delay(200);
                timer += 0.2f;
            }

            await Task.Delay(200);
        }
    }


    private void OnDestroy()
    {
        _isPlaying = false;
        if (_audioService != null && _musicConfig != null)
        {
            _audioService.Stop(_musicConfig, fadeDuration: 0.5f);
        }
    }
}
