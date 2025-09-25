using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


// make more ACTION variant
[CreateAssetMenu(fileName = "Action", menuName = "Configs/Input/Action")]
public class InputActionConfigBase : ScriptableObject, IInputAction
{
    [field: SerializeField] public CharacterAction Action { get; private set; }
    [field: SerializeField] public InputActionReference InputReference { get; private set; }

    public Action OnStartedEvent;
    public Action OnPerformedEvent;
    public Action OnCanceledEvent;

    public void Initialize()
    {
        InputReference.action.started += OnStarted;
        InputReference.action.performed += OnPerformed;
        InputReference.action.canceled += OnCanceled;
    }

    public void Cleanup()
    {
        InputReference.action.started -= OnStarted;
        InputReference.action.performed -= OnPerformed;
        InputReference.action.canceled -= OnCanceled;
    }

    private void OnStarted(InputAction.CallbackContext ctx)
    {
        OnStartedEvent?.Invoke();
    }

    private void OnPerformed(InputAction.CallbackContext ctx)
    {
        OnPerformedEvent?.Invoke();
    }

    private void OnCanceled(InputAction.CallbackContext ctx)
    {
        OnCanceledEvent?.Invoke();
    }

    public bool IsPressed()
    {
        return InputReference.action.IsPressed();
    }

    public string GetKeyName(string controlScheme)
    {
        if (InputReference == null || InputReference.action == null)
            return "";

        var binding = InputReference.action.bindings
            .FirstOrDefault(b => b.groups.Contains(controlScheme) && !b.isPartOfComposite);

        if (string.IsNullOrEmpty(binding.effectivePath))
        {
            binding = InputReference.action.bindings.FirstOrDefault(b => !b.isPartOfComposite);
        }

        if (string.IsNullOrEmpty(binding.effectivePath))
            return "";

        int slashIndex = binding.effectivePath.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < binding.effectivePath.Length - 1)
        {
            return binding.effectivePath.Substring(slashIndex + 1).ToLowerInvariant();
        }

        return binding.effectivePath.ToLowerInvariant();
    }
}
