using System.Collections.Generic;
using UnityEngine;

public class ToggleButton : CustomButton
{
    [SerializeField] private List<ToggleWindow> _windows = new List<ToggleWindow>();

    protected void Awake()
    {
        Button.onClick.AddListener(Toggle);
    }

    protected void Toggle()
    {
        foreach (var window in _windows)
        {
            window?.Toggle();
        }
    }

    protected void OnDestroy()
    {
        Button.onClick.RemoveListener(Toggle);
    }
}
