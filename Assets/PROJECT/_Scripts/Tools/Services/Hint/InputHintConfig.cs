using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using static InputHintConfig.InputHintEntry;

[CreateAssetMenu(fileName = "InputHintConfig", menuName = "Configs/Input/Input Hint Config")]
public class InputHintConfig : ScriptableObject
{
    [Serializable]
    public class InputHintEntry
    {
        [field: SerializeField] public ControlDeviceType ControlDeviceType { get; set; }
        [field: SerializeField] public List<HintInputView> HintInputViews { get; set; } = new();

        [Serializable]
        public class HintInputView
        {
            [field: SerializeField] public string ControlName { get; set; }
            [field: SerializeField] public string ControlButton { get; set; }
            [field: SerializeField, SpritePreview] public Sprite Icon { get; set; }
            [field: SerializeField] public List<string> Aliases { get; set; } = new();
        }
    }

    [SerializeField] private List<InputHintEntry> _hintEntries = new();
    private Dictionary<(ControlDeviceType, string), HintInputView> _hintDictionary;

    public void Initialize()
    {
        _hintDictionary = new();

        foreach (var entry in _hintEntries)
        {
            foreach (var hint in entry.HintInputViews)
            {
                string key = Normalize(hint.ControlButton);
                _hintDictionary[(entry.ControlDeviceType, key)] = hint;

                if (hint.Aliases != null)
                {
                    foreach (var a in hint.Aliases)
                    {
                        string ak = Normalize(a);
                        _hintDictionary[(entry.ControlDeviceType, ak)] = hint;
                    }
                }
            }
        }
    }

    private static string Normalize(string s) => (s ?? string.Empty).ToLowerInvariant().Trim();


    public HintInputView GetHint(ControlDeviceType deviceType, string controlButton)
    {
        if (_hintDictionary == null)
            Initialize();

        if (string.IsNullOrEmpty(controlButton))
            return null;

        var key = (deviceType, controlButton.ToLowerInvariant());
        _hintDictionary.TryGetValue(key, out var view);
        return view;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill All Common Hints")]
    private void AutoFillDefaults()
    {

        AutoFillKeyboard();
        AutoFillGamepadGeneric();
        AutoFillPlayStation();
        AutoFillXbox();
        AutoFillSwitch();
        Debug.Log("InputHintConfig: auto-filled Keyboard + Generic + PS + Xbox + Switch.");
    }

    private InputHintEntry GetOrCreateEntry(ControlDeviceType type)
    {
        var entry = _hintEntries.Find(e => e.ControlDeviceType == type);
        if (entry == null)
        {
            entry = new InputHintEntry { ControlDeviceType = type };
            _hintEntries.Add(entry);
        }
        return entry;
    }

    private void AddOrMergeHint(InputHintEntry entry, HintInputView hint)
    {
        string key = (hint.ControlButton ?? string.Empty).ToLowerInvariant();
        var existing = entry.HintInputViews.Find(h =>
            string.Equals(h.ControlButton ?? string.Empty, key, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {

            hint.ControlButton = key;
            entry.HintInputViews.Add(hint);
            return;
        }


        if (existing.Icon == null && hint.Icon != null)
            existing.Icon = hint.Icon;

        if (string.IsNullOrEmpty(existing.ControlName) && !string.IsNullOrEmpty(hint.ControlName))
            existing.ControlName = hint.ControlName;


        if (hint.Aliases != null)
        {
            foreach (var a in hint.Aliases)
            {
                var la = (a ?? "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(la) && !existing.Aliases.Contains(la))
                    existing.Aliases.Add(la);
            }
        }
    }

    private void AddSimpleButtons(ControlDeviceType type, IEnumerable<string> buttons)
    {
        var entry = GetOrCreateEntry(type);
#if UNITY_EDITOR
        Undo.RecordObject(this, $"Add Input Hints [{type}]");
#endif
        foreach (var raw in buttons)
        {
            var key = (raw ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(key)) continue;

            var name = char.ToUpper(key[0]) + (key.Length > 1 ? key.Substring(1) : "");
            AddOrMergeHint(entry, new InputHintEntry.HintInputView
            {
                ControlButton = key,
                ControlName = name,
                Icon = null
            });
        }
    }

    private void AddBtn(ControlDeviceType type, string name, string key, Sprite icon = null, params string[] aliases)
    {
        var entry = GetOrCreateEntry(type);
#if UNITY_EDITOR
        Undo.RecordObject(this, $"Add Input Hint [{type}:{key}]");
#endif
        AddOrMergeHint(entry, new InputHintEntry.HintInputView
        {
            ControlName = name,
            ControlButton = (key ?? "").ToLowerInvariant(),
            Icon = icon,
            Aliases = aliases == null ? new List<string>() :
                      new List<string>(Array.ConvertAll(aliases, a => (a ?? "").ToLowerInvariant()))
        });
    }

    private void AutoFillKeyboard()
    {
        AddSimpleButtons(ControlDeviceType.Keyboard, new[]{
        "q","w","e","r","t","y","u","i","o","p",
        "a","s","d","f","g","h","j","k","l",
        "z","x","c","v","b","n","m",
        "1","2","3","4","5","6","7","8","9","0",
        "space","enter","tab","escape","backspace",
        "leftshift","rightshift","leftctrl","rightctrl","leftalt","rightalt",
        "uparrow","downarrow","leftarrow","rightarrow",
        "f1","f2","f3","f4","f5","f6","f7","f8","f9","f10","f11","f12",
        "leftbutton","rightbutton","middlebutton"
    });
    }

    private void AutoFillGamepadGeneric()
    {
        AddBtn(ControlDeviceType.GamepadGeneric, "South", "buttonsouth", null, "a", "cross");
        AddBtn(ControlDeviceType.GamepadGeneric, "East", "buttoneast", null, "b", "circle");
        AddBtn(ControlDeviceType.GamepadGeneric, "North", "buttonnorth", null, "y", "triangle");
        AddBtn(ControlDeviceType.GamepadGeneric, "West", "buttonwest", null, "x", "square");
        AddBtn(ControlDeviceType.GamepadGeneric, "LB", "leftshoulder", null, "l1");
        AddBtn(ControlDeviceType.GamepadGeneric, "RB", "rightshoulder", null, "r1");
        AddBtn(ControlDeviceType.GamepadGeneric, "LT", "lefttrigger", null, "l2");
        AddBtn(ControlDeviceType.GamepadGeneric, "RT", "righttrigger", null, "r2");
        AddBtn(ControlDeviceType.GamepadGeneric, "View", "select", null, "share", "minus");
        AddBtn(ControlDeviceType.GamepadGeneric, "Menu", "start", null, "options", "plus");
        AddBtn(ControlDeviceType.GamepadGeneric, "L3", "leftstickpress", null, "l3");
        AddBtn(ControlDeviceType.GamepadGeneric, "R3", "rightstickpress", null, "r3");
        AddBtn(ControlDeviceType.GamepadGeneric, "D-Up", "up");
        AddBtn(ControlDeviceType.GamepadGeneric, "D-Down", "down");
        AddBtn(ControlDeviceType.GamepadGeneric, "D-Left", "left");
        AddBtn(ControlDeviceType.GamepadGeneric, "D-Right", "right");

        AddBtn(ControlDeviceType.GamepadGeneric, "Left Stick", "leftstick", null, "lstick");
        AddBtn(ControlDeviceType.GamepadGeneric, "Right Stick", "rightstick", null, "rstick");
        AddBtn(ControlDeviceType.GamepadGeneric, "D-Pad", "dpad", null, "hat");

        AddBtn(ControlDeviceType.GamepadGeneric, "LS X", "leftstickx", null, "x");
        AddBtn(ControlDeviceType.GamepadGeneric, "LS Y", "leftsticky", null, "y");
        AddBtn(ControlDeviceType.GamepadGeneric, "RS X", "rightstickx");
        AddBtn(ControlDeviceType.GamepadGeneric, "RS Y", "rightsticky");
    }

    private void AutoFillPlayStation()
    {
        AddBtn(ControlDeviceType.PlayStation, "Cross", "cross", null, "buttonsouth", "a");
        AddBtn(ControlDeviceType.PlayStation, "Circle", "circle", null, "buttoneast", "b");
        AddBtn(ControlDeviceType.PlayStation, "Triangle", "triangle", null, "buttonnorth");
        AddBtn(ControlDeviceType.PlayStation, "Square", "square", null, "buttonwest");
        AddBtn(ControlDeviceType.PlayStation, "L1", "l1", null, "leftshoulder");
        AddBtn(ControlDeviceType.PlayStation, "R1", "r1", null, "rightshoulder");
        AddBtn(ControlDeviceType.PlayStation, "L2", "l2", null, "lefttrigger");
        AddBtn(ControlDeviceType.PlayStation, "R2", "r2", null, "righttrigger");
        AddBtn(ControlDeviceType.PlayStation, "Options", "options", null, "start");
        AddBtn(ControlDeviceType.PlayStation, "Share", "share", null, "select", "create");
        AddBtn(ControlDeviceType.PlayStation, "LS X", "leftstickx", null, "x");
        AddBtn(ControlDeviceType.PlayStation, "LS Y", "leftsticky", null, "y");
        AddBtn(ControlDeviceType.PlayStation, "RS X", "rightstickx", null);
        AddBtn(ControlDeviceType.PlayStation, "RS Y", "rightsticky", null);
        AddBtn(ControlDeviceType.PlayStation, "Left Stick", "leftstick", null, "lstick");
        AddBtn(ControlDeviceType.PlayStation, "Right Stick", "rightstick", null, "rstick");
        AddBtn(ControlDeviceType.PlayStation, "L3", "l3", null, "leftstickpress");
        AddBtn(ControlDeviceType.PlayStation, "R3", "r3", null, "rightstickpress");
        AddBtn(ControlDeviceType.PlayStation, "Touchpad", "touchpad", null, "touchpadpress");
        AddBtn(ControlDeviceType.PlayStation, "D-Pad", "dpad", null, "hat", "dpadall");
        AddBtn(ControlDeviceType.PlayStation, "D-Up", "up");
        AddBtn(ControlDeviceType.PlayStation, "D-Down", "down");
        AddBtn(ControlDeviceType.PlayStation, "D-Left", "left");
        AddBtn(ControlDeviceType.PlayStation, "D-Right", "right");
    }

    private void AutoFillXbox()
    {
        AddBtn(ControlDeviceType.Xbox, "A", "buttonsouth", null, "buttonsouth");
        AddBtn(ControlDeviceType.Xbox, "B", "buttoneast", null, "buttoneast");
        AddBtn(ControlDeviceType.Xbox, "Y", "buttonnorth", null, "buttonnorth");
        AddBtn(ControlDeviceType.Xbox, "X", "buttonwest", null, "buttonwest");
        AddBtn(ControlDeviceType.Xbox, "LB", "lb", null, "leftshoulder");
        AddBtn(ControlDeviceType.Xbox, "RB", "rb", null, "rightshoulder");
        AddBtn(ControlDeviceType.Xbox, "LT", "lt", null, "lefttrigger");
        AddBtn(ControlDeviceType.Xbox, "RT", "rt", null, "righttrigger");
        AddBtn(ControlDeviceType.Xbox, "Menu", "menu", null, "start");
        AddBtn(ControlDeviceType.Xbox, "View", "view", null, "select");
        AddBtn(ControlDeviceType.Xbox, "Left Stick", "leftstick", null, "lstick");
        AddBtn(ControlDeviceType.Xbox, "Right Stick", "rightstick", null, "rstick");
        AddBtn(ControlDeviceType.Xbox, "LS X", "leftstickx", null, "x");
        AddBtn(ControlDeviceType.Xbox, "LS Y", "leftsticky", null, "y");
        AddBtn(ControlDeviceType.Xbox, "RS X", "rightstickx", null);
        AddBtn(ControlDeviceType.Xbox, "RS Y", "rightsticky", null);
        AddBtn(ControlDeviceType.Xbox, "L3", "l3", null, "leftstickpress");
        AddBtn(ControlDeviceType.Xbox, "R3", "r3", null, "rightstickpress");
        AddBtn(ControlDeviceType.Xbox, "D-Pad", "dpad", null, "hat");
        AddBtn(ControlDeviceType.Xbox, "D-Up", "up");
        AddBtn(ControlDeviceType.Xbox, "D-Down", "down");
        AddBtn(ControlDeviceType.Xbox, "D-Left", "left");
        AddBtn(ControlDeviceType.Xbox, "D-Right", "right");
    }

    private void AutoFillSwitch()
    {
        AddBtn(ControlDeviceType.NintendoSwitch, "A", "a", null, "buttoneast", "buttonsouth");
        AddBtn(ControlDeviceType.NintendoSwitch, "B", "b", null, "buttonsouth", "buttoneast");
        AddBtn(ControlDeviceType.NintendoSwitch, "X", "buttonnorth", null, "buttonnorth");
        AddBtn(ControlDeviceType.NintendoSwitch, "Y", "buttonwest", null, "buttonwest");
        AddBtn(ControlDeviceType.NintendoSwitch, "L", "l", null, "leftshoulder");
        AddBtn(ControlDeviceType.NintendoSwitch, "R", "r", null, "rightshoulder");
        AddBtn(ControlDeviceType.NintendoSwitch, "ZL", "zl", null, "lefttrigger");
        AddBtn(ControlDeviceType.NintendoSwitch, "ZR", "zr", null, "righttrigger");
        AddBtn(ControlDeviceType.NintendoSwitch, "+", "plus", null, "start");
        AddBtn(ControlDeviceType.NintendoSwitch, "?", "minus", null, "select");
        AddBtn(ControlDeviceType.NintendoSwitch, "Left Stick", "leftstick", null, "lstick");
        AddBtn(ControlDeviceType.NintendoSwitch, "Right Stick", "rightstick", null, "rstick");
        AddBtn(ControlDeviceType.NintendoSwitch, "LS X", "leftstickx", null, "x");
        AddBtn(ControlDeviceType.NintendoSwitch, "LS Y", "leftsticky", null, "y");
        AddBtn(ControlDeviceType.NintendoSwitch, "RS X", "rightstickx", null);
        AddBtn(ControlDeviceType.NintendoSwitch, "RS Y", "rightsticky", null);
        AddBtn(ControlDeviceType.NintendoSwitch, "L-Stick", "l3", null, "leftstickpress");
        AddBtn(ControlDeviceType.NintendoSwitch, "R-Stick", "r3", null, "rightstickpress");
        AddBtn(ControlDeviceType.NintendoSwitch, "D-Pad", "dpad", null, "hat");
        AddBtn(ControlDeviceType.NintendoSwitch, "D-Up", "up");
        AddBtn(ControlDeviceType.NintendoSwitch, "D-Down", "down");
        AddBtn(ControlDeviceType.NintendoSwitch, "D-Left", "left");
        AddBtn(ControlDeviceType.NintendoSwitch, "D-Right", "right");
    }
#endif
}