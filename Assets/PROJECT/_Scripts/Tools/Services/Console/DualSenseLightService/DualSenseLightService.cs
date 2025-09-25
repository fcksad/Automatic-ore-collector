using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Service
{
    public static class DualSenseLightService
    {

        public static void SetColor(Color color)
        {
#if ENABLE_INPUT_SYSTEM
            // PS5
            if (Gamepad.current is DualSenseGamepadHID ds5)
            {
                ds5.SetLightBarColor(color);
                return;
            }
            // PS4
            if (Gamepad.current is DualShockGamepad ds4)
            {
                ds4.SetLightBarColor(color);
                return;
            }
#endif

        }
    }
}


