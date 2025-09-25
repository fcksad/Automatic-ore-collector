using UnityEngine;

namespace Service
{
    public interface IInstantiateFactoryService
    {  /// <summary>
       /// Универсальный метод создания экземпляра префаба.
       /// </summary>
       /// <typeparam name="T">Тип MonoBehaviour на префабе</typeparam>
       /// <param name="prefab">Префаб с компонентом типа T</param>
       /// <param name="parent">Родитель в иерархии (может быть null)</param>
       /// <param name="position">Мировая позиция (по умолчанию Vector3.zero)</param>
       /// <param name="rotation">Поворот (по умолчанию Quaternion.identity)</param>
       /// <param name="customName">Установить имя GameObject (опционально)</param>
        T Create<T>(T prefab, Transform parent = null, Vector3? position = null, Quaternion? rotation = null, string customName = null, string key = null) where T : MonoBehaviour;

        void Release<T>(T instance, string key = null) where T : MonoBehaviour;
        void ReleaseAll();

    }

}

