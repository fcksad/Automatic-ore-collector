using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public class ParticleService : IParticleService, IInitializable
    {
        private Transform _root;
        private readonly List<ParticleController> _active = new();
        private readonly IInstantiateFactoryService _factory;

        public ParticleService(IInstantiateFactoryService factory) => _factory = factory;

        public void Initialize()
        {
            var go = new GameObject("ParticleService");
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
        }

        public ParticleController Play(ParticleController prefab, Transform parent, Vector3 position, Quaternion rotation = default)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ParticleService] Prefab is null");
                return null;
            }

            var p = parent ? parent : _root;

            var instance = _factory.Create(prefab, position: position, rotation: rotation, parent: p);
            if (instance == null)
            {
                Debug.LogWarning("[ParticleService] Create() returned null");
                return null;
            }

            _active.Add(instance);
            instance.Play(onFinished: OnControllerFinished);
            return instance;
        }

        private void OnControllerFinished(ParticleController controller)
        {
            if (controller == null) return;
            _active.Remove(controller);
            _factory.Release(controller);
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (c == null) continue;
                c.StopImmediate();
                Object.Destroy(c.gameObject);
            }
            _active.Clear();
        }
    }

}

