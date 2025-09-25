using System.Collections.Generic;
using UnityEngine;

public class TabController : MonoBehaviour
{
    [System.Serializable]
    public class TabLink
    {
        [field: SerializeField] public CustomButton ToggleButton { get; private set; }
        [field: SerializeField] public ToggleWindow ToggleWindow { get; private set; }
    }

    [SerializeField] private List<TabLink> _tabLink;

    private void Awake()
    {
        foreach (var toggle in _tabLink)
        {
            var capturedToggle = toggle;
            toggle.ToggleButton.Button.onClick.AddListener(() => Select(capturedToggle.ToggleButton));
        }
    }

    private void Select(CustomButton selected)
    {
        foreach (var toggle in _tabLink)
        {
            bool isSelected = toggle.ToggleButton == selected;

            toggle.ToggleWindow.Toggle(isSelected);
            toggle.ToggleButton.Button.interactable = !isSelected;
        }
    }

    private void ResetTabs()
    {
        foreach (var toggle in _tabLink)
        {
            toggle.ToggleWindow.Toggle(false);
            toggle.ToggleButton.Button.interactable = true;
        }
    }
}
