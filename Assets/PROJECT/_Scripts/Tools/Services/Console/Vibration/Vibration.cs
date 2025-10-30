using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;

#if UNITY_PS4 && !ENABLE_INPUT_SYSTEM
using UnityEngine.PS4;
#endif

namespace Services.Device
{
    public enum RumblePreset { Light, Medium, Heavy, ShortPulse, LongPulse, DoubleClick, Click }

    public static class Vibration
    {
        private static System.Func<bool> _isEnabled = () => true;
        public static void BindIsEnabled(System.Func<bool> resolver) => _isEnabled = resolver ?? (() => true);

        private class Runner : MonoBehaviour
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

        private static void StartForPad(Gamepad pad, IEnumerator routine)
        {
            EnsureRunner();
            if (pad == null) return;
            if (!_isEnabled()) return;

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

        public static void Rumble(float low, float high, float duration, Gamepad pad = null)
        {
            if (!_isEnabled()) return;
            pad ??= Gamepad.current;
            if (!(pad is IDualMotorRumble) || pad == null) return;

            if (duration <= 0f)
            {
                pad.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high));
                return;
            }

            StartForPad(pad, OneShot(pad, Mathf.Clamp01(low), Mathf.Clamp01(high), duration));
        }

        public static void Stop(Gamepad pad = null)
        {
            pad ??= Gamepad.current;
            if (pad == null) return;

            if (_running.TryGetValue(pad, out var c) && c != null)
            {
                _runner.StopCoroutine(c);
                _running[pad] = null;
            }
            pad.SetMotorSpeeds(0f, 0f);
        }

        public static void StopAll()
        {
            foreach (var p in Gamepad.all)
            {
                Stop(p);
            }
        }

        public static void RumbleAll(float low, float high, float duration)
        {
            if (!_isEnabled()) return;
            foreach (var p in Gamepad.all)
                Rumble(low, high, duration, p);
        }

        public static void TriggerPreset(RumblePreset preset, Gamepad pad = null)
        {
            if (!_isEnabled()) return;
            EnsureRunner();

            pad ??= Gamepad.current;
            if (pad == null && Gamepad.all.Count > 0)
                pad = Gamepad.all[0];
            if (pad == null) { Debug.LogWarning("[Vibration] No gamepad found"); return; }

            switch (preset)
            {
                case RumblePreset.Click: StartForPad(pad, OneShot(pad, .10f, .20f, .10f)); break;
                case RumblePreset.Light: StartForPad(pad, OneShot(pad, .30f, .40f, .20f)); break;
                case RumblePreset.Medium: StartForPad(pad, OneShot(pad, .40f, .70f, .40f)); break;
                case RumblePreset.Heavy: StartForPad(pad, OneShot(pad, .70f, .90f, .90f)); break;
                case RumblePreset.ShortPulse: StartForPad(pad, Pulse(pad, 0.65f, 0.85f, 0.09f, 3, 0.05f)); break;
                case RumblePreset.LongPulse: StartForPad(pad, Pulse(pad, 0.70f, 0.90f, 0.15f, 4, 0.08f)); break;
                case RumblePreset.DoubleClick: StartForPad(pad, DoubleClick(pad)); break;
            }
        }
        private static IEnumerator OneShot(Gamepad pad, float low, float high, float duration)
        {
            pad.SetMotorSpeeds(low, high);
            yield return new WaitForSecondsRealtime(duration);
            pad.SetMotorSpeeds(0f, 0f);
        }

        private static IEnumerator Pulse(Gamepad pad, float low, float high, float on, int count, float off)
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

        private static IEnumerator DoubleClick(Gamepad pad)
        {
            if (pad == null) yield break;
            pad.SetMotorSpeeds(0.75f, 0.95f); yield return new WaitForSecondsRealtime(0.11f);
            pad.SetMotorSpeeds(0f, 0f); yield return new WaitForSecondsRealtime(0.06f);
            pad.SetMotorSpeeds(0.75f, 0.95f); yield return new WaitForSecondsRealtime(0.11f);
            pad.SetMotorSpeeds(0f, 0f);
        }
#else
        public static bool IsSupported(object _ = null) { return false; }
        public static void Rumble(float low, float high, float duration, object _ = null) { }
        public static void Stop(object _ = null) { }
        public static void StopAll() { }
        public static void RumbleAll(float low, float high, float duration) { }
        public static void TriggerPreset(RumblePreset preset, object _ = null) { }
#endif
    }
}
