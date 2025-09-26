using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    [Header("Auto-collect if empty: all ParticleSystem in children")]
    [SerializeField] private List<ParticleSystem> _systems = new();

    private Action<ParticleController> _onFinished;
    private bool _isPlaying;
    private Coroutine _waitRoutine;

    private void Awake()
    {
        if (_systems == null || _systems.Count == 0)
        {
            _systems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>(true));
        }
    }

    public void Play(Action<ParticleController> onFinished = null)
    {
        _onFinished = onFinished;
        _isPlaying = true;

 
        for (int i = 0; i < _systems.Count; i++)
        {
            var ps = _systems[i];
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }


        if (_waitRoutine != null) StopCoroutine(_waitRoutine);
        _waitRoutine = StartCoroutine(WaitAllFinished());
    }

    public void StopImmediate()
    {
        _isPlaying = false;
        if (_waitRoutine != null) { StopCoroutine(_waitRoutine); _waitRoutine = null; }

        for (int i = 0; i < _systems.Count; i++)
        {
            var ps = _systems[i];
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public bool IsAlive()
    {
        if (_systems == null) return false;
        for (int i = 0; i < _systems.Count; i++)
        {
            var ps = _systems[i];
            if (ps != null && ps.IsAlive(true))
                return true;
        }
        return false;
    }

    private IEnumerator WaitAllFinished()
    {

        while (_isPlaying && IsAlive())
            yield return null;

        _waitRoutine = null;
        _isPlaying = false;

        var cb = _onFinished;
        _onFinished = null;
        cb?.Invoke(this);
    }
}
