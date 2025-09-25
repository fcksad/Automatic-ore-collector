using Service;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Settings
{
    public enum ScreenMode
    {
        Windowed = 0,
        Maximized = 1,
        Fullscreen = 2
    }

    public class ScreenModeControl : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _screenModeDropdown;

        private IGraphicsService _graphicsService;

        private void Awake()
        {
            _graphicsService = ServiceLocator.Get<IGraphicsService>();

            List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData(ScreenMode.Windowed.ToString()),
                new TMP_Dropdown.OptionData(ScreenMode.Maximized.ToString()),
                new TMP_Dropdown.OptionData(ScreenMode.Fullscreen.ToString())
            };


            _screenModeDropdown.options = dropdownOptions;

            int savedValue = _graphicsService.Get(GraphicType.ScreenMode);
            _screenModeDropdown.value = Mathf.Clamp(savedValue, 0, dropdownOptions.Count - 1);
            _screenModeDropdown.RefreshShownValue();

            _screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
        }

        private void SetScreenMode(int selectedScreenModeIndex)
        {
            _graphicsService.Set(GraphicType.ScreenMode, selectedScreenModeIndex);
        }

        private void OnDestroy()
        {
            _screenModeDropdown.onValueChanged.RemoveListener(SetScreenMode);
        }
    }

}
