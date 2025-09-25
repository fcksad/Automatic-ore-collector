using System.Collections.Generic;
using UnityEngine;

public static class ResourceLoader
{
    /// <summary>
    /// Загружает все ассеты типа T из указанной папки Resources.
    /// Оставь путь пустым, чтобы получить все ассеты типа T из всей папки Resources(включая подкаталоги).
    /// Работает в билде и в рантайме.
    /// </summary>
    /// <typeparam name="T">Тип ассета (например, ScriptableObject, GameObject и т.д.)</typeparam>
    /// <param name="resourcePath">Путь относительно Resources (например, "Components")</param>
    /// <returns>Список найденных объектов</returns>
    public static List<T> GetAll<T>(string resourcePath = "") where T : UnityEngine.Object
    {
        var assets = Resources.LoadAll<T>(resourcePath);
        return new List<T>(assets);
    }

    /// <summary>
    /// Загружает один ассет типа T из указанного пути внутри папки Resources.
    /// </summary>
    /// <typeparam name="T">Тип ассета</typeparam>
    /// <param name="resourcePath">Путь к файлу относительно папки Resources (например, "Components")</param>
    /// <returns>Загруженный объект или null, если не найден</returns>
    public static T Get<T>(string resourcePath = "") where T : UnityEngine.Object
    {
        var asset = Resources.Load<T>(resourcePath);
        if (asset == null)
        {
            UnityEngine.Debug.LogError($"[ResourceLoader] Could not load asset of type {typeof(T)} at path: {resourcePath}");
        }
        return asset;
    }


#if UNITY_EDITOR
   /* /// <summary>
    /// Загружает ассеты указанного типа из пути в AssetDatabase (включая подкаталоги).
    /// Работает строго в Editor
    /// </summary>
    /// <typeparam name="T">Тип ассета (например, ScriptableObject, GameObject и т.д.)</typeparam>
    /// <param name="resourcePath">Путь в AssetDatabase (например, "Assets/MyConfigs")</param>
    /// <param name="typeFilter">Фильтр типа для AssetDatabase (например, "ScriptableObject", "Prefab")</param>
    public static List<T> GetAll<T>(string resourcePath, string typeFilter) where T : UnityEngine.Object
    {
        List<T> assetList = new List<T>();

        string[] guids = AssetDatabase.FindAssets($"t:{typeFilter}", new[] { resourcePath });

        var loadedAssets = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();

        assetList.AddRange(loadedAssets);

        return assetList;
    }*/
#endif

}

public enum ResourceType
{

}

public static class ResourceLocation
{
    private static readonly Dictionary<ResourceType, string> _resourcePaths = new()
    {

    };


    public static string GetPathByType(ResourceType type)
    {
        return _resourcePaths[type];
    }
}
