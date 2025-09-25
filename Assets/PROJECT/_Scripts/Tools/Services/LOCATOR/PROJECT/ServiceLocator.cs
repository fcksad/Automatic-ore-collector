using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Service
{

    [DefaultExecutionOrder(-900)]
    public class ServiceLocator : MonoBehaviour
    {
        private static readonly Dictionary<Type, object> _map = new();
        private static readonly HashSet<Type> _constructionStack = new();

        private static ServiceLocator _instance;
        public static GameObject Container => _instance.gameObject;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstanceExists()
        {
            if (_instance != null) return;
            var locator = Resources.Load<ServiceLocator>("ServiceLocator");
            _instance = Instantiate(locator);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Bind

        public static void BindWithInterface<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
        {
            var key = typeof(TInterface);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"Service already bound: {key.Name}");

            var instance = (TInterface)CreateWithInjection(typeof(TImplementation));
            _map[key] = instance;

            if (instance is IInitializable init) init.Initialize();
        }

        public static void BindComponent<T>() where T : class
        {
            var key = typeof(T);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"Service already bound: {key.Name}");

            var instance = (T)CreateWithInjection(typeof(T));
            _map[key] = instance;

            if (instance is IInitializable init) init.Initialize();
        }

        public static T BindFromChildren<T>(bool includeInactive = true) where T : Component
        {
            var comp = Container.GetComponentInChildren<T>(includeInactive);
            if (comp == null)
                throw new InvalidOperationException($"{typeof(T).Name} not found under Container '{Container.name}' (deep search).");

            var key = typeof(T);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"Service already bound: {key.Name}");

            _map[key] = comp;

            if (comp is IInitializable init) init.Initialize();
            return comp;
        }

        #endregion


        #region Get

        public static T Get<T>()
        {
            if (_map.TryGetValue(typeof(T), out var obj))
                return (T)obj;

            throw new KeyNotFoundException($"Service not found: {typeof(T).Name}. Did you bind it?");
        }

        public static bool TryGet<T>(out T service)
        {
            if (_map.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = default;
            return false;
        }

        #endregion

        #region Unbind

        public static bool Unbind<T>()
        {
            var key = typeof(T);
            if (_map.TryGetValue(key, out var obj))
            {
                SafeDispose(obj);
                return _map.Remove(key);
            }
            return false;
        }

        public static void Clear()
        {
            foreach (var obj in _map.Values) SafeDispose(obj);
            _map.Clear();
        }

        private static void SafeDispose(object obj)
        {
            if (obj is Component) return;

            if (obj is IDisposable d)
            {
                try { d.Dispose(); }
                catch (Exception e) { Debug.LogError($"Dispose failed for {obj.GetType().Name}: {e}"); }
            }
        }

        #endregion

        #region DI factory


        private static object CreateWithInjection(Type implType)
        {
            if (typeof(Component).IsAssignableFrom(implType))
                throw new InvalidOperationException($"Use BindFromChildren<T>() for MonoBehaviours. Type: {implType.Name}");

            if (_constructionStack.Contains(implType))
                throw new InvalidOperationException($"Cyclic dependency detected while constructing {implType.Name}");

            _constructionStack.Add(implType);
            try
            {
                var ctors = implType
                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .OrderByDescending(c => c.GetParameters().Length)
                    .ToArray();

                if (ctors.Length == 0)
                {
                    var defaultCtor = implType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (defaultCtor != null) return defaultCtor.Invoke(null);

                    return Activator.CreateInstance(implType);
                }

                foreach (var ctor in ctors)
                {
                    if (TryBuildParameters(ctor, out var args))
                    {
                        return ctor.Invoke(args);
                    }
                }

                var paramless = implType.GetConstructor(Type.EmptyTypes);
                if (paramless != null) return Activator.CreateInstance(implType);

                throw new InvalidOperationException(
                    $"No suitable constructor found for {implType.Name}. " +
                    $"Make sure all constructor parameters are bound or available under Container.");
            }
            finally
            {
                _constructionStack.Remove(implType);
            }
        }

        private static bool TryBuildParameters(ConstructorInfo ctor, out object[] args)
        {
            var ps = ctor.GetParameters();
            if (ps.Length == 0) { args = Array.Empty<object>(); return true; }

            var list = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var pType = ps[i].ParameterType;

                if (_map.TryGetValue(pType, out var bound))
                {
                    list[i] = bound;
                    continue;
                }

                if (typeof(Component).IsAssignableFrom(pType))
                {
                    var found = Container != null
                        ? Container.GetComponentInChildren(pType, true)
                        : null;

                    if (found == null)
                    {
                        found = (Component)FindFirstObjectByType(pType, FindObjectsInactive.Include);
                    }

                    if (found != null)
                    {
                        _map[pType] = found; 
                        if (found is IInitializable init) init.Initialize();
                        list[i] = found;
                        continue;
                    }

                    args = null;
                    return false; 
                }


                if (!pType.IsInterface && !pType.IsAbstract && !typeof(Component).IsAssignableFrom(pType))
                {
                    var created = CreateWithInjection(pType);
                    _map[pType] = created; 
                    if (created is IInitializable init) init.Initialize();
                    list[i] = created;
                    continue;
                }

                args = null;
                return false;
            }

            args = list;
            return true;
        }
        #endregion
    }
}

