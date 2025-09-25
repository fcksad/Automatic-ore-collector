namespace Service.Save.Prefs
{
    public interface ISaveService
    {

        bool HasKey(string key);
        void DeleteKey(string key);
        void DeleteAll();
        void SaveAll();


        void SaveInt(string key, int value);
        int LoadInt(string key, int defaultValue = 0);

        void SaveFloat(string key, float value);
        float LoadFloat(string key, float defaultValue = 0f);

        void SaveBool(string key, bool value);
        bool LoadBool(string key, bool defaultValue = false);

        void SaveString(string key, string value);
        string LoadString(string key, string defaultValue = "");
    }
}
