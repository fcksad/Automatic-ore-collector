using System;
using UnityEngine;

namespace Service
{
    public interface IInputService
    {
        void AddActionListener(CharacterAction action, Action onStarted = null, Action onPerformed = null, Action onCanceled = null);
        void RemoveActionListener(CharacterAction action, Action onStarted = null, Action onPerformed = null, Action onCanceled = null);
        bool IsPressed(CharacterAction action);
        public Vector2 GetVector2(CharacterAction action);
        void ChangeInputMap(InputMapType type);
        public string GetActionKey(CharacterAction action, string controlScheme);
    }

}
