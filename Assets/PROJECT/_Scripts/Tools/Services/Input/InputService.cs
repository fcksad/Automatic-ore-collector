using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Service
{
    public class InputService : IInputService, IInitializable, IDisposable
    {
        private PlayerInput _playerInput;
        private List<InputActionConfigBase> _inputActions = new List<InputActionConfigBase>();
        private Dictionary<CharacterAction, InputActionConfigBase> _actionDictionary = new Dictionary<CharacterAction, InputActionConfigBase>();

        public InputService(PlayerInput playerInput)
        {
            _playerInput = playerInput;
        }

        public void Initialize()
        {
            _inputActions = ResourceLoader.GetAll<InputActionConfigBase>();

            foreach (var action in _inputActions)
            {
                if (action is IInputAction inputAction)
                {
                    inputAction.Initialize();
                }

                if (!_actionDictionary.ContainsKey(action.Action))
                {
                    _actionDictionary[action.Action] = action;
                }
            }
        }

        public void AddActionListener(CharacterAction action, Action onStarted = null, Action onPerformed = null, Action onCanceled = null)
        {
            if (_actionDictionary.TryGetValue(action, out var config))
            {
                if (onStarted != null) config.OnStartedEvent += onStarted;
                if (onPerformed != null) config.OnPerformedEvent += onPerformed;
                if (onCanceled != null) config.OnCanceledEvent += onCanceled;
            }
        }

        public void RemoveActionListener(CharacterAction action, Action onStarted = null, Action onPerformed = null, Action onCanceled = null)
        {
            if (_actionDictionary.TryGetValue(action, out var config))
            {
                if (onStarted != null) config.OnStartedEvent -= onStarted;
                if (onPerformed != null) config.OnPerformedEvent -= onPerformed;
                if (onCanceled != null) config.OnCanceledEvent -= onCanceled;
            }
        }

        public Vector2 GetVector2(CharacterAction action)
        {
            if (_actionDictionary.TryGetValue(action, out var config))
            {
                return config.InputReference.action.ReadValue<Vector2>();
            }

            return Vector2.zero;
        }

        public float GetAxis(CharacterAction action)
        {
            if (_actionDictionary.TryGetValue(action, out var config))
            {
                return config.InputReference.action.ReadValue<float>();
            }

            return 0f;
        }

        public float GetAxisFromButtons(CharacterAction negative, CharacterAction positive)
        {
            bool neg = IsPressed(negative);
            bool pos = IsPressed(positive);
            if (pos == neg) return 0f;
            return pos ? 1f : -1f;
        }

        public bool IsPressed(CharacterAction action)
        {
            return _actionDictionary.TryGetValue(action, out var config) && config.IsPressed();
        }

        public string GetActionKey(CharacterAction action, string controlScheme)
        {
            return _actionDictionary.TryGetValue(action, out var config) ? config.GetKeyName(controlScheme) : "";
        }

        public void ChangeInputMap(InputMapType type)
        {
            foreach (var map in _playerInput.actions.actionMaps)
            {
                map.Disable();
            }

            var targetMap = _playerInput.actions.FindActionMap(type.ToString(), true);
            if (targetMap != null)
            {
                targetMap.Enable();
                Debug.Log($"Activated Input Map: {type}");
            }
            else
            {
                Debug.LogWarning($"InputActionMap with name {type} not found.");
            }
        }

        public void Dispose()
        {
            foreach (var action in _inputActions)
            {
                if (action is IInputAction inputAction)
                {
                    inputAction.Cleanup();
                }
            }

            _actionDictionary.Clear();
        }
    }

}
