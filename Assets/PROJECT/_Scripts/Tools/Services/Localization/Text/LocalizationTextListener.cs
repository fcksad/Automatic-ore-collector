using UnityEngine;
using TMPro;
using Localization;
using Service;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationTextListener : MonoBehaviour
{
    [SerializeField] protected LocalizationConfig _localizationConfig;
    [SerializeField] private TextMeshProUGUI _targetText;

    private LocalizationConfig _boundConfig;
    private ILocalizationService _localizationService;

    private void Awake()
    {
        _localizationService = ServiceLocator.Get<ILocalizationService>();

        if (_localizationConfig == null)
        {
            Debug.LogWarning($"Localization config not found - {gameObject.name}");
            return;
        }

        _localizationService.BindTo(_targetText, _localizationConfig, this);
    }

    public void SetLocal(LocalizationConfig newConfig)
    {
        if (newConfig == _localizationConfig) return;

        UnbindCurrent();
        _localizationConfig = newConfig;
        BindCurrent();
    }

    private void BindCurrent()
    {
        if (_localizationService == null)
        {
            Debug.LogError($"{nameof(LocalizationTextListener)}: ILocalizationService not found.");
            return;
        }
        if (_targetText == null || _localizationConfig == null) return;

        if (_boundConfig == _localizationConfig) return;

        UnbindCurrent();

        _localizationService.BindTo(_targetText, _localizationConfig, this);
        _boundConfig = _localizationConfig;
    }

    private void UnbindCurrent()
    {
        if (_localizationService == null) return;
        if (_targetText == null || _boundConfig == null) return;

        _localizationService.UnbindTo(_targetText, _boundConfig, this);
        _boundConfig = null;
    }

    private void OnDestroy()
    {
        UnbindCurrent();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_targetText == null)
        {
            _targetText = GetComponent<TextMeshProUGUI>();
        }
    }
#endif
}
