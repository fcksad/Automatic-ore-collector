using System.Collections.Generic;
using System;
using UnityEngine;

namespace Service.Tweener
{
    public enum Ease
    {
        Linear,
        InSine, OutSine, InOutSine,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart,
        InQuint, OutQuint, InOutQuint,
        InExpo, OutExpo, InOutExpo,
        InCirc, OutCirc, InOutCirc,
        InBack, OutBack, InOutBack
    }
    public sealed class Tweener<T> : BaseTween
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        private readonly T startValue;
        private readonly T endValue;

        private readonly ITweenDriver<T> driver;

        public Tweener(Func<T> getter, Action<T> setter, T endValue, float duration)
        {
            this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
            this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
            this.startValue = getter();
            this.endValue = endValue;
            this.duration = Mathf.Max(0.0001f, duration);

            driver = TweenDrivers.Get<T>();
            if (driver == null) throw new NotSupportedException($"No tween driver for type {typeof(T).Name}");
        }

        // специфичные опции
        private bool _useSlerp; // для кватерниона
        public Tweener<T> UseSlerp(bool value) { _useSlerp = value; return this; }

        public new Tweener<T> SetDelay(float d) { base.SetDelay(d); return this; }
        public new Tweener<T> SetEase(Ease e) { base.SetEase(e); return this; }
        public new Tweener<T> SetEase(AnimationCurve c) { base.SetEase(c); return this; }
        public new Tweener<T> SetLoops(int count, bool yoyo = false) { base.SetLoops(count, yoyo); return this; }
        public new Tweener<T> Pause() { base.Pause(); return this; }
        public new Tweener<T> Resume() { base.Resume(); return this; }
        public new Tweener<T> Kill(bool complete = false) { base.Kill(complete); return this; }
        public new Tweener<T> OnComplete(Action cb) { base.OnComplete(cb); return this; }
        public new Tweener<T> OnUpdate(Action cb) { base.OnUpdate(cb); return this; }

        protected override void Apply(float t)
        {
            var v = driver.Lerp(startValue, endValue, t, _useSlerp);
            setter(v);
        }
    }

    internal sealed class IntervalTween : BaseTween
    {
        private readonly Action _tick;
        private readonly float _step;

        public IntervalTween(float stepSeconds, int loops, Action tick)
        {
            duration = Mathf.Max(0.0001f, stepSeconds);
            _step = duration;
            this.loops = loops <= 0 ? -1 : loops;
            _tick = tick;
        }

        protected override void Apply(float t) { /* no-op */ }

        public override void Update(float dt)
        {
            if (IsKilled || IsComplete || !IsPlaying) return;

            if (delay > 0f)
            {
                delay -= dt;
                if (delay > 0f) return;
                dt = -delay;
                delay = 0f;
            }

            elapsed += dt;
            if (elapsed >= _step)
            {
                elapsed -= _step;  
                try { _tick?.Invoke(); } catch { /* ignore */ }

                if (loops != -1)
                {
                    loopsDone++;
                    if (loopsDone >= loops)
                    {
                        CompleteInternal();
                        return;
                    }
                }
            }
        }
    }

    public static partial class TW
    {
        // === To: generic через getter/setter ===
        public static Tweener<T> To<T>(
            Func<T> getter, Action<T> setter, T endValue, float duration)
        {
            EnsureRunner();
            var t = new Tweener<T>(getter, setter, endValue, duration);
            _active.Add(t);
            return t;
        }

        // === Delay ===
        public static ITweener Delay(float seconds, Action callback)
        {
            EnsureRunner();
            var t = new TimerTween(seconds, callback);
            _active.Add(t);
            return t;
        }

        // === Утилиты-экстеншены ===
        public static Tweener<Vector3> MoveTo(this Transform tr, Vector3 to, float duration)
            => To(() => tr.position, v => tr.position = v, to, duration);

        public static Tweener<Vector3> LocalMoveTo(this Transform tr, Vector3 to, float duration)
            => To(() => tr.localPosition, v => tr.localPosition = v, to, duration);

        public static Tweener<Vector3> ScaleTo(this Transform tr, Vector3 to, float duration)
            => To(() => tr.localScale, v => tr.localScale = v, to, duration);

        public static Tweener<Quaternion> RotateTo(this Transform tr, Quaternion to, float duration)
            => To(() => tr.rotation, v => tr.rotation = v, to, duration).UseSlerp(true);

        public static Tweener<Quaternion> LocalRotateTo(this Transform tr, Quaternion to, float duration)
            => To(() => tr.localRotation, v => tr.localRotation = v, to, duration).UseSlerp(true);

        // === Менеджмент ===
        internal static void UpdateAll(float dt)
        {
            // копия на случай изменения списка во время итерации
            _buffer.Clear();
            _buffer.AddRange(_active);

            for (int i = 0; i < _buffer.Count; i++)
            {
                var t = _buffer[i];
                if (t.IsKilled) continue;

                t.Update(dt);
                if (t.IsComplete || t.IsKilled)
                    _toRemove.Add(t);
            }

            if (_toRemove.Count > 0)
            {
                for (int i = 0; i < _toRemove.Count; i++)
                    _active.Remove(_toRemove[i]);
                _toRemove.Clear();
            }
        }
        public static ITweener Every(float stepSeconds, int loops, Action tick)
        {
            EnsureRunner();
            var t = new IntervalTween(stepSeconds, loops, tick);
            _active.Add((BaseTween)t);
            return t;
        }

        public static void KillAll(bool complete = false)
        {
            foreach (var t in _active)
                t.Kill(complete);
            _active.Clear();
        }

        // === Внутреннее ===
        private static readonly List<BaseTween> _active = new();
        private static readonly List<BaseTween> _buffer = new();
        private static readonly List<BaseTween> _toRemove = new();
        private static TweenRunner _runner;

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("[TweenLite]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<TweenRunner>();
        }
    }

    // ---------- База твинеров ----------
    public abstract class BaseTween : ITweener
    {
        public bool IsPlaying { get; protected set; } = true;
        public bool IsComplete { get; protected set; }
        public bool IsKilled { get; protected set; }

        protected float duration;
        protected float elapsed;
        protected float delay;
        protected int loops;          // -1 = infinite
        protected bool yoyo;
        protected int loopsDone;
        protected bool forward = true;

        protected Func<float, float> easeFunc = EaseFuncs.Linear;
        protected AnimationCurve customCurve;

        protected Action onComplete;
        protected Action onUpdate;

        public BaseTween SetDelay(float d) { delay = Mathf.Max(0, d); return this; }
        public BaseTween SetEase(Ease e) { easeFunc = EaseFuncs.Get(e); customCurve = null; return this; }
        public BaseTween SetEase(AnimationCurve curve) { customCurve = curve; return this; }
        public BaseTween SetLoops(int count, bool yoyo = false) { loops = count; this.yoyo = yoyo; return this; }

        public ITweener Pause() { IsPlaying = false; return this; }
        public ITweener Resume() { if (!IsComplete && !IsKilled) IsPlaying = true; return this; }
        public ITweener Kill(bool complete = false) { IsKilled = true; if (complete && !IsComplete) CompleteInternal(); return this; }

        public ITweener OnComplete(Action cb) { onComplete += cb; return this; }
        public ITweener OnUpdate(Action cb) { onUpdate += cb; return this; }

        public virtual void Update(float dt)
        {
            if (IsKilled || IsComplete) return;
            if (!IsPlaying) return;

            if (delay > 0f)
            {
                delay -= dt;
                if (delay > 0f) return;
                dt = -delay; // взять остаток дельты в первый кадр после задержки
                delay = 0f;
            }

            elapsed += dt;
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float eased = customCurve != null ? customCurve.Evaluate(t) : easeFunc(t);

            Apply(forward ? eased : 1f - eased);
            onUpdate?.Invoke();

            if (elapsed >= duration)
            {
                if (loops == -1 || loopsDone < loops)
                {
                    loopsDone++;
                    elapsed = 0f;
                    if (yoyo) forward = !forward;
                    // не завершаем — следующий цикл
                }
                else
                {
                    CompleteInternal();
                }
            }
        }

        protected virtual void CompleteInternal()
        {
            if (IsComplete) return;
            Apply(forward ? 1f : 0f); 
            IsComplete = true;
            IsPlaying = false;
            try { onComplete?.Invoke(); } catch { /* ignored */ }
        }

        protected abstract void Apply(float t);
    }


   

    // ---------- Драйверы типов ----------
    internal interface ITweenDriver<T>
    {
        T Lerp(T a, T b, float t, bool slerp);
    }

    internal static class TweenDrivers
    {
        public static ITweenDriver<T> Get<T>()
        {
            if (typeof(T) == typeof(float)) return (ITweenDriver<T>)(object)FloatDriver.Instance;
            if (typeof(T) == typeof(Vector2)) return (ITweenDriver<T>)(object)Vector2Driver.Instance;
            if (typeof(T) == typeof(Vector3)) return (ITweenDriver<T>)(object)Vector3Driver.Instance;
            if (typeof(T) == typeof(Color)) return (ITweenDriver<T>)(object)ColorDriver.Instance;
            if (typeof(T) == typeof(Quaternion)) return (ITweenDriver<T>)(object)QuaternionDriver.Instance;
            return null;
        }

        private sealed class FloatDriver : ITweenDriver<float>
        {
            public static readonly FloatDriver Instance = new();
            public float Lerp(float a, float b, float t, bool _) => Mathf.LerpUnclamped(a, b, t);
        }
        private sealed class Vector2Driver : ITweenDriver<Vector2>
        {
            public static readonly Vector2Driver Instance = new();
            public Vector2 Lerp(Vector2 a, Vector2 b, float t, bool _) => Vector2.LerpUnclamped(a, b, t);
        }
        private sealed class Vector3Driver : ITweenDriver<Vector3>
        {
            public static readonly Vector3Driver Instance = new();
            public Vector3 Lerp(Vector3 a, Vector3 b, float t, bool _) => Vector3.LerpUnclamped(a, b, t);
        }
        private sealed class ColorDriver : ITweenDriver<Color>
        {
            public static readonly ColorDriver Instance = new();
            public Color Lerp(Color a, Color b, float t, bool _) => Color.LerpUnclamped(a, b, t);
        }
        private sealed class QuaternionDriver : ITweenDriver<Quaternion>
        {
            public static readonly QuaternionDriver Instance = new();
            public Quaternion Lerp(Quaternion a, Quaternion b, float t, bool slerp)
                => slerp ? Quaternion.SlerpUnclamped(a, b, t) : Quaternion.LerpUnclamped(a, b, t);
        }
    }

    // ---------- Таймер как твина ----------
    internal sealed class TimerTween : BaseTween
    {
        private readonly Action cb;
        public TimerTween(float seconds, Action cb)
        {
            this.duration = Mathf.Max(0.0001f, seconds);
            this.cb = cb;
        }
        protected override void Apply(float t) { /* no-op */ }
        protected override void CompleteInternal()
        {
            base.CompleteInternal();
            try { cb?.Invoke(); } catch { /* ignored */ }
        }
    }

    // ---------- Апдейтер ----------
    internal sealed class TweenRunner : MonoBehaviour
    {
        private void Update()
        {
            // Time.deltaTime — можно заменить на unscaled по желанию
            TW.UpdateAll(Time.deltaTime);
        }
    }

    // ---------- Ease-функции ----------
    internal static class EaseFuncs
    {
        public static readonly Func<float, float> Linear = t => t;

        public static Func<float, float> Get(Ease e) => e switch
        {
            Ease.Linear => Linear,
            Ease.InSine => InSine,
            Ease.OutSine => OutSine,
            Ease.InOutSine => InOutSine,
            Ease.InQuad => InQuad,
            Ease.OutQuad => OutQuad,
            Ease.InOutQuad => InOutQuad,
            Ease.InCubic => InCubic,
            Ease.OutCubic => OutCubic,
            Ease.InOutCubic => InOutCubic,
            Ease.InQuart => InQuart,
            Ease.OutQuart => OutQuart,
            Ease.InOutQuart => InOutQuart,
            Ease.InQuint => InQuint,
            Ease.OutQuint => OutQuint,
            Ease.InOutQuint => InOutQuint,
            Ease.InExpo => InExpo,
            Ease.OutExpo => OutExpo,
            Ease.InOutExpo => InOutExpo,
            Ease.InCirc => InCirc,
            Ease.OutCirc => OutCirc,
            Ease.InOutCirc => InOutCirc,
            Ease.InBack => InBack,
            Ease.OutBack => OutBack,
            Ease.InOutBack => InOutBack,
            _ => Linear
        };

        // Sine
        private static float InSine(float t) => 1f - Mathf.Cos((t * Mathf.PI) / 2f);
        private static float OutSine(float t) => Mathf.Sin((t * Mathf.PI) / 2f);
        private static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        // Quad
        private static float InQuad(float t) => t * t;
        private static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        // Cubic
        private static float InCubic(float t) => t * t * t;
        private static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        // Quart
        private static float InQuart(float t) => t * t * t * t;
        private static float OutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
        private static float InOutQuart(float t) => t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;

        // Quint
        private static float InQuint(float t) => t * t * t * t * t;
        private static float OutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);
        private static float InOutQuint(float t) => t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;

        // Expo
        private static float InExpo(float t) => t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
        private static float OutExpo(float t) => t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        private static float InOutExpo(float t) =>
            t == 0f ? 0f : t == 1f ? 1f :
            t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f
                     : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;

        // Circ
        private static float InCirc(float t) => 1f - Mathf.Sqrt(1f - t * t);
        private static float OutCirc(float t) => Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
        private static float InOutCirc(float t) =>
            t < 0.5f ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f
                     : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;

        // Back
        private const float c1 = 1.70158f;
        private const float c3 = c1 + 1f;
        private static float InBack(float t) => c3 * t * t * t - c1 * t * t;
        private static float OutBack(float t) => 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        private static float InOutBack(float t)
        {
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }
    }
}
