using Localization;
using Service;
using Service.Tweener;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BindingReference
{
    public Control Control;
    public InputAction Action;
    public int BindingIndex;
    public Guid BindingId;

    public BindingReference(Control control, InputAction action, int bindingIndex)
    {
        Control = control;
        Action = action;
        BindingIndex = bindingIndex;
        BindingId = action.bindings[bindingIndex].id;
    }
}

public enum ControlDeviceType
{
    Keyboard,
    Mouse,
    GamepadGeneric,
    PlayStation,
    Xbox,
    NintendoSwitch,
}

[Serializable]
public class ControlPage
{
    [field: SerializeField] public List<ControlDeviceType> Devices;
    [field: SerializeField] public GameObject Parent;
}

public class ControlsController : MonoBehaviour
{
    [SerializeField] private Control _bindingPrefab;
    [SerializeField] private GameObject _waitingForInputScreen;
    [SerializeField] private TextMeshProUGUI _waitingForInputScreenText;
    [SerializeField] private List<ControlPage> _controlPages = new List<ControlPage>();
    [SerializeField] private LocalizationConfig _localizedResetKey;
    [SerializeField] private LocalizationConfig _localizedBindKey;

    private BindingReference _activeRebindRef;
    private Action _cancelRebind;
    private ITweener _rebindTimeoutTween;

    private float _rebindCountdownRemaining;
    private ITweener _countdownTween;

    private InputActionMap _actionMap;

    private List<BindingReference> _bindings = new List<BindingReference>();

    private IControlsService _controlsService;
    private ILocalizationService _localizationService;
    private MessageBoxController _messageBoxController;

    private readonly Dictionary<ControlDeviceType, string> _deviceTags = new()
{
    { ControlDeviceType.Keyboard, "<Keyboard>" },
    { ControlDeviceType.Mouse, "<Mouse>" },
    { ControlDeviceType.GamepadGeneric, "<GamepadGeneric>" },
    { ControlDeviceType.PlayStation, "<PlayStation>" },
    { ControlDeviceType.Xbox, "<Xbox>" },
    { ControlDeviceType.NintendoSwitch, "<NintendoSwitch>" }
};

    private void Awake()
    {
        _controlsService = ServiceLocator.Get<IControlsService>();
        _messageBoxController = ServiceLocator.Get<MessageBoxController>();
        _localizationService = ServiceLocator.Get<ILocalizationService>();

        _actionMap = _controlsService.GetFirstActionMap();
        GenerateBindings();
    }

    private void GenerateBindings()
    {
        foreach (var controlPage in _controlPages)
        {
            foreach (var device in controlPage.Devices)
            {
                if (!_deviceTags.TryGetValue(device, out var deviceTag))
                {
                    Debug.LogWarning($"Device {device} not supported!");
                    continue;
                }

                foreach (var action in _actionMap.actions)
                {
                    if (action.type != InputActionType.Button)
                        continue;

                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        var binding = action.bindings[i];

                        if (binding.isComposite || binding.isPartOfComposite)
                            continue;

                        if (!binding.effectivePath.StartsWith(deviceTag))
                            continue;

                        GameObject newObj = Instantiate(_bindingPrefab.gameObject, controlPage.Parent.transform);
                        Control bindingUI = newObj.GetComponent<Control>();
                        var bindingRef = new BindingReference(bindingUI, action, i);
                        _bindings.Add(bindingRef);

                        UpdateBindingUI(bindingRef);

                        bindingUI.KeyBind.onClick.AddListener(() =>
                        {
                            StartRebind(bindingRef);
                        });

                        bindingUI.ResetButton.onClick.AddListener(() =>
                        {
                            ResetBinding(bindingRef);
                        });
                    }
                }
            }
        }

        CheckConflicts();
    }

    private void StartRebind(BindingReference bindingRef)
    {
        _messageBoxController.ShowYesNo(_localizationService.GetLocalizationString(_localizedBindKey),
                  onYes: () =>
                  {
                      _activeRebindRef = bindingRef;

                      _waitingForInputScreen.SetActive(true);
                      _rebindCountdownRemaining = 20f;

                      bindingRef.Control.KeyAction.text = "Press any key...";
                      UpdateCountdownText();

                      _rebindTimeoutTween?.Kill();
                      _rebindTimeoutTween = TW.Delay(20f, CancelRebind);

                      _countdownTween?.Kill();
                      _countdownTween = TW.Every(1f, 20, CountdownTick);

                      _cancelRebind = _controlsService.Binding(bindingRef.Action, bindingRef.BindingIndex, () =>
                      {
                          CleanupRebindUI();
                          UpdateBindingUI(bindingRef);
                          CheckConflicts();
                      });
                  },
                  autoCloseDelay: 20f
        );
    }

    private void CancelRebind()
    {
        _cancelRebind?.Invoke();
        _cancelRebind = null;

        CleanupRebindUI();

        if (_activeRebindRef != null)
            _activeRebindRef.Control.KeyAction.text = _activeRebindRef.Action.name;

        _activeRebindRef = null;
    }

    private void CleanupRebindUI()
    {
        _rebindTimeoutTween?.Kill();
        _rebindTimeoutTween = null;

        _countdownTween?.Kill();
        _countdownTween = null;

        _waitingForInputScreen.SetActive(false);
        _waitingForInputScreenText.text = "";
    }

    private void ResetBinding(BindingReference bindingRef)
    {
        _messageBoxController.ShowYesNo(_localizationService.GetLocalizationString(_localizedResetKey),
                 onYes: () =>
                 {
                     _controlsService.Rebinding(bindingRef.Action, bindingRef.BindingId);
                     UpdateBindingUI(bindingRef);
                     CheckConflicts();
                 }
                 ,
                  autoCloseDelay: 20f
       );
    }

    private void UpdateBindingUI(BindingReference bindingRef)
    {
        var binding = bindingRef.Action.bindings[bindingRef.BindingIndex];
        bindingRef.Control.KeyAction.text = bindingRef.Action.name; 
        bindingRef.Control.KeyBind.GetComponentInChildren<TextMeshProUGUI>().text = binding.ToDisplayString();
    }

    private void CheckConflicts()
    {
        var usedBindings = new Dictionary<string, List<BindingReference>>();

        foreach (var bindingRef in _bindings)
        {
            var path = bindingRef.Action.bindings[bindingRef.BindingIndex].effectivePath;
            if (string.IsNullOrEmpty(path))
                continue;

            if (!usedBindings.ContainsKey(path))
                usedBindings[path] = new List<BindingReference>();

            usedBindings[path].Add(bindingRef);
        }

        foreach (var bindingRef in _bindings)
        {
            var path = bindingRef.Action.bindings[bindingRef.BindingIndex].effectivePath;
            bool hasConflict = usedBindings.ContainsKey(path) && usedBindings[path].Count > 1;

            var color = bindingRef.Control.Background.color;
            color.a = hasConflict ? 1f : 0f; 
            bindingRef.Control.Background.color = color;
        }
    }

    private void CountdownTick()
    {
        _rebindCountdownRemaining--;
        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (_waitingForInputScreenText != null)
            _waitingForInputScreenText.text = Mathf.CeilToInt(_rebindCountdownRemaining).ToString();
    }
}
