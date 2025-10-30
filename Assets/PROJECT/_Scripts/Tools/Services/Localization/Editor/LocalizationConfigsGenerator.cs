#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;              

public static class LocalizationConfigsGenerator
{
    private const string OutputRoot = "Assets/Resources/Localization/Configs";

    [MenuItem("Tools/Localization/Sync LocalizationConfigs (all tables)")]
    public static void SyncAllTables()
    {
        var collections = LocalizationEditorSettings.GetStringTableCollections();
        if (collections == null || collections.Count == 0)
        {
            Debug.LogWarning("[LocCfg] Нет StringTableCollection в проекте.");
            return;
        }

        EnsureFolder(OutputRoot);

        foreach (var col in collections)
        {
            var colName = col.TableCollectionName;
            var folder = $"{OutputRoot}/{Sanitize(colName)}";
            EnsureFolder(folder);

            foreach (var e in col.SharedData.Entries)
                CreateOrUpdateConfig(folder, colName, e.Key, e.Id);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=#5cf>[LocCfg]</color> Синхронизация завершена.");
    }

    [MenuItem("Tools/Localization/Sync LocalizationConfigs (selected table)")]
    public static void SyncSelectedTable()
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("[LocCfg] Выделите StringTableCollection в Project.");
            return;
        }

        var col = selected as StringTableCollection;
        if (col == null)
        {
            var pathSel = AssetDatabase.GetAssetPath(selected);
            col = AssetDatabase.LoadAssetAtPath<StringTableCollection>(pathSel);
        }
        if (col == null)
        {
            Debug.LogWarning("[LocCfg] Это не StringTableCollection.");
            return;
        }

        var colName = col.TableCollectionName;
        var folder = $"{OutputRoot}/{Sanitize(colName)}";
        EnsureFolder(OutputRoot);
        EnsureFolder(folder);

        foreach (var e in col.SharedData.Entries)
            CreateOrUpdateConfig(folder, colName, e.Key, e.Id);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=#5cf>[LocCfg]</color> Обновлено: {colName}");
    }

    private static void CreateOrUpdateConfig(string folder, string tableCollectionName, string key, long entryId)
    {
        var fileName = $"{Sanitize(key)}.asset";
        var path = $"{folder}/{fileName}";

        // Создаём / берём существующий конфиг
        var cfg = AssetDatabase.LoadAssetAtPath<Localization.LocalizationConfig>(path);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<Localization.LocalizationConfig>();
            AssetDatabase.CreateAsset(cfg, path);
        }

        // Обновляем LocalizedString через SerializedObject (совместимо со старыми версиями пакета)
        var so = new SerializedObject(cfg);
        var locStrProp = so.FindProperty("_localizedString");
        if (locStrProp == null)
        {
            Debug.LogError($"[LocCfg] Не найдено поле _localizedString в {path}");
            return;
        }

        // m_TableReference
        var tableRefProp = locStrProp.FindPropertyRelative("m_TableReference");
        if (tableRefProp != null)
        {
            var nameProp = tableRefProp.FindPropertyRelative("m_TableCollectionName");
            if (nameProp != null) nameProp.stringValue = tableCollectionName;

            var guidProp = tableRefProp.FindPropertyRelative("m_TableCollectionNameGuid");
            if (guidProp != null) guidProp.stringValue = string.Empty; // очищаем, чтобы резолвилось по имени
        }

        // m_TableEntryReference
        var entryRefProp = locStrProp.FindPropertyRelative("m_TableEntryReference");
        if (entryRefProp != null)
        {
            var idProp = entryRefProp.FindPropertyRelative("m_KeyId");
            if (idProp != null) idProp.longValue = entryId;

            var keyProp = entryRefProp.FindPropertyRelative("m_Key");
            if (keyProp != null) keyProp.stringValue = string.Empty;   // работаем по ID
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cfg);

        // На всякий случай — имя ассета под ключ
        var onDisk = Path.GetFileNameWithoutExtension(path);
        var safe = Sanitize(key);
        if (!string.Equals(onDisk, safe))
            AssetDatabase.RenameAsset(path, safe);
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "_";
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(s.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return safe.Replace('/', '_').Replace('\\', '_').Trim();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var parts = folder.Split('/');
        var cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
