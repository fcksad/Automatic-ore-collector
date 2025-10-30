using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using Service;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

namespace Localization
{
    public class LocalizationService : ILocalizationService, IInitializable, Service.IDisposable
    {
        public event Action OnLanguageChangedEvent;
        private readonly Dictionary<TextMeshProUGUI, Action> _bindings = new();

        private ISaveService _saveService;

        public LocalizationService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void Initialize()
        {
            LoadLanguage();
        }

        public void Dispose()
        {
            foreach (var kvp in _bindings)
                kvp.Value?.Invoke();
            _bindings.Clear();

            OnLanguageChangedEvent = null;
        }

        public async Task SetLanguage(string localeCode)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            var targetLocale = locales.Find(locale => locale.Identifier.Code == localeCode);

            if (targetLocale != null)
            {
                LocalizationSettings.SelectedLocale = targetLocale;
                OnLanguageChangedEvent?.Invoke();
            }

            await LocalizationSettings.InitializationOperation.Task;
            SaveLanguage(localeCode);
        }

        public string CurrentLanguageCode => LocalizationSettings.SelectedLocale.Identifier.Code;

        public List<string> GetAvailableLanguages()
        {
            var languages = new List<string>();

            var locales = LocalizationSettings.AvailableLocales.Locales;
            foreach (var locale in locales)
            {
                languages.Add(locale.Identifier.Code); 
            }

            return languages;
        }

        private void SaveLanguage(string localeCode) => _saveService.SettingsData.LocalizationData.Localization = localeCode;
        private async void LoadLanguage() => await SetLanguage(_saveService.SettingsData.LocalizationData.Localization);

        public string GetLocalizationString(LocalizationConfig config)
        {
            var op = config.LocalizedString.GetLocalizedStringAsync();
            return op.IsDone ? op.Result : string.Empty;
        }

        public void Subscribe(LocalizationConfig config, Action<string> onChanged, out Action unsubscribe)
        {
            var localizedString = config.LocalizedString;

            var handler = new LocalizedString.ChangeHandler(onChanged);
            localizedString.StringChanged += handler;
            localizedString.RefreshString();

            void onLangChanged() => localizedString.RefreshString();
            OnLanguageChangedEvent += onLangChanged;

            unsubscribe = () =>
            {
                localizedString.StringChanged -= handler;
                OnLanguageChangedEvent -= onLangChanged;
            };
        }
        public void BindTo(TextMeshProUGUI label, LocalizationConfig config, MonoBehaviour owner)
        {
            Subscribe(config, value => label.text = value, out var unsubscribe);

            if (owner != null)
            {
                void Cleanup() => unsubscribe?.Invoke();

                owner.StartCoroutine(WaitForDestroy(owner, Cleanup));
            }
        }

        public void UnbindTo(TextMeshProUGUI label, LocalizationConfig config, MonoBehaviour owner)
        {
            if (_bindings.TryGetValue(label, out var unsubscribe))
            {
                unsubscribe?.Invoke();
                _bindings.Remove(label);
            }
        }


        private System.Collections.IEnumerator WaitForDestroy(MonoBehaviour owner, Action onDestroyed)
        {
            while (owner != null)
                yield return null;

            onDestroyed?.Invoke();
        }
    }
}
