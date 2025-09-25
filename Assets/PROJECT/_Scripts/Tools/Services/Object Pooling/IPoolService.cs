using UnityEngine;

public interface IPoolService
{
    T GetFromPool<T>(T prefab, string key = null) where T : Component;
    void ReturnToPool<T>(T instance, string key = null) where T : Component;
    void ReleaseAll();
}
