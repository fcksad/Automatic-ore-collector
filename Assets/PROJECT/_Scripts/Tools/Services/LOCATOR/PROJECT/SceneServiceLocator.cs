using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Service
{
    [DefaultExecutionOrder(-700)]
    public class SceneServiceLocator : MonoBehaviour
    {
        private static readonly List<SceneServiceLocator> _instances = new();

        public Transform Container => transform;

        private readonly Dictionary<Type, object> _map = new();
        private readonly HashSet<Type> _constructionStack = new();

        #region Lifecycle

        public static SceneServiceLocator Current
        {
            get
            {
                var active = _instances.FirstOrDefault(x => x.gameObject.scene == SceneManager.GetActiveScene());
                if (active != null) return active;

                if (_instances.Count > 0) return _instances[0];

                var go = new GameObject("[SceneServiceLocator]");
                var created = go.AddComponent<SceneServiceLocator>();
                return created;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExistsForActiveScene()
        {
            _ = Current;
        }


        private void OnEnable()
        {
            if (!_instances.Contains(this)) _instances.Add(this);
        }

        private void OnDisable()
        {
            _instances.Remove(this);
        }

        private void OnDestroy()
        {
            // Dispose всех не-MB сценовых сервисов
            foreach (var obj in _map.Values) SafeDispose(obj);
            _map.Clear();
        }

        #endregion

        #region Static helpers

        /// <summary> Найти ближайший SceneServiceLocator по иерархии от контекста. </summary>
        public static SceneServiceLocator For(Component context)
        {
            if (context == null)
                return _instances.Count > 0 ? _instances[0] : null;

            var t = context.transform;
            while (t != null)
            {
                var local = t.GetComponent<SceneServiceLocator>();
                if (local != null) return local;
                t = t.parent;
            }

            // если у контекста нет своего — вернём первый активный
            return _instances.Count > 0 ? _instances[0] : null;
        }

        /// <summary> Утилита: резолвит из ближайшего сценового локатора или глобального. </summary>
        public static T Resolve<T>(Component context)
        {
            var local = For(context);
            if (local != null && local.TryGet(out T value)) return value;
            return ServiceLocator.Get<T>();
        }

        #endregion

        #region Public API (Scene-scope)

        /// <summary> Интерфейс → реализация (оба НЕ MonoBehaviour), конструкторная инъекция. </summary>
        public void BindWithInterface<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            var key = typeof(TInterface);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"[Scene] Service already bound: {key.Name}");

            var instance = (TInterface)CreateWithInjection(typeof(TImplementation));
            _map[key] = instance;

            if (instance is IInitializable init) init.Initialize();
        }

        /// <summary> Просто класс (НЕ MonoBehaviour), конструкторная инъекция. </summary>
        public void BindComponent<T>() where T : class
        {
            var key = typeof(T);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"[Scene] Service already bound: {key.Name}");

            var instance = (T)CreateWithInjection(typeof(T));
            _map[key] = instance;

            if (instance is IInitializable init) init.Initialize();
        }

        /// <summary> Компонент (MonoBehaviour) из детей контейнера (глубокий поиск). </summary>
        public T BindFromChildren<T>(bool includeInactive = true) where T : Component
        {
            var comp = Container.GetComponentInChildren<T>(includeInactive);
            if (comp == null)
                throw new InvalidOperationException($"[Scene] {typeof(T).Name} not found under '{Container.name}' (deep search).");

            var key = typeof(T);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"[Scene] Service already bound: {key.Name}");

            _map[key] = comp;
            if (comp is IInitializable init) init.Initialize();
            return comp;
        }

        public void BindInstance<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var key = typeof(T);
            if (_map.ContainsKey(key)) throw new InvalidOperationException($"[Scene] Service already bound: {key.Name}");
            _map[key] = instance;
            if (instance is IInitializable init) init.Initialize();
        }

        public T BindFromScene<T>(bool includeInactive = true) where T : Component
        {
            // 1) сначала пробуем под контейнером (как раньше)
            var comp = Container ? Container.GetComponentInChildren<T>(includeInactive) : null;

#if UNITY_2023_1_OR_NEWER
            // 2) если не нашли — ищем по всей сцене
            if (comp == null) comp = FindFirstObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
    if (comp == null) comp = (T)UnityEngine.Object.FindObjectOfType(typeof(T), includeInactive);
#endif

            if (comp == null)
                throw new InvalidOperationException($"[Scene] {typeof(T).Name} not found anywhere in scene (searched under '{Container?.name ?? "<null>"}' and globally).");

            var key = typeof(T);
            if (_map.ContainsKey(key))
                throw new InvalidOperationException($"[Scene] Service already bound: {key.Name}");

            _map[key] = comp;
            if (comp is IInitializable init) init.Initialize();
            return comp;
        }

        public T Get<T>()
        {
            if (_map.TryGetValue(typeof(T), out var obj))
                return (T)obj;

            // падение в глобальный – удобнее, чем сразу исключение
            return ServiceLocator.Get<T>();
        }

        public bool TryGet<T>(out T service)
        {
            if (_map.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            if (ServiceLocator.TryGet(out service))
                return true;

            service = default;
            return false;
        }

        public bool Unbind<T>()
        {
            var key = typeof(T);
            if (_map.TryGetValue(key, out var obj))
            {
                SafeDispose(obj);
                return _map.Remove(key);
            }
            return false;
        }

        public void ClearScene()
        {
            foreach (var obj in _map.Values) SafeDispose(obj);
            _map.Clear();
        }

        #endregion

        #region DI factory (scene-first, then global)

        private object CreateWithInjection(Type implType)
        {
            if (typeof(Component).IsAssignableFrom(implType))
                throw new InvalidOperationException($"Use BindFromChildren<T>() for MonoBehaviours. Type: {implType.Name}");

            if (_constructionStack.Contains(implType))
                throw new InvalidOperationException($"[Scene] Cyclic dependency detected while constructing {implType.Name}");

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
                        return ctor.Invoke(args);
                }

                var paramless = implType.GetConstructor(Type.EmptyTypes);
                if (paramless != null) return Activator.CreateInstance(implType);

                throw new InvalidOperationException(
                    $"[Scene] No suitable constructor found for {implType.Name}. Make sure all constructor parameters are bound (scene/global) or available under Container.");
            }
            finally
            {
                _constructionStack.Remove(implType);
            }
        }

        private bool TryBuildParameters(ConstructorInfo ctor, out object[] args)
        {
            var ps = ctor.GetParameters();
            if (ps.Length == 0) { args = Array.Empty<object>(); return true; }

            var list = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var pType = ps[i].ParameterType;

                // 1) сценовый бинд?
                if (_map.TryGetValue(pType, out var local))
                {
                    list[i] = local;
                    continue;
                }

                // 2) глобальный бинд?
                if (ServiceLocator.TryGet(out object globalObj) && globalObj.GetType() == pType || ServiceLocator.TryGet(out globalObj) && pType.IsInstanceOfType(globalObj))
                {
                    // небольшой хак: TryGet<object> нельзя напрямую — сделаем generic через рефлексию
                }

                if (TryGetFromGlobal(pType, out var global))
                {
                    list[i] = global;
                    continue;
                }

                // 3) Component — ищем в сценовом контейнере, затем в глобальном контейнере, затем по сцене
                if (typeof(Component).IsAssignableFrom(pType))
                {
                    Component found = null;

                    if (Container != null) found = (Component)Container.GetComponentInChildren(pType, true);

                    if (found == null && ServiceLocator.Container != null)
                        found = (Component)ServiceLocator.Container.GetComponentInChildren(pType, true);

#if UNITY_2023_1_OR_NEWER
                    if (found == null)
                        found = (Component)FindFirstObjectByType(pType, FindObjectsInactive.Include);
#else
                    if (found == null)
                        found = (Component)UnityEngine.Object.FindObjectOfType(pType, true);
#endif
                    if (found != null)
                    {
                        // кэшируем в СЦЕНОВУЮ карту (это сценовый ресурс)
                        _map[pType] = found;
                        if (found is IInitializable initC) initC.Initialize();
                        list[i] = found;
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"[Scene] Cannot resolve parameter '{pType.Name}' (Component) for {ctor.DeclaringType!.Name}. " +
                        $"Place it under scene Container '{Container?.name}' or bind it explicitly.");
                }

                // 4) Конкретный класс (не интерфейс/не абстракция/не MB) — создаём автоматически в сценовом скоупе
                if (!pType.IsInterface && !pType.IsAbstract && !typeof(Component).IsAssignableFrom(pType))
                {
                    var created = CreateWithInjection(pType);   // рекурсивно
                    _map[pType] = created;
                    if (created is IInitializable initS) initS.Initialize();
                    list[i] = created;
                    continue;
                }

                // 5) Интерфейс/абстракция не найдены ни в сцене, ни в глобале
                throw new InvalidOperationException(
                    $"[Scene] Unbound dependency '{pType.Name}' (interface/abstract) required by {ctor.DeclaringType!.Name}. " +
                    $"Bind it in SceneServiceLocator or ServiceLocator before binding {ctor.DeclaringType!.Name}.");
            }

            args = list;
            return true;
        }

        private static bool TryGetFromGlobal(Type t, out object obj)
        {
            // аккуратно вытаскиваем из глобального локатора объект данного типа, если он есть
            if (ServiceLocator.TryGet(out obj))
            {
                // выше TryGet<object> не вызвать, поэтому берём из мапы через рефлексию
            }

            // прямого API у тебя нет — сделаем проход по внутренней карте через резолв по типу
            try
            {
                var mi = typeof(ServiceLocator).GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                var generic = mi.MakeGenericMethod(t);
                obj = generic.Invoke(null, null);
                return true;
            }
            catch
            {
                obj = null;
                return false;
            }
        }

        private static void SafeDispose(object obj)
        {
            if (obj is Component) return;
            if (obj is IDisposable d)
            {
                try { d.Dispose(); }
                catch (Exception e) { Debug.LogError($"[Scene] Dispose failed for {obj.GetType().Name}: {e}"); }
            }
        }

        #endregion
    }
}
