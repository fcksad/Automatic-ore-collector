using Service;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Settings
{
    public class QualityControl : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _qualityLevelDropdown;

        private List<string> _qualityLevels;

        private IGraphicsService _graphicsService;

        private void Awake()
        {
            _graphicsService = ServiceLocator.Get<IGraphicsService>();

            string[] qualityNames = QualitySettings.names;
            _qualityLevels = new List<string>(qualityNames);


            _qualityLevelDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            foreach (string qualityLevel in _qualityLevels)
            {
                options.Add(new TMP_Dropdown.OptionData(qualityLevel));
            }

            _qualityLevelDropdown.AddOptions(options);
            _qualityLevelDropdown.value = _graphicsService.Get(GraphicType.QualityLevel);
            _qualityLevelDropdown.RefreshShownValue();

            _qualityLevelDropdown.onValueChanged.AddListener(SetQuality);
        }

        private void SetQuality(int selectedQualityIndex)
        {
            _graphicsService.Set(GraphicType.QualityLevel, selectedQualityIndex);
        }


        private void OnDestroy()
        {
            _qualityLevelDropdown.onValueChanged.RemoveListener(SetQuality);
        }
    }

}

