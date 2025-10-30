using UnityEngine;
using UnityEngine.UI;

public class VibrationSettings : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle _toggle;

    private const string PREF_KEY = "VibrationEnable";

    private static bool _enabled = true;
    private static bool _isInitialized = false;

    private bool _suppressToggleEvent = false;

    public static bool Enabled
    {
        get
        {
            EnsureInitialized();
            return _enabled;
        }
        set
        {
            EnsureInitialized();
            if (_enabled == value) return;

            _enabled = value;
/*
            UpscaleSDK.Saves.SetInt(PREF_KEY, value ? 1 : 0);
            UpscaleSDK.Saves.Save();*/

            if (!_enabled)
                Services.Device.Vibration.StopAll();
        }
    }

    private void Awake()
    {
        EnsureInitialized();
        Services.Device.Vibration.BindIsEnabled(() => Enabled);

        if (!Enabled)
            Services.Device.Vibration.StopAll();
    }

    private void OnEnable()
    {
        _suppressToggleEvent = true;
        if (_toggle != null)
            _toggle.SetIsOnWithoutNotify(Enabled);
        _suppressToggleEvent = false;

        if (_toggle != null)
            _toggle.onValueChanged.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggle);
    }

    private void OnToggle(bool value)
    {
        if (_suppressToggleEvent) return;
        Enabled = value;
    }

    private static void EnsureInitialized()
    {
        if (_isInitialized) return;

        //_enabled = UpscaleSDK.Saves.GetInt(PREF_KEY, 1) == 1;
        _isInitialized = true;
    }
}


