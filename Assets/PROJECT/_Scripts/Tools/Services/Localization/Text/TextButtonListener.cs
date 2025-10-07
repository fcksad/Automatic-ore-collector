using UnityEngine;
using TMPro;
using Localization;
using Service;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextButtonListener : MonoBehaviour
{
    [SerializeField] protected LocalizationConfig _localizationConfig;
    [SerializeField] private TextMeshProUGUI _targetText;

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

    private void OnValidate()
    {
        if (_targetText == null)
        {
            _targetText = GetComponent<TextMeshProUGUI>();
        }
    }
}
