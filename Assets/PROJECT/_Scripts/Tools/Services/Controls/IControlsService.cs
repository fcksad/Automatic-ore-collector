using System;
using UnityEngine.InputSystem;

namespace Service
{
    public interface IControlsService
    {
        event Action OnBindingRebindEvent;
        void Rebinding(InputAction action, Guid bindingId);
        void Binding(string name, int bindingIndex, Action value);
        InputActionMap GetFirstActionMap();
    }
}
