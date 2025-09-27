using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomAnimator : MonoBehaviour
{
    [Header("Rig")]
    [SerializeField] private Transform rigRoot;              // корень рига
    [SerializeField] private bool applyScale = true;         // примен€ть ли scale

    public CustomAnimClip Current { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool Loop { get; private set; }
    public float TimePos { get; private set; }               // текущее врем€ (сек)
    public float TimeScale { get; private set; } = 1f;       // множитель скорости

    public int FrameCount => Current ? Current.FrameCount : 0;
    public int CurrentFrameIndex
    {
        get
        {
            if (!Current || Current.Length <= 0f) return 0;
            float t = Mathf.Clamp(TimePos, 0, Current.Length - 1e-6f);
            float f = t * Mathf.Max(1, Current.Fps);
            return Mathf.Clamp(Mathf.FloorToInt(f), 0, FrameCount - 1);
        }
    }

    /// <summary> ¬ызываетс€, когда срабатывает встроенное событие из клипа (по имени) </summary>
    public event Action<string> ClipEventFired;

    // ---- внутреннее ----
    private Transform[] _bones;                               // кэш костей
    private readonly Dictionary<int, Action> _frameEvents = new();   // внешние событи€ по кадрам
    private readonly SortedList<float, Action> _normEvents = new();  // внешние событи€ по норм. времени (ключ 0..1)

    // чтобы не дергать Clip.Events каждый кадр
    private float[] _clipEventTimes;                          // нормализованные точки из клипа
    private string[] _clipEventNames;

    #region Public API

    public void Play(CustomAnimClip clip, float timeScale = 1f, bool loop = true, float startTime = 0f)
    {
        if (clip == null)
        {
            Stop();
            return;
        }

        Current = clip;
        Loop = loop;
        TimeScale = Mathf.Max(0.001f, timeScale);
        TimePos = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.Length - 1e-6f));
        IsPlaying = true;

        MapBonesIfNeeded(force: true);
        ApplyFrame(CurrentFrameIndex, force: true);

        // подготовим кеш встроенных событий клипа
        CacheClipEvents(clip);

        // (по желанию) можно чистить внешние одноразовые подписки
        // _frameEvents.Clear(); _normEvents.Clear();
    }

    public void Stop()
    {
        IsPlaying = false;
        Current = null;
        _bones = null;
        _clipEventTimes = null;
        _clipEventNames = null;
        _frameEvents.Clear();
        _normEvents.Clear();
        TimePos = 0f;
    }

    public void Pause() => IsPlaying = false;
    public void Resume() => IsPlaying = Current != null;

    public void SetSpeed(float timeScale) => TimeScale = Mathf.Max(0.001f, timeScale);

    /// <summary> ѕроиграть клип так, чтобы его длительность равн€лась 1/attacksPerSecond. </summary>
    public void PlayScaledForAttack(CustomAnimClip clip, float attacksPerSecond, bool loop = false)
    {
        if (!clip) return;
        float desiredDur = Mathf.Max(0.001f, 1f / Mathf.Max(0.001f, attacksPerSecond));
        float scale = clip.Length / desiredDur; // >1 быстрее, <1 медленнее
        Play(clip, timeScale: scale, loop: loop, startTime: 0f);
    }

    // ---- внешние подписки на событи€ ----

    /// <summary> —рабатывает один раз при проходе через указанный кадр (0..FrameCount-1). ћожно вызывать многократно. </summary>
    public void OnFrame(int frameIndex, Action action)
    {
        if (action == null) return;
        if (!Current) return;
        frameIndex = Mathf.Clamp(frameIndex, 0, FrameCount - 1);
        if (_frameEvents.TryGetValue(frameIndex, out var exist))
            _frameEvents[frameIndex] = exist + action;
        else
            _frameEvents[frameIndex] = action;
    }

    /// <summary> —рабатывает один раз при проходе нормализованной точки времени (0..1). </summary>
    public void OnNormalized(float normTime, Action action)
    {
        if (action == null) return;
        normTime = Mathf.Clamp01(normTime);
        if (_normEvents.TryGetValue(normTime, out var exist))
            _normEvents[normTime] = exist + action;
        else
            _normEvents.Add(normTime, action);
    }

    /// <summary> ”добный хелпер дл€ Ђпредпоследнего кадраї. </summary>
    public void OnPenultimateFrame(Action action)
    {
        if (!Current) return;
        int idx = Mathf.Max(0, FrameCount - 2);
        OnFrame(idx, action);
    }

    #endregion

    #region Update / Playback

    private void Update()
    {
        if (!IsPlaying || Current == null || Current.Length <= 0f)
            return;

        float prevTime = TimePos;
        int prevFrame = CurrentFrameIndex;

        // шаг времени
        TimePos += UnityEngine.Time.deltaTime * TimeScale;

        // управление концом/лупом
        if (TimePos >= Current.Length)
        {
            if (Loop)
            {
                TimePos = Mathf.Repeat(TimePos, Current.Length);
            }
            else
            {
                TimePos = Current.Length - 1e-6f;
                IsPlaying = false;
            }
        }

        int curFrame = CurrentFrameIndex;

        // примен€ем позу при смене кадра
        if (curFrame != prevFrame) ApplyFrame(curFrame);

        // дергаем событи€ (внешние frame/norm + встроенные клиповые)
        FireExternalFrameEvents(prevFrame, curFrame);
        FireExternalNormalizedEvents(prevTime, TimePos);
        FireClipEvents(prevTime, TimePos);
    }

    private void ApplyFrame(int frameIndex, bool force = false)
    {
        if (Current == null) return;
        if (!MapBonesIfNeeded()) return;

        int f = Mathf.Clamp(frameIndex, 0, Current.FrameCount - 1);
        var fr = Current.Frames[f];

        // защита от несоответстви€ размеров
        if (fr.LocalPos == null || fr.LocalRot == null) return;
        int count = Mathf.Min(_bones.Length, fr.LocalPos.Length, fr.LocalRot.Length);

        for (int i = 0; i < count; i++)
        {
            var t = _bones[i];
            if (!t) continue;

            t.localPosition = fr.LocalPos[i];
            t.localRotation = fr.LocalRot[i];
            if (applyScale && fr.LocalScale != null && i < fr.LocalScale.Length)
                t.localScale = fr.LocalScale[i];
        }
    }

    #endregion

    #region Bone mapping

    private bool MapBonesIfNeeded(bool force = false)
    {
        if (!Current) return false;
        if (!rigRoot)
        {
            Debug.LogWarning("[CustomAnimator] rigRoot is not assigned.");
            return false;
        }

        if (!force && _bones != null && _bones.Length == (Current.BonePaths?.Length ?? 0))
            return true;

        var paths = Current.BonePaths ?? Array.Empty<string>();
        _bones = new Transform[paths.Length];

        for (int i = 0; i < paths.Length; i++)
        {
            string p = paths[i];
            _bones[i] = string.IsNullOrEmpty(p) ? rigRoot : rigRoot.Find(p);
#if UNITY_EDITOR
            if (_bones[i] == null)
                Debug.LogWarning($"[CustomAnimator] Bone not found by path '{p}' under '{rigRoot.name}'");
#endif
        }
        return true;
    }

    #endregion

    #region Events firing

    private void FireExternalFrameEvents(int prevFrame, int curFrame)
    {
        if (_frameEvents.Count == 0 || FrameCount <= 0) return;

        if (curFrame == prevFrame) return;

        if (curFrame > prevFrame)
        {
            for (int f = prevFrame + 1; f <= curFrame; f++)
                if (_frameEvents.TryGetValue(f, out var act)) act?.Invoke();
        }
        else
        {
            // зацикливание
            for (int f = prevFrame + 1; f < FrameCount; f++)
                if (_frameEvents.TryGetValue(f, out var act)) act?.Invoke();
            for (int f = 0; f <= curFrame; f++)
                if (_frameEvents.TryGetValue(f, out var act)) act?.Invoke();
        }
    }

    private void FireExternalNormalizedEvents(float prevTime, float curTime)
    {
        if (_normEvents.Count == 0 || Current == null || Current.Length <= 0f) return;

        float len = Current.Length;
        float t0 = Mathf.Clamp01(prevTime / len);
        float t1 = Mathf.Clamp01(curTime / len);

        if (t1 > t0)
        {
            // обычный прогресс
            for (int i = 0; i < _normEvents.Count; i++)
            {
                float key = _normEvents.Keys[i];
                if (key > t0 && key <= t1) _normEvents.Values[i]?.Invoke();
            }
        }
        else if (t1 < t0)
        {
            // луп: [t0..1] и [0..t1]
            for (int i = 0; i < _normEvents.Count; i++)
            {
                float key = _normEvents.Keys[i];
                if (key > t0 || key <= t1) _normEvents.Values[i]?.Invoke();
            }
        }
    }

    private void CacheClipEvents(CustomAnimClip clip)
    {
        if (clip.Events == null || clip.Events.Length == 0)
        {
            _clipEventTimes = null;
            _clipEventNames = null;
            return;
        }

        int n = clip.Events.Length;
        _clipEventTimes = new float[n];
        _clipEventNames = new string[n];

        for (int i = 0; i < n; i++)
        {
            _clipEventTimes[i] = Mathf.Clamp01(clip.Events[i].NormalizedTime);
            _clipEventNames[i] = clip.Events[i].Name;
        }

        Array.Sort(_clipEventTimes, _clipEventNames);
    }

    private void FireClipEvents(float prevTime, float curTime)
    {
        if (_clipEventTimes == null || _clipEventNames == null || Current == null || Current.Length <= 0f) return;

        float len = Current.Length;
        float t0 = Mathf.Clamp01(prevTime / len);
        float t1 = Mathf.Clamp01(curTime / len);

        if (t1 > t0)
        {
            // обычный прогресс
            for (int i = 0; i < _clipEventTimes.Length; i++)
            {
                float k = _clipEventTimes[i];
                if (k > t0 && k <= t1) ClipEventFired?.Invoke(_clipEventNames[i]);
            }
        }
        else if (t1 < t0)
        {
            // луп: [t0..1] и [0..t1]
            for (int i = 0; i < _clipEventTimes.Length; i++)
            {
                float k = _clipEventTimes[i];
                if (k > t0 || k <= t1) ClipEventFired?.Invoke(_clipEventNames[i]);
            }
        }
    }

    #endregion
}
