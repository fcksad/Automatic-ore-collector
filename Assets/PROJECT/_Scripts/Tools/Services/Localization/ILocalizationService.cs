using Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ILocalizationService 
{
    event Action OnLanguageChangedEvent;

    public Task SetLanguage(string localeCode);
    string CurrentLanguageCode { get; }
    List<string> GetAvailableLanguages();

    string GetLocalizationString(LocalizationConfig config);
    void Subscribe(LocalizationConfig config, Action<string> onChanged, out Action unsubscribe);
    void BindTo(TMPro.TextMeshProUGUI label, LocalizationConfig config, MonoBehaviour owner);
}
