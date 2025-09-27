using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Service
{
    public class AudioService : IAudioService, IInitializable
    {
        private readonly Dictionary<AudioType, float> _volumes = new();
        private readonly Dictionary<(AudioType, string), List<AudioEmitter>> _active = new();
        private readonly Dictionary<AudioType, AudioSource> _oneShot2D = new();

        private GameObject _audioRoot;
        private AudioEmitter _audioEmitterPrefab;

        private ISaveService _saveService;
        private IInstantiateFactoryService _instantiateFactoryService;

        public AudioService(ISaveService saveService, IInstantiateFactoryService instantiateFactoryService)
        {
            _saveService = saveService;
            _instantiateFactoryService = instantiateFactoryService;
        }

        public void Initialize()
        {
            _audioEmitterPrefab = ResourceLoader.Get<AudioEmitter>("Components/Audio/AudioEmitter");

            _audioRoot = new GameObject("[AudioService]");
            UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

            foreach (AudioType type in Enum.GetValues(typeof(AudioType)))
            {
                float vol = _saveService.SettingsData.AudioData.SoundVolumes.TryGetValue(type.ToString(), out float loaded)
                    ? loaded : 0.5f;
                _volumes[type] = vol;

                var go = new GameObject($"{type}_OneShot2D");
                go.transform.SetParent(_audioRoot.transform, false);

                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.loop = false;
                src.volume = vol;

                _oneShot2D[type] = src;
            }
        }

        public void Play(AudioConfig config,Transform parent = null, Vector3? position = null, int clipIndex = -1, float fadeIn = 0f, Action onFinished = null)
        {
            if (config == null || config.AudioClips == null || config.AudioClips.Count == 0) return;

            if (config.Spatial == SpatialMode.TwoD)
            {
                if (config.OneShoot) Play2DOneShot(config, clipIndex, onFinished);
                else Play2DLoop(config, clipIndex, fadeIn, onFinished);
            }
            else 
            {
                Play3D(config, parent, position, loop: config.Loop && !config.OneShoot, clipIndex: clipIndex, fadeIn: fadeIn, onFinished: onFinished);
            }
        }

        public void Stop(AudioConfig config, float fadeOut = 0f)
        {
            if (config == null) return;
            var key = (config.Type, config.AudioName ?? string.Empty);
            if (!_active.TryGetValue(key, out var list)) return;

            foreach (var emitter in list.ToArray())
            {
                if (!emitter) { list.Remove(emitter); continue; }
                emitter.Stop(fadeOut);
            }

            if (list.Count == 0) _active.Remove(key);
        }

        public void Pause(AudioConfig config)
        {
            if (config == null) return;
            var key = (config.Type, config.AudioName ?? string.Empty);
            if (!_active.TryGetValue(key, out var list)) return;
            foreach (var emitter in list) if (emitter && emitter.Source) emitter.Source.Pause();
        }

        public void Resume(AudioConfig config)
        {
            if (config == null) return;
            var key = (config.Type, config.AudioName ?? string.Empty);
            if (!_active.TryGetValue(key, out var list)) return;
            foreach (var emitter in list) if (emitter && emitter.Source) emitter.Source.UnPause();
        }

        public void Play2DOneShot(AudioConfig config, int clipIndex = -1, Action onFinished = null)
        {
            var src = _oneShot2D[config.Type];
            if (!src) return;

            var clip = SelectClip(config, clipIndex);
            src.pitch = UnityEngine.Random.Range(config.MinPitch, config.MaxPitch);
            src.PlayOneShot(clip, _volumes[config.Type]);

            if(onFinished != null)
    {
                // длина с учётом pitch (+ небольшой запас)
                var ms = (int)((clip.length / Mathf.Max(0.01f, src.pitch)) * 1000f) + 50;
                FireAfter(ms, onFinished);
            }
        }

        public void Play2DLoop(AudioConfig config, int clipIndex = -1, float fadeIn = 0f, Action onFinished = null)
        {
            Play3D(config, parent: _audioRoot.transform, position: Vector3.zero,loop: true, clipIndex: clipIndex, fadeIn: fadeIn, force2D: true, onFinished: onFinished);
        }

        public AudioEmitter Play3D( AudioConfig config, Transform parent = null, Vector3? position = null, bool loop = false, int clipIndex = -1, float fadeIn = 0f,  bool force2D = false, Action onFinished = null)
        {
            if (_audioEmitterPrefab == null) return null;

            var key = (config.Type, config.AudioName ?? string.Empty);
            var list = GetList(key);


            var emitter = _instantiateFactoryService.Create(_audioEmitterPrefab, parent: parent ? parent : _audioRoot.transform,position: position ?? (parent ? parent.position : Vector3.zero), rotation: Quaternion.identity, customName: $"{config.Type}_{config.AudioName}"/*,key: $"{config.Type}:{config.AudioName}"*/);

            if (!emitter || emitter.Source == null) return emitter;

            var clip = SelectClip(config, clipIndex);
            var vol = _volumes[config.Type];

            emitter.Configure(clip: clip, volume: vol,pitch: UnityEngine.Random.Range(config.MinPitch, config.MaxPitch),spatialBlend: force2D ? 0f : Mathf.Clamp01(config.SpatialBlend),
                minDist: config.MinDistance,maxDist: config.MaxDistance,loop: loop,mixer: null );

            emitter.Play(fadeIn, e =>
            {
                try { onFinished?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
                OnEmitterFinished(e);
            });

            list.Add(emitter);
            return emitter;
        }


        public void SetVolume(AudioType type, float value)
        {
            _volumes[type] = value;
            _saveService.SettingsData.AudioData.SoundVolumes[type.ToString()] = value;

            if (_oneShot2D.TryGetValue(type, out var bus) && bus) bus.volume = value;

            foreach (var active in _active)
            {
                if (active.Key.Item1 != type) continue;
                active.Value.RemoveAll(e => e == null);
                foreach (var e in active.Value) if (e && e.Source) e.Source.volume = value;
            }
        }

        public float GetVolume(AudioType type) => _volumes.TryGetValue(type, out float volume) ? volume : 0.5f;

        private void OnEmitterFinished(AudioEmitter emitter)
        {
            if (!emitter) return;

            foreach (var value in _active.ToArray())
            {
                var list = value.Value;
                if (list.Remove(emitter) && list.Count == 0) _active.Remove(value.Key);
            }

            _instantiateFactoryService.Release(emitter);
        }

        private AudioClip SelectClip(AudioConfig config, int clipIndex)
        {
            if (config.AudioClips == null || config.AudioClips.Count == 0) return null;
            if (clipIndex >= 0 && clipIndex < config.AudioClips.Count) return config.AudioClips[clipIndex];
            return config.AudioClips[UnityEngine.Random.Range(0, config.AudioClips.Count)];
        }

        private List<AudioEmitter> GetList((AudioType, string) key)
        {
            if (!_active.TryGetValue(key, out var list))
            {
                list = new List<AudioEmitter>(4);
                _active[key] = list;
            }
            return list;
        }

        private async void FireAfter(int milliseconds, Action cb)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(milliseconds);
                cb?.Invoke();
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}

