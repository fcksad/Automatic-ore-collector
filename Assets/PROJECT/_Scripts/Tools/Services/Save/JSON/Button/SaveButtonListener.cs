using Service;

public class SaveButtonListener : CustomButton
{
    private ISaveService _saveService;

    private void Start()
    {
        _saveService = ServiceLocator.Get<ISaveService>();
        Button.onClick.AddListener(SaveSettings);
    }

    private void SaveSettings()
    {
        _saveService.SaveSettings();
    }

    private void OnDestroy()
    {
        Button.onClick.RemoveListener(SaveSettings);
    }
}
