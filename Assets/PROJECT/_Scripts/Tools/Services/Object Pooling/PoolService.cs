using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public class PoolService : IPoolService, IInitializable
    {
        private Dictionary<string, Queue<GameObject>> _pool = new();
        private Dictionary<string, Transform> _holders = new();

        private Transform _root;

        public void Initialize()
        {
            if (_root == null)
            {
                CreatePoolRoot();
            }
        }

        public T GetFromPool<T>(T prefab, string key = null) where T : Component
        {
            if (key == null)
            {
                key = typeof(T).Name;
            }

            if (!_pool.ContainsKey(key))
                _pool[key] = new Queue<GameObject>();

            if (!_holders.ContainsKey(key))
            {
                var holder = new GameObject($"[Pool] {key}").transform;
                if (_root == null)
                {
                    CreatePoolRoot();
                }

                holder.SetParent(_root);
                _holders[key] = holder;
            }

            GameObject obj;

            if (_pool[key].Count > 0)
            {
                obj = _pool[key].Dequeue();
            }
            else
            {
                obj = UnityEngine.Object.Instantiate(prefab.gameObject);
            }

            return obj.GetComponent<T>();
        }

        public void ReturnToPool<T>(T instance, string key = null) where T : Component
        {
            if (!instance.gameObject.activeInHierarchy) return;

            if (key == null)
            {
                key = typeof(T).Name;
            }

            if (!_pool.ContainsKey(key))
                _pool[key] = new Queue<GameObject>();

            if (!_holders.ContainsKey(key))
            {
                var holder = new GameObject($"[Pool] {key}").transform;
                holder.SetParent(_root);
                _holders[key] = holder;
            }

            if (instance is IPoolable poolable)
                poolable.OnDespawn();

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_holders[key]);
            _pool[key].Enqueue(instance.gameObject);
        }

        public void ReleaseAll()
        {
            foreach (var queue in _pool.Values)
            {
                while (queue.Count > 0)
                {
                    var obj = queue.Dequeue();
                    Object.Destroy(obj);
                }
            }

            _pool.Clear();

            foreach (var holder in _holders.Values)
                Object.Destroy(holder.gameObject);

            _holders.Clear();
        }

        private void CreatePoolRoot()
        {
            var go = new GameObject("[PoolService]");
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
        }
    }

}
