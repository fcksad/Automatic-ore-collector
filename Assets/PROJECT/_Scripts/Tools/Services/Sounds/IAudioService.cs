using UnityEngine;

public interface IAudioService
{
    AudioSource Play(AudioConfig audio, bool loop = false,int clipIndex = -1, Transform parent = null, Vector3? position = null, float fadeDuration = 0f, float minSoundDistance = 1, float maxSoundDistance = 100);
    void Stop(AudioConfig audio, float fadeDuration = 0f); 
    void Pause(AudioConfig audio);
    void Resume(AudioConfig audio);
    void SetVolume(AudioType type, float value);
    float GetVolume(AudioType type);
}
