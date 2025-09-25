using Localization;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

[CreateAssetMenu(fileName = "DialogueConfig", menuName = "Configs/Service/Dialogue/Dialogue Text")]
public class DialogueConfig : ScriptableObject
{
    [field: SerializeField] public string TextNameID {  get; private set; }
    [SerializeField] private List<LocalizationConfig> _localizationText = new List<LocalizationConfig>();

    [SerializeField] private List<string> _debug = new List<string>(); 

    public async Task<List<string>> GetLocalizedLinesAsync()
    {
        var result = new List<string>();

        foreach (var locStr in _localizationText)
        {
            if (locStr != null)
            {
                var handle = locStr.LocalizedString.GetLocalizedStringAsync();
                await handle.Task;
                result.Add(handle.Result);
            }
        }

        return result;
    }

#if UNITY_EDITOR

    private async void OnValidate()
    {
        if(_localizationText == null || _localizationText.Count == 0)
        return;

        var selectedLocale = LocalizationSettings.SelectedLocale;
        var englishLocale = LocalizationSettings.AvailableLocales.GetLocale("en");

        if (englishLocale == null)
        {
            Debug.LogWarning("English locale ('en') not found.");
            return;
        }

        LocalizationSettings.SelectedLocale = englishLocale;

        _debug.Clear();

        foreach (var locStr in _localizationText)
        {
            if (locStr == null)
            {
                _debug.Add("[NULL]");
                continue;
            }

            var handle = locStr.LocalizedString.GetLocalizedStringAsync();
            await handle.Task;
            _debug.Add(handle.Result);
        }

        LocalizationSettings.SelectedLocale = selectedLocale;

        EditorUtility.SetDirty(this);
    }

#endif
}
