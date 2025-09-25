using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Service
{
    /// <summary>
    /// USe on input version 1.11.2
    /// </summary>
    public class ControlsService : IControlsService, IInitializable
    {
        private PlayerInput _playerInput;
        private ISaveService _saveService;

        public event Action OnBindingRebindEvent;

        public ControlsService(ISaveService saveService, PlayerInput playerInput)
        {
            _saveService = saveService;
            _playerInput = playerInput;
        }

        public void Initialize()
        {
            LoadBindings();
#if UNITY_EDITOR
            string inputVersion = InputSystem.version.ToString();
            if (inputVersion != "1.11.2" && inputVersion != "1.11.2") 
            {
                Debug.LogError($"Input System version {inputVersion} is not supported. Use version 1.11.2 for full compatibility.");
            }
#endif
        }

        public void Binding(string actionName, int bindingIndex, Action onComplete = null)
        {
            var action = _playerInput.actions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogError($"Action {actionName} not found!");
                return;
            }

            action.Disable();

            action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(operation =>
                {
                    action.Enable();
                    SaveBinding(action, bindingIndex);
                    OnBindingRebindEvent?.Invoke();
                    onComplete?.Invoke();
                })
                .Start();
        }

        public void SaveBinding(InputAction action, int bindingIndex)
        {
            string key = $"{action.name}_{action.bindings[bindingIndex].id}";
            string value = action.bindings[bindingIndex].overridePath ?? action.bindings[bindingIndex].effectivePath;

            _saveService.SettingsData.ControlsData.ControlData[key] = value;
        }

        public void Rebinding(InputAction action, Guid bindingId)
        {
            int bindingIndex = -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].id == bindingId)
                {
                    bindingIndex = i;
                    break;
                }
            }

            if (bindingIndex >= 0)
            {
                action.RemoveBindingOverride(bindingIndex);
                Save();
            }

            OnBindingRebindEvent.Invoke();
        }

        public InputActionMap GetFirstActionMap()
        {
            return _playerInput.actions.actionMaps[0];
        }

        public void Save()
        {
            var controlData = _saveService.SettingsData.ControlsData.ControlData;
            controlData.Clear();

            foreach (var action in _playerInput.actions)
            {
                foreach (var binding in action.bindings)
                {
                    if (binding.isComposite || binding.isPartOfComposite)
                        continue;

                    string key = $"{action.name}_{binding.id}";
                    string value = binding.overridePath ?? binding.effectivePath;

                    controlData[key] = value;
                }
            }

            _saveService.SettingsData.ControlsData.ControlData = controlData;
        }

        public void LoadBindings()
        {
            var controlData = _saveService.SettingsData.ControlsData.ControlData;
            if (controlData == null || controlData.Count == 0)
                return;

            foreach (var action in _playerInput.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];

                    if (binding.isComposite || binding.isPartOfComposite)
                        continue;

                    string key = $"{action.name}_{binding.id}";
                    if (controlData.TryGetValue(key, out var path))
                    {
                        action.ApplyBindingOverride(i, path);
                    }
                }
            }
        }
    }
}


