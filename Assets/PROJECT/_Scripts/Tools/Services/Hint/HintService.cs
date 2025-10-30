using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

namespace Service
{
    public class HintService : IHintService, IInitializable, IDisposable
    {
        private HintView _hintView;
        private PlayerInput _playerInput;
        private IInputService _inputService;
        private ISaveService _saveService;
        private IControlsService _controlsService;

        private readonly Dictionary<string, List<string>> _cachedKeys = new();

        public HintService(HintView hintView, IInputService inputService, PlayerInput playerInput, ISaveService saveService, IControlsService controlsService)
        {
            _hintView = hintView;
            _inputService = inputService;
            _playerInput = playerInput;
            _saveService = saveService;
            _controlsService = controlsService;
        }

        public void Initialize() 
        {
            ToggleView(_saveService.SettingsData.HintData.IsEnable);
            _controlsService.OnBindingRebindEvent += HideAll;
        }

        public void Dispose()
        {
            _controlsService.OnBindingRebindEvent -= HideAll;
        }

        public void ShowHint(string localizationAction, List<CharacterAction> actions)
        {
            var controlScheme = _playerInput.currentControlScheme;
            var deviceType = GetDeviceType(controlScheme);
            string cacheKey = $"{localizationAction}_{deviceType}";

            if (!_cachedKeys.TryGetValue(cacheKey, out var keys))
            {
                keys = new List<string>(actions.Count);
                foreach (var action in actions)
                {
                    keys.Add(_inputService.GetActionKey(action, controlScheme));
                }

                _cachedKeys[cacheKey] = keys;
            }

            _hintView.Show(localizationAction, actions, keys, deviceType);
        }

        public void HideHint(string localizationAction)
        {
            _hintView.Hide(localizationAction);
        }

        public void HideAll()
        {
            _cachedKeys.Clear();
            _hintView.HideAll();
        }

        private ControlDeviceType DetectGamepadType()
        {
            var pad = Gamepad.current;
            if (pad == null) return ControlDeviceType.Keyboard;

            if (pad is DualSenseGamepadHID || pad is DualShockGamepad) return ControlDeviceType.PlayStation;
            if (pad is XInputController) return ControlDeviceType.Xbox;
            if (pad is SwitchProControllerHID) return ControlDeviceType.NintendoSwitch;

            var d = pad.description;
            string m = (d.manufacturer ?? "").ToLowerInvariant();
            string p = (d.product ?? "").ToLowerInvariant();
            string caps = d.capabilities ?? "";

            if (m.Contains("sony") || p.Contains("dualsense") || p.Contains("dualshock") || caps.Contains("\"vendorId\":1356"))
                return ControlDeviceType.PlayStation;
            if (m.Contains("microsoft") || p.Contains("xbox") || caps.Contains("\"vendorId\":1118"))
                return ControlDeviceType.Xbox;
            if (m.Contains("nintendo") || p.Contains("switch") || p.Contains("joy-con") || p.Contains("pro controller") || caps.Contains("\"vendorId\":1406"))
                return ControlDeviceType.NintendoSwitch;

            return ControlDeviceType.GamepadGeneric;
        }

        private ControlDeviceType GetDeviceType(string controlScheme)
        {
            switch (controlScheme)
            {
                case "Keyboard&Mouse":
                case "Keyboard&MouseScheme":
                case "KeyboardMouse":
                    return ControlDeviceType.Keyboard;

                case "PlayStation":
                case "PS4":
                case "PS5":
                    return ControlDeviceType.PlayStation;

                case "Xbox":
                case "XInput":
                    return ControlDeviceType.Xbox;

                case "Switch":
                case "Nintendo":
                    return ControlDeviceType.NintendoSwitch;

                case "Gamepad":
                case "GamepadScheme":
                default:
                    return DetectGamepadType();
            }
        }

        public void ToggleView(bool value)
        {
            _hintView.Toggle(value);
        }

        public Sprite GetHintSprite(CharacterAction action)
        {
            if (_hintView == null || _inputService == null || _playerInput == null)
                return null;

            var scheme = _playerInput.currentControlScheme;
            var device = GetDeviceType(scheme);

            string controlButton = _inputService.GetActionKey(action, scheme);
            if (string.IsNullOrEmpty(controlButton))
                return null;
            var spr = _hintView.GetSprite(device, controlButton);
            return spr;
        }
    }
}
   
