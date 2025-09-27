using Service;
using UnityEngine;


public class AudioMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioConfig _musicConfig;

    private MusicPlaylist _musicPlaylist;
    private IAudioService _audioService;

   /* public void Start()
    {
        _audioService = ServiceLocator.Get<IAudioService>();

        _musicPlaylist = new MusicPlaylist(ServiceLocator.Get<IAudioService>(), _musicConfig, shuffle: true);
        _musicPlaylist.Crossfade = 0.8f;
        _musicPlaylist.Play();

        *//*        // управление:
                _musicPlaylist.Next();
                _musicPlaylist.Pause();
                _musicPlaylist.Resume();
                _musicPlaylist.SetShuffle(false);
                _musicPlaylist.SetVolume(0.7f);
                _musicPlaylist.Stop();*//*
    }

    private void OnDestroy()
    {
        _musicPlaylist?.Stop();
    }*/
}
