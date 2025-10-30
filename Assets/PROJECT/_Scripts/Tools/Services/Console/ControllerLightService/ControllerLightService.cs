using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_PS4
using UnityEngine.PS4;
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine.InputSystem.DualShock;
#endif

#if UNITY_PS5
using UnityEngine.InputSystem.PS5;
#endif

public enum LightbarColorKey
{
    Default,
    Emerald,
    Purple,
    Red,
    Yellow,
    Blue,
    White,
    Black
}

public class ControllerLightService : MonoBehaviour
{
    [Serializable]
    public struct PaletteEntry
    {
        public LightbarColorKey Key;
        public Color Color;
    }

    [Header("Palette")]
    [Tooltip("Список цветов палитры. Можно переопределять в инспекторе.")]
    public List<PaletteEntry> Palette = new List<PaletteEntry>()
    {
        new PaletteEntry{ Key = LightbarColorKey.Default, Color = Color.white },
        new PaletteEntry{ Key = LightbarColorKey.Emerald, Color = new Color(0.0f, 0.78f, 0.55f) },
        new PaletteEntry{ Key = LightbarColorKey.Purple,  Color = new Color(0.60f, 0.25f, 0.85f) },
        new PaletteEntry{ Key = LightbarColorKey.Red,     Color = new Color(1.00f, 0.10f, 0.10f) },
        new PaletteEntry{ Key = LightbarColorKey.Yellow,  Color = new Color(1.00f, 0.90f, 0.20f) },
        new PaletteEntry{ Key = LightbarColorKey.Blue,    Color = new Color(0.20f, 0.50f, 1.00f) },
        new PaletteEntry{ Key = LightbarColorKey.White,   Color = Color.white },
        new PaletteEntry{ Key = LightbarColorKey.Black,   Color = Color.black },
    };

    public Color Get(LightbarColorKey key)
    {
        foreach (var e in Palette)
            if (e.Key == key) return e.Color;
        return Color.white;
    }

    [Header("Timings")]
    [Range(0f, 2f)] public float DefaultFlashDuration = 0.4f;
    [Range(0f, 2f)] public float DefaultFadeDuration = 0.6f;

    [Header("Global Switch")]
    [Tooltip("Глобальный выключатель подсветки.")]
    public bool Enabled = true;

    private enum Priority { Normal, High }
    private Priority _runningPriority = Priority.Normal;

    private Coroutine _activeRoutine;
    private Color _baseColor;
    private bool _initialized;

    private struct OverrideEntry { public int Token; public Color Color; }
    private readonly List<OverrideEntry> _overrides = new();
    private int _nextToken = 1;

    private int _requestVersion = 0;
    private int BumpVersion() => ++_requestVersion;
    private bool IsStale(int ver) => ver != _requestVersion;

    private void OnEnable()
    {
        _baseColor = Get(LightbarColorKey.Default);
        SubscribeDeviceEvents();
        StartCoroutine(CoInitOnce());
    }

    private void OnDisable()
    {
        UnsubscribeDeviceEvents();
        ForceOff();
        _initialized = false;
    }

    private IEnumerator CoInitOnce()
    {
        yield return null; yield return null;

        float t = 0f, timeout = 1f;
        while (!HasAnySupportedPad() && t < timeout)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
        }

        _initialized = true;

        if (Enabled) ApplyRaw(_baseColor);
        else ApplyRaw(Color.black);
    }

    private static bool HasAnySupportedPad()
    {
#if UNITY_PS4
        var handles = Pad.GetHandles();
        if (handles != null && handles.Length > 0) return true;
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Gamepad.all.Any(g => g is DualShockGamepad)) return true;
#endif

#if UNITY_PS5
        if (Gamepad.all.Any(g => g is DualSenseGamepad)) return true;
#endif

        return false;
    }

    private IEnumerator CoFadeToVersioned(Color target, float duration, int version, bool restoreBaseAfter = true)
    {
        if (!Enabled)
        {
            ApplyRaw(Color.black);
            yield break;
        }

        var start = ReadApproxColor();
        duration = Mathf.Max(0.01f, duration);
        float t = 0f;

        while (t < duration)
        {
            if (IsStale(version)) yield break; // пришёл новый запрос — этот отменяем
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            var col = Color.Lerp(start, target, Smooth(k));
            ApplyRaw(col);
            yield return null;
        }

        if (!IsStale(version))
            ApplyRaw(target);

        // не откатываемся, если restoreBaseAfter=false
        if (restoreBaseAfter && !IsStale(version) && target != _baseColor)
            ApplyRaw(_baseColor);
    }

    /// <summary>Задать базовый цвет (возврат к нему после эффектов).</summary>
    public void SetBaseColor(LightbarColorKey key)
    {
        var col = Get(key);
        _baseColor = col;

        var effective = GetEffectiveBaseColor();
        if (Enabled) ApplyRaw(effective); else ApplyRaw(Color.black);
    }

    /// <summary>Мгновенно выключить световую полосу и остановить эффекты.</summary>
    public void ForceOff()
    {
        StopEffect();
        ApplyRaw(Color.black);
    }

    /// <summary>Одноразовая вспышка указанным цветом, затем возврат к базовому.</summary>
    public void Flash(LightbarColorKey key, float duration = -1f, bool highPriority = true)
    {
        var d = duration > 0f ? duration : DefaultFlashDuration;
        var col = Get(key);
        PlayEffect(CoFlash(col, d), highPriority ? Priority.High : Priority.Normal);
    }

    /// <summary>Плавный переход к цвету из палитры.</summary>
    public void FadeTo(LightbarColorKey key, float duration = -1f)
    {
        var d = duration > 0f ? duration : DefaultFadeDuration;
        var col = Get(key);
        _baseColor = col; // считаем, что это и новая база
        PlayEffect(CoFadeTo(col, d), Priority.Normal);
    }

    /// <summary>Плавное гашение до чёрного, база сохраняется.</summary>
    public void FadeOff(float duration = -1f)
    {
        var d = duration > 0f ? duration : DefaultFadeDuration;
        var ver = BumpVersion();
        PlayEffect(CoFadeToVersioned(Color.black, d, ver, restoreBaseAfter: false), Priority.High);
    }


    /// <summary>Плавное восстановление к базовому цвету.</summary>
    public void FadeOn(float duration = -1f)
    {
        var d = duration > 0f ? duration : DefaultFadeDuration;
        var ver = BumpVersion();
        PlayEffect(CoFadeToVersioned(_baseColor, d, ver), Priority.High);
    }

    /// <summary>Серия миганий указанным цветом.</summary>
    public void Blink(LightbarColorKey key, int count, float on = 0.2f, float off = 0.15f)
    {
        PlayEffect(CoBlink(Get(key), count, on, off), Priority.Normal);
    }

    private IEnumerator CoFlash(Color c, float duration)
    {
        if (!Enabled) yield break;

        var prev = ReadApproxColor();
        ApplyRaw(c);
        yield return new WaitForSecondsRealtime(duration);
        ApplyRaw(_baseColor);
        _runningPriority = Priority.Normal;
    }

    private IEnumerator CoFadeTo(Color target, float duration, bool restoreBaseAfter = true)
    {
        if (!Enabled)
        {
            ApplyRaw(Color.black);
            yield break;
        }

        var start = ReadApproxColor();
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            var col = Color.Lerp(start, target, Smooth(k));
            ApplyRaw(col);
            yield return null;
        }

        ApplyRaw(target);

        if (restoreBaseAfter && target != _baseColor)
            ApplyRaw(_baseColor);

        _runningPriority = Priority.Normal;
    }

    private IEnumerator CoBlink(Color c, int count, float on, float off)
    {
        if (!Enabled) yield break;

        for (int i = 0; i < count; i++)
        {
            ApplyRaw(c);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, on));
            ApplyRaw(_baseColor);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, off));
        }

        _runningPriority = Priority.Normal;
    }

    private static float Smooth(float x) => x * x * (3f - 2f * x); // smoothstep

    private Color ReadApproxColor()
    {
        return _lastApplied;
    }

    private void StopEffect()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = null;
        _runningPriority = Priority.Normal;
    }

    private void PlayEffect(IEnumerator routine, Priority priority)
    {
        if (_activeRoutine != null && priority < _runningPriority) return;
        StopEffect();
        _runningPriority = priority;
        _activeRoutine = StartCoroutine(RunEffect(routine));
    }

    private IEnumerator RunEffect(IEnumerator routine)
    {
        yield return routine;
        if (Enabled) ApplyRaw(_baseColor);
        else ApplyRaw(Color.black);
        _runningPriority = Priority.Normal;
        _activeRoutine = null;
    }

    public int PushBaseOverride(Color color)
    {
        var token = _nextToken++;
        _overrides.Add(new OverrideEntry { Token = token, Color = color });

        if (Enabled)
        {
            _baseColor = color;
            var ver = BumpVersion();
            PlayEffect(CoFadeToVersioned(_baseColor, 0.15f, ver), Priority.High);
        }
        return token;
    }

    public void PopBaseOverride(int token)
    {
        var i = _overrides.FindIndex(e => e.Token == token);
        if (i >= 0) _overrides.RemoveAt(i);
        var effective = GetEffectiveBaseColor();
        _baseColor = effective;
        if (Enabled)
        {
            var ver = BumpVersion();
            PlayEffect(CoFadeToVersioned(_baseColor, 0.15f, ver), Priority.High);
        }
    }

    public Color GetEffectiveBaseColor()
    {
        if (_overrides.Count > 0) return _overrides[_overrides.Count - 1].Color;
        return _baseColor;
    }

    private void SubscribeDeviceEvents()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        Application.focusChanged += OnFocusChanged;
    }

    private void UnsubscribeDeviceEvents()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        Application.focusChanged -= OnFocusChanged;
    }

    private void OnFocusChanged(bool focus)
    {
        if (!_initialized) return;
        if (focus)
        {
            if (Enabled) ApplyRaw(_baseColor);
            else ApplyRaw(Color.black);
        }
    }

    private void OnDeviceChange(InputDevice dev, InputDeviceChange change)
    {
        if (!_initialized) return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Enabled:
            case InputDeviceChange.ConfigurationChanged:
                if (Enabled) ApplyRaw(_baseColor);
                else ApplyRaw(Color.black);
                break;
        }
    }


    private Color _lastApplied = Color.black;

    private void ApplyRaw(Color color)
    {
        _lastApplied = color;

#if UNITY_PS4
        var handles = Pad.GetHandles();
        if (handles != null && handles.Length > 0)
        {
            int h = handles[0];
            Pad.SetLightBar(
                h,
                (byte)(Mathf.Clamp01(color.r) * 255),
                (byte)(Mathf.Clamp01(color.g) * 255),
                (byte)(Mathf.Clamp01(color.b) * 255)
            );
            return;
        }
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
        foreach (var device in Gamepad.all)
        {
            if (device is DualShockGamepad dsBase)
            {
                dsBase.SetLightBarColor(color);
            }
#if UNITY_PS5
            else if (device is DualSenseGamepad ds5)
            {
                ds5.SetLightBarColor(color);
            }
#endif
        }
#else
#endif
    }

}
