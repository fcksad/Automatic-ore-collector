using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Service
{
    public class SaveService : ISaveService, IInitializable
    {
        [field: SerializeField] public GameData GameData { get; private set; }
        [field: SerializeField] public SettingsData SettingsData { get; private set; }

        private FileDataHandler _gameDataHandler;
        private FileDataHandler _settingsDataHandler;

        private const string GameProfileID = "Game.json";
        private const string SettingsProfileID = "Settings.json";

        private ISceneService _sceneService;

        public SaveService(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        public void Initialize()
        {
            string savePath = Application.persistentDataPath;
            _gameDataHandler = new FileDataHandler(savePath, GameProfileID);
            _settingsDataHandler = new FileDataHandler(savePath, SettingsProfileID);

            LoadGame();
            LoadSettings();

            BindEvents();
        }

        private void BindEvents()
        {
            _sceneService.OnSceneLoadEvent += LoadGame;
            _sceneService.OnSceneUnloadEvent += SaveGame;

            _sceneService.OnSceneLoadEvent += LoadSettings;
            _sceneService.OnSceneUnloadEvent += SaveSettings;
        }

        private void NewGame()
        {
            GameData = new GameData();
        }

        private void NewSettings()
        {
            SettingsData = new SettingsData();
        }

        public void LoadGame()
        {
            GameData = _gameDataHandler.Load<GameData>("");

            if (GameData == null)
            {
                Debug.Log("No data was found. A New Game needs to be started before data can be loaded. profile id");
                NewGame();

                foreach (IDataInitialize dataPersistenceObject in FindAllDataPersistenceObjects<IDataInitialize>())
                {
                    dataPersistenceObject.Initialize();
                }

                return;
            }

            foreach (IDataPersistence dataPersistenceObject in FindAllDataPersistenceObjects<IDataPersistence>())
            {
                dataPersistenceObject.LoadData(GameData);
            }

            foreach (IDataLoad dataPersistenceObject in FindAllDataPersistenceObjects<IDataLoad>())
            {
                dataPersistenceObject.LoadData(GameData);
            }

            Debug.Log("Game Loaded");
        }

        public void LoadSettings()
        {
            SettingsData = _settingsDataHandler.Load<SettingsData>("");

            if (SettingsData == null)
            {
                Debug.Log("No SettingsData found. Creating default settings.");
                NewSettings();
            }
            else
            {
                Debug.Log("Settings Loaded");
            }
        }

        public void SaveGame()
        {
            if (GameData == null)
            {
                Debug.Log("No data was found. A New Game needs to be started before data can be saved");
                return;
            }

            foreach (IDataPersistence dataPersistenceObject in FindAllDataPersistenceObjects<IDataPersistence>())
            {
                dataPersistenceObject.SaveData(GameData);
            }

            foreach (IDataSave dataPersistenceObject in FindAllDataPersistenceObjects<IDataSave>())
            {
                dataPersistenceObject.SaveData(GameData);
            }

            _gameDataHandler.Save(GameData, "");

            Debug.Log("Game Saved");

        }

        public void SaveSettings()
        {
            if (SettingsData == null)
            {
                Debug.Log("No SettingsData found to save.");
                return;
            }

            _settingsDataHandler.Save(SettingsData, "");
            Debug.Log("Settings Saved");
        }


        private List<T> FindAllDataPersistenceObjects<T>() where T : class
        {
            IEnumerable<T> dataPersistenceObjects = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID).OfType<T>();
            return new List<T>(dataPersistenceObjects);
        }

        private Dictionary<string, T> GetAllprofilesGameData<T>() where T : class // if wanna make more data profile
        {
            return _gameDataHandler.LoadAllProfiles<T>();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
            SaveSettings();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            SaveGame();
            SaveSettings();
#endif
        }
    }

}