using Service;
using UnityEngine;
using UnityEngine.UI;

public class FPSSettingsController : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;

    private ISaveService _saveService;
    private FpsCounter _fpsCounter;

    private void Awake()
    {
        _saveService = ServiceLocator.Get<ISaveService>();
        _fpsCounter = ServiceLocator.Get<FpsCounter>();
    }

    private void OnEnable()
    {
        _toggle.isOn = _saveService.SettingsData.FPSData.IsEnable;
        _toggle.onValueChanged.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnToggle);
    }

    private void OnToggle(bool value)
    {
        _fpsCounter.Toggle(value);
        _saveService.SettingsData.FPSData.IsEnable = value;
    }
}
