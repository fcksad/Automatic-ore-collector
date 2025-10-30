public static class LightbarSettings
{
    private const string PREF_KEY = "LightbarEnabled";
    private static bool _isInitialized = false;
    private static bool _enabled = true;

    public static bool Enabled
    {
        get
        {
            EnsureInitialized();
            return _enabled;
        }
        set
        {
            _enabled = value;
/*            UpscaleSDK.Saves.SetInt(PREF_KEY, value ? 1 : 0);
            UpscaleSDK.Saves.Save();*/
        }
    }

    private static void EnsureInitialized()
    {
        if (_isInitialized) return;
        //_enabled = UpscaleSDK.Saves.GetInt(PREF_KEY, 1) == 1;
        _isInitialized = true;
    }
}
