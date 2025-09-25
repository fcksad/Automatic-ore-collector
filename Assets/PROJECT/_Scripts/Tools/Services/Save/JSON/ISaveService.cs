using UnityEngine;

public interface ISaveService 
{
    GameData GameData { get; }
    SettingsData SettingsData { get; }

    void LoadGame();
    void SaveGame();
    void SaveSettings();
}
