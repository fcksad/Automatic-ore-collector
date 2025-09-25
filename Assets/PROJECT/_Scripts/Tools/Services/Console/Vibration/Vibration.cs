using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;


namespace Services.Device
{
    public enum VibrationPreset
    {
        Click = 0,

    }

    public static class Vibration
    {
       /* private class Runner : MonoBehaviour
        {
            private void OnApplicationQuit() => Vibration.StopAll();
            private void OnDisable() => Vibration.StopAll();
        }

        private static Runner _runner;
        private static void EnsureRunner()
        {
            if (_runner) return;
            var go = new GameObject("[VibrationRunner]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }

        static Vibration() => EnsureRunner();

#if ENABLE_INPUT_SYSTEM 

        private static readonly Dictionary<Gamepad, Coroutine> _running = new();

        private class PadState
        {
            public readonly Dictionary<string, Vector2> channels = new(); // key -> (low, high)
        }

        private static readonly Dictionary<Gamepad, PadState> _pads = new();

        private static PadState GetPadState(Gamepad pad)
        {
            pad ??= Gamepad.current;
            if (pad == null) return null;
            if (!_pads.TryGetValue(pad, out var st))
            {
                st = new PadState();
                _pads[pad] = st;
            }
            return st;
        }

        private static void ApplyMix(Gamepad pad)
        {
            if (pad == null) return;
            var st = GetPadState(pad);
            if (st == null) return;

            float low = 0f, high = 0f;
            foreach (var kv in st.channels.Values)
            {
                if (kv.x > low) low = kv.x;
                if (kv.y > high) high = kv.y;
            }
            pad.SetMotorSpeeds(low, high);
        }

        public static void SetChannel(string key, float low, float high, Gamepad pad = null)
        {
            if (!Application.isPlaying) return;
            pad ??= Gamepad.current;
            if (!(pad is IDualMotorRumble) || pad == null) return;

            var st = GetPadState(pad);
            if (st == null) return;

            st.channels[key] = new Vector2(Mathf.Clamp01(low), Mathf.Clamp01(high));
            ApplyMix(pad);
        }

        public static void ClearChannel(string key, Gamepad pad = null)
        {
            pad ??= Gamepad.current;
            var st = GetPadState(pad);
            if (st == null) return;

            if (st.channels.Remove(key))
                ApplyMix(pad);
        }

        private static void StartForPad(Gamepad pad, System.Collections.IEnumerator routine)
        {
            EnsureRunner();
            if (pad == null) return;

            if (_running.TryGetValue(pad, out var c) && c != null)
                _runner.StopCoroutine(c);

            _running[pad] = _runner.StartCoroutine(routine);
        }

        public static bool IsSupported(Gamepad pad = null)
        {
            pad ??= Gamepad.current;
            bool ok = pad != null;
            Debug.Log(ok ? $"[Vibration] Gamepad: {pad.displayName}" : "[Vibration] No gamepad found.");
            return ok;
        }

        /// low/high [0..1], duration seconds (realtime)
        public static void Rumble(float low, float high, float duration, Gamepad pad = null)
        {
            if (!Application.isPlaying) return;
            pad ??= Gamepad.current;
            if (!(pad is IDualMotorRumble) || pad == null) return;

            if (duration <= 0f)
            {

                SetChannel("_runtime_", low, high, pad);
                return;
            }


            string key = "_oneshot_" + Time.realtimeSinceStartup.ToString("F6");
            StartForPad(pad, OneShotRoutine(pad, key, Mathf.Clamp01(low), Mathf.Clamp01(high), duration));
        }


        private static System.Collections.IEnumerator OneShotRoutine(Gamepad pad, string key, float low, float high, float duration)
        {
            SetChannel(key, low, high, pad);
            yield return new WaitForSecondsRealtime(duration);
            ClearChannel(key, pad);
        }


        public static void TriggerPreset(RumblePreset preset, Gamepad pad = null)
        {
            EnsureRunner();
            pad ??= Gamepad.current;
            if (pad == null) return;

            switch (preset)
            {
                case RumblePreset.Click: StartForPad(pad, OneShotRoutine(pad, "_click_", .10f, .20f, .10f)); break;
                case RumblePreset.Light: StartForPad(pad, OneShotRoutine(pad, "_light_", .30f, .40f, .20f)); break;
                case RumblePreset.Medium: StartForPad(pad, OneShotRoutine(pad, "_medium_", .40f, .70f, .40f)); break;
                case RumblePreset.Heavy: StartForPad(pad, OneShotRoutine(pad, "_heavy_", .70f, .90f, .90f)); break;
                case RumblePreset.ShortPulse: StartForPad(pad, Pulse(pad, 0.65f, 0.85f, 0.09f, 3, 0.05f)); break;
                case RumblePreset.LongPulse: StartForPad(pad, Pulse(pad, 0.70f, 0.90f, 0.15f, 4, 0.08f)); break;
                case RumblePreset.RampUp: StartForPad(pad, EngineKickAndSustain(pad)); break;
                case RumblePreset.RampDown: StartForPad(pad, Ramp(pad, 1, 0, 0.5f, hold: 0f, stopAtEnd: true)); break;
                case RumblePreset.DoubleClick: _runner.StartCoroutine(DoubleClick(pad)); break;
            }
        }

        public static void Stop(Gamepad pad = null)
        {
            pad ??= Gamepad.current;
            if (pad == null) return;
            var st = GetPadState(pad);
            st?.channels.Clear();
            pad.SetMotorSpeeds(0f, 0f);
        }

        public static void StopAll()
        {
            foreach (var p in Gamepad.all)
            {
                var st = GetPadState(p);
                st?.channels.Clear();
                p?.SetMotorSpeeds(0f, 0f);
            }
        }

        public static void RumbleAll(float low, float high, float duration)
        {
            EnsureRunner();
            foreach (var p in Gamepad.all) Rumble(low, high, duration, p);
        }

        private static System.Collections.IEnumerator RumbleRoutine(Gamepad pad, float low, float high, float duration)
        {
            pad.SetMotorSpeeds(low, high);
            yield return new WaitForSecondsRealtime(duration);
            if (pad != null) pad.SetMotorSpeeds(0f, 0f);
        }

        private static System.Collections.IEnumerator Pulse(Gamepad pad, float low, float high, float on, int count, float off)
        {
            for (int i = 0; i < count; i++)
            {
                if (pad == null) yield break;
                pad.SetMotorSpeeds(low, high);
                yield return new WaitForSecondsRealtime(on);
                pad.SetMotorSpeeds(0f, 0f);
                if (i < count - 1) yield return new WaitForSecondsRealtime(off);
            }
        }

        private static System.Collections.IEnumerator Ramp(Gamepad pad, float from, float to, float duration, float hold = 0f, bool stopAtEnd = false)
        {
            float t = 0f;
            while (t < duration)
            {
                if (pad == null) yield break;
                t += Time.fixedDeltaTime;
                float v = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                pad.SetMotorSpeeds(v * 0.8f, v);
                yield return null;
            }

            if (pad == null) yield break;

            if (hold > 0f)
                yield return new WaitForSecondsRealtime(hold);

            if (stopAtEnd)
                pad.SetMotorSpeeds(0f, 0f);
            else
                pad.SetMotorSpeeds(to * 0.8f, to);
        }

        private static System.Collections.IEnumerator DoubleClick(Gamepad pad)
        {
            if (pad == null) yield break;
            pad.SetMotorSpeeds(0.75f, 0.95f); yield return new WaitForSecondsRealtime(0.11f);
            pad.SetMotorSpeeds(0f, 0f); yield return new WaitForSecondsRealtime(0.06f);
            pad.SetMotorSpeeds(0.75f, 0.95f); yield return new WaitForSecondsRealtime(0.11f);
            pad.SetMotorSpeeds(0f, 0f);
        }

        private static System.Collections.IEnumerator EngineKickAndSustain(Gamepad pad, float peak = 1f, float sustain = 0.6f, float attack = 0.10f, float decay = 0.30f, float sustainTime = 0.5f, bool stopAtEnd = false)    
        {
            if (pad == null) yield break;


            float t = 0f;
            while (t < attack)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / attack);
                float v = Mathf.Lerp(0f, peak, a);           
                pad.SetMotorSpeeds(v * 0.8f, v);
                yield return null;
            }

  
            t = 0f;
            while (t < decay)
            {
                t += Time.unscaledDeltaTime;
                float d = Mathf.Clamp01(t / decay);
                float v = Mathf.SmoothStep(peak, sustain, d);
                pad.SetMotorSpeeds(v * 0.8f, v);
                yield return null;
            }

            pad.SetMotorSpeeds(sustain * 0.8f, sustain);
            if (sustainTime > 0f)
                yield return new WaitForSecondsRealtime(sustainTime);

            if (stopAtEnd)
                pad.SetMotorSpeeds(0f, 0f);
        }

        public static void PlayEngineStart(string key = "engine", Gamepad pad = null,
                                    float peak = 0.9f, float attack = 0.08f, float decay = 0.25f)
        {
            EnsureRunner();
            pad ??= Gamepad.current;
            if (pad == null) return;
            _runner.StartCoroutine(EngineStartRoutine(pad, key, peak, attack, decay));
        }

        public static void PlayEngineStop(string key = "engine", Gamepad pad = null,
                                          float from = 0.5f, float hold = 0.05f, float release = 0.20f)
        {
            EnsureRunner();
            pad ??= Gamepad.current;
            if (pad == null) return;
            _runner.StartCoroutine(EngineStopRoutine(pad, key, from, hold, release));
        }

        private static IEnumerator EngineStartRoutine(Gamepad pad, string key, float peak, float attack, float decay)
        {

            float t = 0f;
            while (t < attack)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / attack);
                float v = Mathf.Lerp(0f, peak, a);
                SetChannel(key, v * 0.8f, v, pad);
                yield return null;
            }


            t = 0f;
            while (t < decay)
            {
                t += Time.unscaledDeltaTime;
                float d = Mathf.Clamp01(t / decay);
                float v = Mathf.Lerp(peak, 0f, d);
                SetChannel(key, v * 0.8f, v, pad);
                yield return null;
            }

            ClearChannel(key, pad);
        }

        private static IEnumerator EngineStopRoutine(Gamepad pad, string key, float from, float hold, float release)
        {

            SetChannel(key, from * 0.8f, from, pad);
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            float t = 0f;
            while (t < release)
            {
                t += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(from, 0f, Mathf.Clamp01(t / release));
                SetChannel(key, v * 0.8f, v, pad);
                yield return null;
            }

            ClearChannel(key, pad);
        }
#else
        public static bool IsSupported(object _ = null) { Debug.LogWarning("[Haptics] Enable the new Input System."); return false; }
        public static void Rumble(float low, float high, float duration, object _ = null) { }
        public static void Stop(object _ = null) { }
        public static void StopAll() { }
        public static void RumbleAll(float low, float high, float duration) { }
        public static void TriggerPreset(RumblePreset preset, object _ = null) { }
#endif*/
    }
}