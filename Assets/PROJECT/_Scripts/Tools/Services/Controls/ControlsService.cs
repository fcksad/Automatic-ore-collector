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

        private InputActionRebindingExtensions.RebindingOperation _rebinding;
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

        public Action Binding(InputAction action, int bindingIndex, Action onComplete = null)
        {
            if (action == null)
            {
                Debug.LogError("BeginRebind: action is null");
                return null;
            }

            CancelActiveRebind();

            action.Disable();

            _rebinding = action.PerformInteractiveRebinding(bindingIndex).OnComplete(rebinding => 
            {
                try
                {
                    action.Enable();
                    SaveBinding(action, bindingIndex);
                    OnBindingRebindEvent?.Invoke();
                    onComplete?.Invoke();
                }
                finally
               {
                    rebinding.Dispose();
                    if (_rebinding == rebinding) _rebinding = null;
                }
            })
            .OnCancel(rebinding =>
            {
            try
            {
                action.Enable();
            }
            finally
            {
                rebinding.Dispose();
                if (_rebinding == rebinding) _rebinding = null;
            }
        });

            _rebinding.Start();

             return () =>
            {
                if (_rebinding != null)
                {
                    try { _rebinding.Cancel(); } catch { /* ignore */ }

                }
            };

            /*            action.Disable();

                        action.PerformInteractiveRebinding(bindingIndex)
                            .OnComplete(operation =>
                            {
                                action.Enable();
                                SaveBinding(action, bindingIndex);
                                OnBindingRebindEvent?.Invoke();
                                onComplete?.Invoke();
                            })
                            .Start();*/
        }

        public void CancelActiveRebind()
        {
            if (_rebinding != null)
            {
                try { _rebinding.Cancel(); } catch {  }

            }
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
            CancelActiveRebind();

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


