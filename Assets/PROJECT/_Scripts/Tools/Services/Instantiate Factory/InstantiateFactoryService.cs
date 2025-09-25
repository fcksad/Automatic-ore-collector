using UnityEngine;

namespace Service
{
    public class InstantiateFactoryService : IInstantiateFactoryService
    {
        private GameObject _container;
        private IPoolService _poolService;

        public InstantiateFactoryService(IPoolService poolService)
        {
            _poolService = poolService;
            _container = ServiceLocator.Container;
        }

        /// <summary>
        /// Универсальный метод создания экземпляра префаба.
        /// </summary>
        /// <typeparam name="T">Тип MonoBehaviour на префабе</typeparam>
        /// <param name="prefab">Префаб с компонентом типа T</param>
        /// <param name="parent">Родитель в иерархии (может быть null)</param>
        /// <param name="position">Мировая позиция (по умолчанию Vector3.zero)</param>
        /// <param name="rotation">Поворот (по умолчанию Quaternion.identity)</param>
        /// <param name="customName">Установить имя GameObject (опционально)</param>
        public T Create<T>(T prefab, Transform parent = null, Vector3? position = null, Quaternion? rotation = null, string customName = null, string key = null) where T : MonoBehaviour
        {
            T instance;

            instance = _poolService.GetFromPool(prefab, key);

            var obj = instance.gameObject;

            obj.transform.SetParent(parent, false);
            obj.transform.position = position ?? Vector3.zero;
            obj.transform.rotation = rotation ?? Quaternion.identity;

            if (!string.IsNullOrEmpty(customName))
                obj.name = customName;

            obj.SetActive(true);
            return instance;
        }

        public void Release<T>(T instance, string key = null) where T : MonoBehaviour
        {
            _poolService.ReturnToPool(instance, key);
        }

        public void ReleaseAll() => _poolService.ReleaseAll();
    }

}

