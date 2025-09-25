using Service;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Settings
{
    public class VSynceControl : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _vSynceModeDropdown;

        private IGraphicsService _graphicsService;

        private void Awake()
        {
            _graphicsService = ServiceLocator.Get<IGraphicsService>();

            List<TMP_Dropdown.OptionData> VSyncenOptions = new List<TMP_Dropdown.OptionData>();
            VSyncenOptions.Add(new TMP_Dropdown.OptionData("Disable"));
            VSyncenOptions.Add(new TMP_Dropdown.OptionData("Enable"));

            _vSynceModeDropdown.options = VSyncenOptions;
            _vSynceModeDropdown.value = _graphicsService.Get(GraphicType.VSync);
            _vSynceModeDropdown.RefreshShownValue();

            _vSynceModeDropdown.onValueChanged.AddListener(SetVSynce);
        }

        private void SetVSynce(int selectedVSynceIndex)
        {
            _graphicsService.Set(GraphicType.VSync, selectedVSynceIndex);
        }

        private void OnDestroy()
        {
            _vSynceModeDropdown.onValueChanged.RemoveListener(SetVSynce);
        }
    }
}
