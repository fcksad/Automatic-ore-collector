using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public class ParticlService : IParticlService, IInitializable
    {
        private Transform _parent;
        private List<GameObject> _activeParticles = new();

        public void Initialize()
        {
            var container = new GameObject("ParticlService");
            Object.DontDestroyOnLoad(container);
            _parent = container.transform;
        }
        public void Create(ParticleSystem particlePrefab, Vector3 position, Quaternion rotation = default)
        {
            if (particlePrefab == null)
            {
                Debug.LogWarning("[ParticleService] Particle prefab is null.");
                return;
            }

            ParticleSystem instance = Object.Instantiate(particlePrefab, position, rotation, _parent);
            instance.Play();

            GameObject instanceObj = instance.gameObject;
            _activeParticles.Add(instanceObj);

            float lifeTime = instance.main.duration + instance.main.startLifetime.constantMax;

            Timer(lifeTime, instanceObj);
        }

        private async void Timer(float delay, GameObject obj)
        {
            await System.Threading.Tasks.Task.Delay((int)(delay * 1000));

            if (obj != null)
            {
                _activeParticles.Remove(obj);
                Object.Destroy(obj);
            }
        }

        public void ClearAll()
        {
            foreach (var particle in _activeParticles)
            {
                if (particle != null)
                    Object.Destroy(particle);
            }

            _activeParticles.Clear();
        }
    }

}

