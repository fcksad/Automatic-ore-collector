using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public class SurfaceAudioService : ISurfaceAudioService, IInitializable, IDisposable
    {
        private List<SurfaceAudioConfig> _configs = new List<SurfaceAudioConfig>();
        private Dictionary<SourceType, Dictionary<Material, AudioConfig>> _surfaceMaterials = new();
        private Dictionary<SourceType, AudioConfig> _defaultConfigs = new();

        public void Initialize()
        {
            _configs = ResourceLoader.GetAll<SurfaceAudioConfig>();
            Setup();
        }

        public void Dispose()
        {
            _configs = null;
            _surfaceMaterials = null;
        }

        private void Setup()
        {
            foreach (var config in _configs)
            {
                if (!_surfaceMaterials.ContainsKey(config.SourceType))
                    _surfaceMaterials[config.SourceType] = new Dictionary<Material, AudioConfig>();

                var materialMap = _surfaceMaterials[config.SourceType];

                foreach (var surface in config.Surfaces)
                {
                    foreach (var material in surface.Materials)
                    {
                        if (material == null) continue;

                        if (!materialMap.ContainsKey(material))
                            materialMap.Add(material, surface.Audio);
                    }

                    if (surface.Type == SurfaceType.Default && !_defaultConfigs.ContainsKey(config.SourceType))
                    {
                        _defaultConfigs[config.SourceType] = surface.Audio;
                    }
                }
            }
        }

        public AudioConfig GetConfigByMaterial(Renderer renderer, SourceType sourceType)
        {
            if (renderer == null)
                return GetDefault(sourceType);

            var materials = renderer.sharedMaterials;

            if (_surfaceMaterials == null)
                Setup();

            if (_surfaceMaterials.TryGetValue(sourceType, out var materialMap))
            {
                foreach (var mat in materials)
                {
                    if (mat != null && materialMap.TryGetValue(mat, out var audio))
                    {
                        return audio;
                    }
                }
            }

            return GetDefault(sourceType);
        }

        private AudioConfig GetDefault(SourceType sourceType)
        {
            if (_defaultConfigs.TryGetValue(sourceType, out var defaultAudio))
                return defaultAudio;

            Debug.LogWarning($"[SurfaceAudioService] No default AudioConfig found for SourceType {sourceType}");
            return null;
        }
    }

}
