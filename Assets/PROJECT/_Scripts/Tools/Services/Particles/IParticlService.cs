using UnityEngine;

public interface IParticlService 
{

    void Create(ParticleSystem particlePrefab, Vector3 position, Quaternion rotation = default);
    void ClearAll();

}
