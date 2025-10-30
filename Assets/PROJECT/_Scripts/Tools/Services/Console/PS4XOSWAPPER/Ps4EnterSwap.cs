// PS4 only
#if UNITY_PS4 && !UNITY_EDITOR
using UnityEngine.PS4;
#endif
using UnityEngine.InputSystem;

public static class Ps4EnterSwap
{
    public static void Apply(InputActionAsset actions)
    {
        if (actions == null) return;

#if UNITY_PS4 && !UNITY_EDITOR
    bool crossIsEnter =
        UnityEngine.PS4.Utility.GetSystemServiceParam(
            UnityEngine.PS4.Utility.SystemServiceParamId.EnterButtonAssign) == 1; // 1: Cross=Enter, 0: Circle=Enter
#else
        bool crossIsEnter = true; 
#endif

        var ui = actions.FindActionMap("UI", throwIfNotFound: false);
        if (ui != null)
        {
            var submit = ui.FindAction("Submit", throwIfNotFound: false);
            var cancel = ui.FindAction("Cancel", throwIfNotFound: false);

            if (submit != null && cancel != null)
            {
                if (crossIsEnter)
                {

                    SetGamepadButton(submit, "<Gamepad>/buttonSouth"); 
                    SetGamepadButton(cancel, "<Gamepad>/buttonEast");  
                }
                else
                {
                    SetGamepadButton(submit, "<Gamepad>/buttonEast");  
                    SetGamepadButton(cancel, "<Gamepad>/buttonSouth"); 
                }
            }
        }


        var player = actions.FindActionMap("Player", throwIfNotFound: false);
        if (player != null)
        {
            var cross = player.FindAction("Cross", throwIfNotFound: false);
            var circle = player.FindAction("Circle", throwIfNotFound: false);

            if (cross != null && circle != null)
            {
                if (crossIsEnter)
                {
                    SetGamepadButton(cross, "<Gamepad>/buttonSouth");
                    SetGamepadButton(circle, "<Gamepad>/buttonEast");
                }
                else
                {
                    SetGamepadButton(cross, "<Gamepad>/buttonEast");
                    SetGamepadButton(circle, "<Gamepad>/buttonSouth");
                }
            }
        }
    }

    static void SetGamepadButton(InputAction action, string path)
    {
        if (action == null) return;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite) continue;

            bool isGamepad =
                (!string.IsNullOrEmpty(b.groups) && b.groups.Contains("Gamepad")) ||
                (!string.IsNullOrEmpty(b.path) && (b.path.Contains("<Gamepad>")
                                                 || b.path.Contains("<DualShockGamepadHID>")
                                                 || b.path.Contains("<DualSenseGamepad>"))) ||
                (!string.IsNullOrEmpty(b.effectivePath) && (b.effectivePath.Contains("Gamepad")
                                                         || b.effectivePath.Contains("DualShock")
                                                         || b.effectivePath.Contains("DualSense")));

            if (!isGamepad) continue;

            action.ApplyBindingOverride(i, new InputBinding { overridePath = path });
        }
    }
}
