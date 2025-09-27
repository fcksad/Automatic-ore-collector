using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioEmitter : MonoBehaviour
{
    [field: SerializeField]public AudioSource Source { get; private set; }
    private Action<AudioEmitter> _onFinished;
    private bool _loop;
    private float _stopAt = float.PositiveInfinity;

    public void Configure(AudioClip clip, float volume, float pitch, float spatialBlend, float minDist, float maxDist, bool loop, AudioMixerGroup mixer = null)
    {
        Source.clip = clip;
        Source.volume = volume;
        Source.pitch = pitch;
        Source.spatialBlend = spatialBlend;  
        Source.minDistance = minDist;
        Source.maxDistance = maxDist;
        Source.loop = loop;
        if (mixer) Source.outputAudioMixerGroup = mixer;

        _loop = loop;
        _stopAt = float.PositiveInfinity;
    }

    public void Play(float fadeIn = 0f, Action<AudioEmitter> onFinished = null)
    {
        _onFinished = onFinished;
        if (fadeIn > 0f)
        {
            float tgt = Source.volume;
            Source.volume = 0f;
            Source.Play();
            Service.Tweener.TW.To(() => Source.volume, v => Source.volume = v, tgt, fadeIn);
        }
        else Source.Play();

        if (!_loop)
            _stopAt = Time.unscaledTime + (Source.clip.length / Mathf.Max(0.01f, Source.pitch)) + 0.05f;
    }

    public void Stop(float fadeOut = 0f)
    {
        if (fadeOut > 0f)
        {
            Service.Tweener.TW.To(() => Source.volume, v => Source.volume = v, 0f, fadeOut)
                .OnComplete(() => { Source.Stop(); _onFinished?.Invoke(this); });
        }
        else
        {
            Source.Stop();
            _onFinished?.Invoke(this);
        }
    }

    private void Update()
    {
        if (!_loop && (Time.unscaledTime >= _stopAt || !Source.isPlaying))
        {
            _onFinished?.Invoke(this);
            _onFinished = null;
        }
    }

    private void OnDisable()
    {
        _onFinished = null;
    }
}
