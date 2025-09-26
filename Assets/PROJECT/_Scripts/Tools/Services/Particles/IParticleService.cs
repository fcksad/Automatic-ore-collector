using UnityEngine;

public interface IParticleService 
{

    ParticleController Play(ParticleController prefab, Transform parent, Vector3 position, Quaternion rotation = default);
    void ClearAll();

}
