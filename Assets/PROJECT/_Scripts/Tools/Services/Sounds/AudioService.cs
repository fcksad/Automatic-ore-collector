using Service.Tweener;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Service
{
    public class AudioService : IAudioService, IInitializable
    {
        private readonly Dictionary<AudioType, float> _volumes = new();
        private readonly Dictionary<(AudioType, string), List<AudioSource>> _namedSources = new();
        private readonly Dictionary<AudioType, AudioSource> _oneShootSources = new();

        private GameObject _audioRoot;
        private ISaveService _saveService;

        public AudioService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void Initialize()
        {
            _audioRoot = new GameObject("[AudioService]");
            UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

            foreach (AudioType type in Enum.GetValues(typeof(AudioType)))
            {
                float volume = _saveService.SettingsData.AudioData.SoundVolumes.TryGetValue(type.ToString(), out float loadedVolume) ? loadedVolume : 0.5f;
                _volumes[type] = volume;

                if (type != AudioType.SFX)
                {
                    var src = CreateAudioSource(type.ToString() + "OneShoot");
                    src.volume = volume;
                    _oneShootSources[type] = src;
                }
            }
        }

        public AudioSource Play(AudioConfig audio, bool loop = false, int clipIndex = -1, Transform parent = null, Vector3? position = null, float fadeDuration = 0, float minSoundDistance = 1, float maxSoundDistance = 100)
        {
            if (audio.OneShoot == true)
            {
                var source = _oneShootSources[audio.Type];
                source.pitch = UnityEngine.Random.Range(audio.MinPitch, audio.MaxPitch);
                source.PlayOneShot(GetRandomClip(audio.AudioClips));
                return source;
            }

            var go = new GameObject($"{audio.Type}_{audio.AudioName}");
            go.transform.SetParent(parent ? parent : _audioRoot.transform, worldPositionStays: false);
            if (position.HasValue) go.transform.position = position.Value;

            var src = go.AddComponent<AudioSource>();
            SetupSource(src, audio, loop, clipIndex, minSoundDistance, maxSoundDistance);
            src.Play();
            if (fadeDuration > 0f)
            {
                float target = _volumes[audio.Type];
                src.volume = 0f;
                TW.To(() => src.volume, v => src.volume = v, target, fadeDuration)
                  .SetEase(Ease.InOutQuad);
            }

            var key = (audio.Type, audio.AudioName);
            if (!_namedSources.ContainsKey(key)) _namedSources[key] = new List<AudioSource>();
            _namedSources[key].Add(src);

            if (loop == false)
            {

                var playTime = src.clip.length + 0.5f;
                TimerRemove(src, key, playTime);
                UnityEngine.Object.Destroy(go, playTime);
            }

            return src;
        }

        public void Stop(AudioConfig audio, float fade = 0)
        {
            foreach (var target in _namedSources.Where(key => key.Key.Item1 == audio.Type && (string.IsNullOrEmpty(audio.AudioName) || key.Key.Item2 == audio.AudioName)).ToList())
            {
                foreach (var source in target.Value)
                {
                    if (source == null) continue;

                    if (fade > 0f)
                    {
                        TW.To(() => source.volume, v => source.volume = v, 0f, fade)
                          .SetEase(Ease.InOutQuad)
                          .OnComplete(() => { if (source) UnityEngine.Object.Destroy(source.gameObject); });
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(source.gameObject);
                    }
                }

                _namedSources.Remove(target.Key);
            }
        }

        public void Pause(AudioConfig audio)
        {
            foreach (var source in _namedSources)
            {
                if (source.Key.Item1 == audio.Type && (audio.AudioName == null || source.Key.Item2 == audio.AudioName))
                    source.Value.ForEach(s => s?.Pause());
            }
        }

        public void Resume(AudioConfig audio)
        {
            foreach (var source in _namedSources)
            {
                if (source.Key.Item1 == audio.Type && (audio.AudioName == null || source.Key.Item2 == audio.AudioName))
                    source.Value.ForEach(s => s?.UnPause());
            }
        }

        public void SetVolume(AudioType type, float value)
        {
            _volumes[type] = value;
            _saveService.SettingsData.AudioData.SoundVolumes[type.ToString()] = value;

            if (_oneShootSources.TryGetValue(type, out var oneShoot)) oneShoot.volume = value;

            foreach (var source in _namedSources)
            {
                if (source.Key.Item1 == type)
                {
                    source.Value.RemoveAll(named => named == null);
                    foreach (var named in source.Value)
                    {
                        if (named != null) named.volume = value;
                    }
                }
            }
        }

        public float GetVolume(AudioType type) => _volumes.TryGetValue(type, out float volume) ? volume : 0.5f;

        private void SetupSource(AudioSource src, AudioConfig audio, bool loop, int clipIndex = -1, float minSoundDistance = 1, float maxSoundDistance = 100)
        {
            src.loop = loop;
            src.playOnAwake = false;
            src.spatialBlend = audio.SpatialBlend;
            src.pitch = UnityEngine.Random.Range(audio.MinPitch, audio.MaxPitch);
            src.minDistance = minSoundDistance;
            src.maxDistance = maxSoundDistance;
            src.clip = clipIndex >= 0 ? audio.AudioClips[clipIndex] : GetRandomClip(audio.AudioClips);
            src.volume = _volumes[audio.Type];
        }

        private async void TimerRemove(AudioSource src, (AudioType, string) key, float delay)
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delay));
            if (_namedSources.TryGetValue(key, out var list))
            {
                list.Remove(src);
                if (list.Count == 0) _namedSources.Remove(key);
            }
        }

        private AudioSource CreateAudioSource(string name, Transform parent = null, Vector3? pos = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent != null ? parent : _audioRoot.transform);
            obj.transform.position = pos ?? Vector3.zero;

            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private AudioClip GetRandomClip(List<AudioClip> clips) => clips[UnityEngine.Random.Range(0, clips.Count)];

    }
}

