using Services.Device;
using UnityEngine;
using UnityEngine.UI;

public class VibrationButtonListener : CustomButton
{
    private void Awake()
    {
        if (Button != null)
            Button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (Button != null)
            Button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        Vibration.TriggerPreset(RumblePreset.Click);
    }
}
