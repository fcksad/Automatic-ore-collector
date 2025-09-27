using System;
using UnityEngine.InputSystem;

namespace Service
{
    public interface IControlsService
    {
        event Action OnBindingRebindEvent;
        void Rebinding(InputAction action, Guid bindingId);
        Action Binding(InputAction action, int bindingIndex, Action onComplete = null);
        InputActionMap GetFirstActionMap();
    }
}
