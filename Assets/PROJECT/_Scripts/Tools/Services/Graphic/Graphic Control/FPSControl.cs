using Service;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSControl : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _fpsDropdown;

    private List<string> _fpsOptions = new List<string> { "Disable", "60", "120" };

    private IGraphicsService _graphicsService;

    private void Awake()
    {
        _graphicsService = ServiceLocator.Get<IGraphicsService>();

        _fpsDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string fpsOption in _fpsOptions)
        {
            options.Add(new TMP_Dropdown.OptionData(fpsOption));
        }

        _fpsDropdown.AddOptions(options);

        int currentFPS = _graphicsService.Get(GraphicType.FPS);
        int dropdownIndex = GetDropdownIndexFromFPS(currentFPS);
        _fpsDropdown.value = dropdownIndex;
        _fpsDropdown.RefreshShownValue();

        _fpsDropdown.onValueChanged.AddListener(SetFPS);
    }

    private void SetFPS(int selectedIndex)
    {
        int fps = GetFPSFromDropdownIndex(selectedIndex);
        _graphicsService.Set(GraphicType.FPS, fps);
    }

    private int GetDropdownIndexFromFPS(int fps)
    {
        switch (fps)
        {
            case -1: return 0;
            case 60: return 1;
            case 120: return 2;
            default: return 0;
        }
    }

    private int GetFPSFromDropdownIndex(int index)
    {
        switch (index)
        {
            case 0: return -1;
            case 1: return 60;
            case 2: return 120;
            default: return -1;
        }
    }

    private void OnDestroy()
    {
        _fpsDropdown.onValueChanged.RemoveListener(SetFPS);
    }
}
