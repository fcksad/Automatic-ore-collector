using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlaylist
{
    private readonly IAudioService _audio;
    private readonly AudioConfig _config;          
    private readonly List<int> _order;            
    private int _cursor = 0;
    private bool _isPlaying;

    public bool Shuffle { get; private set; }
    public bool Loop { get; set; } = true;         
    public float Crossfade { get; set; } = 0.5f;

    public MusicPlaylist(IAudioService audio, AudioConfig musicConfig, bool shuffle = true)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _config = musicConfig ?? throw new ArgumentNullException(nameof(musicConfig));
        if (_config.AudioClips == null || _config.AudioClips.Count == 0)
            throw new ArgumentException("AudioConfig must contain at least 1 clip.", nameof(musicConfig));

        _order = new List<int>(_config.AudioClips.Count);
        for (int i = 0; i < _config.AudioClips.Count; i++) _order.Add(i);

        SetShuffle(shuffle);
    }

    public void Play()
    {
        if (_isPlaying) return;
        _isPlaying = true;
        PlayCurrent();
    }

    public void Stop()
    {
        _isPlaying = false;
        _audio.Stop(_config, fadeOut: Crossfade);
    }

    public void Pause() => _audio.Pause(_config);
    public void Resume() => _audio.Resume(_config);

    public void Next()
    {
        if (_order.Count == 0) return;
        _cursor = (_cursor + 1) % _order.Count;
        if (_isPlaying) PlayCurrent();
    }

    public void Prev()
    {
        if (_order.Count == 0) return;
        _cursor = (_cursor - 1 + _order.Count) % _order.Count;
        if (_isPlaying) PlayCurrent();
    }

    public void SetShuffle(bool enabled)
    {
        Shuffle = enabled;
        if (!enabled)
        {
            _order.Clear();
            for (int i = 0; i < _config.AudioClips.Count; i++) _order.Add(i);
        }
        else
        {
            _order.Clear();
            for (int i = 0; i < _config.AudioClips.Count; i++) _order.Add(i);
            for (int i = _order.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }
        _cursor = 0;
    }

    public void SetVolume(float v) => _audio.SetVolume(_config.Type, Mathf.Clamp01(v));

    private void PlayCurrent()
    {
        if (_order.Count == 0) return;
        int clipIndex = _order[_cursor];

        _audio.Stop(_config, fadeOut: Crossfade);

        _audio.Play(_config,
            parent: null,
            position: Vector3.zero,
            clipIndex: clipIndex,
            fadeIn: Crossfade,
            onFinished: OnTrackFinished);
    }

    private void OnTrackFinished()
    {
        if (!_isPlaying) return;

        if (_cursor + 1 < _order.Count)
        {
            _cursor++;
            PlayCurrent();
        }
        else if (Loop)
        {
            _cursor = 0;
            PlayCurrent();
        }
        else
        {
            _isPlaying = false;
        }
    }
}
