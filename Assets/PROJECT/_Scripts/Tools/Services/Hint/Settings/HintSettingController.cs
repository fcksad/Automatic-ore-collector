using Service;
using UnityEngine;
using UnityEngine.UI;

public class HintSettingController : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;

    private ISaveService _saveService;
    private IHintService _hintService;


    private void Awake()
    {
        _saveService = ServiceLocator.Get<ISaveService>();
        _hintService = ServiceLocator.Get<IHintService>();
    }

    private void OnEnable()
    {
        _toggle.isOn = _saveService.SettingsData.HintData.IsEnable;
        _toggle.onValueChanged.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnToggle);
    }

    private void OnToggle(bool value)
    {
        _hintService.ToggleView(value);
        _saveService.SettingsData.HintData.IsEnable = value;
    }
}
