using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Service.Coroutines
{
    public interface IRoutine : IDisposable
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        event Action Completed;

        void Stop();
        void Pause();
        void Resume();
    }

    [DefaultExecutionOrder(-995)]
    internal sealed class CoroutineRunner : MonoBehaviour { }

    public sealed class CoroutineService : ICoroutineService
    {
        private readonly List<Routine> _routines = new();
        private readonly List<Action<float>> _updateSubs = new();

        private GameObject _go;
        private CoroutineRunner _runner;

        private bool _disposed;

        public void Initialize()
        {
            if (_runner != null) return;

            _go = new GameObject("[CoroutineService]");
            UnityEngine.Object.DontDestroyOnLoad(_go);
            _runner = _go.AddComponent<CoroutineRunner>();
            _go.AddComponent<Driver>().Init(this);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            StopAll();
            if (_go != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_go);
                else UnityEngine.Object.DestroyImmediate(_go);
            }
            _go = null;
            _runner = null;
        }

        public IRoutine Start(IEnumerator enumerator)
        {
            EnsureRunner();
            var r = new Routine(this, enumerator);
            _routines.Add(r);
            r.Start(_runner);
            return r;
        }

        public IRoutine Start(Func<IEnumerator> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return Start(factory());
        }

        public IRoutine Delay(float seconds, Action callback)
        {
            return Start(DelayImpl(seconds, callback));
        }

        public IRoutine NextFrame(Action callback)
        {
            return Start(NextFrameImpl(callback));
        }

        public IRoutine Every(float intervalSeconds, Action tick, bool invokeImmediately = false)
        {
            return Start(EveryImpl(intervalSeconds, tick, invokeImmediately));
        }

        public void AddOnUpdate(Action<float> onUpdate)
        {
            if (onUpdate == null) return;
            if (!_updateSubs.Contains(onUpdate)) _updateSubs.Add(onUpdate);
        }

        public void RemoveOnUpdate(Action<float> onUpdate)
        {
            if (onUpdate == null) return;
            _updateSubs.Remove(onUpdate);
        }

        public void Stop(IRoutine routine)
        {
            if (routine is Routine r) r.Stop();
        }

        public void StopAll()
        {
            var copy = _routines.ToArray();
            foreach (var r in copy) r.Stop();
            _routines.Clear();
        }

        private void EnsureRunner()
        {
            if (_runner != null) return;
            Initialize();
        }

        private void OnRoutineCompleted(Routine r)
        {
            _routines.Remove(r);
        }

        private IEnumerator DelayImpl(float seconds, Action cb)
        {
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
            cb?.Invoke();
        }

        private IEnumerator NextFrameImpl(Action cb)
        {
            yield return null;
            cb?.Invoke();
        }

        private IEnumerator EveryImpl(float interval, Action tick, bool invokeNow)
        {
            if (invokeNow) tick?.Invoke();
            if (interval <= 0f)
            {
                // каждую рамку
                while (true)
                {
                    yield return null;
                    tick?.Invoke();
                }
            }
            else
            {
                var w = new WaitForSeconds(interval);
                while (true)
                {
                    yield return w;
                    tick?.Invoke();
                }
            }
        }
        private sealed class Driver : MonoBehaviour
        {
            private CoroutineService _svc;
            public void Init(CoroutineService svc) => _svc = svc;

            private void Update()
            {
                if (_svc == null) return;
                var subs = _svc._updateSubs;
                if (subs.Count == 0) return;

                float dt = Time.deltaTime;

                var arr = subs.ToArray();
                for (int i = 0; i < arr.Length; i++)
                    arr[i]?.Invoke(dt);
            }
        }

        private sealed class Routine : IRoutine
        {
            private readonly CoroutineService _svc;
            private readonly IEnumerator _enumerator;

            private Coroutine _coro;
            private bool _paused;
            private bool _running;
            private bool _stopping;

            public bool IsRunning => _running;
            public bool IsPaused => _paused;

            public event Action Completed;

            public Routine(CoroutineService svc, IEnumerator enumerator)
            {
                _svc = svc;
                _enumerator = Wrap(enumerator);
            }

            public void Start(CoroutineRunner runner)
            {
                if (_running) return;
                _running = true;
                _coro = runner.StartCoroutine(_enumerator);
            }

            public void Stop()
            {
                if (!_running) return;
                _stopping = true;
                try
                {
                    if (_svc._go != null && _coro != null)
                        _svc._runner.StopCoroutine(_coro);
                }
                catch { /* ignored */ }
                finally
                {
                    Finish();
                }
            }

            public void Pause() { _paused = true; }
            public void Resume() { _paused = false; }

            public void Dispose() => Stop();

            private IEnumerator Wrap(IEnumerator inner)
            {
                // вставка Ђпаузыї: пока _paused Ч ждЄм кадр
                while (true)
                {
                    if (_stopping) yield break;

                    if (_paused)
                    {
                        yield return null;
                        continue;
                    }

                    if (!inner.MoveNext())
                        break;

                    yield return inner.Current;
                }

                Finish();
            }

            private void Finish()
            {
                if (!_running) return;
                _running = false;
                _svc.OnRoutineCompleted(this);
                try { Completed?.Invoke(); } catch { /* ignored */ }
            }
        }
    }
}
