using Service;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class LightbarToggleView : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    private ControllerLightService _dualSenseLightService;

    private void Start()
    {
        _dualSenseLightService = ServiceLocator.Get<ControllerLightService>();

#if !(UNITY_PS4 || UNITY_PS5 || UNITY_STANDALONE || UNITY_EDITOR)

        gameObject.SetActive(false);
#endif

        _toggle.isOn = LightbarSettings.Enabled;
        _toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        LightbarSettings.Enabled = value;
        var effective = _dualSenseLightService.GetEffectiveBaseColor();

        if (value)
        {
            _dualSenseLightService.SetBaseColor(_dualSenseLightService.Palette.First(p => p.Color == effective).Key);
            _dualSenseLightService.FadeOn(0.25f);
        }
        else
        {
            _dualSenseLightService.FadeOff(0.25f);
        }


    }
}
