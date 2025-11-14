#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputActionConfigsGenerator
{
    private const string InputActionsAssetPath = "Assets/PROJECT/_Scripts/Tools/Services/Input/InputSystem_Actions.inputactions";

    private const string OutputRoot = "Assets/Resources/Components/Config/Input/Configs";


    [MenuItem("Tools/Input/Sync InputActionConfigs (all CharacterAction)")]
    public static void SyncAllCharacterActions()
    {
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
        if (inputAsset == null)
        {
            Debug.LogError($"[InputCfg] Не найден InputActionAsset по пути: {InputActionsAssetPath}");
            return;
        }

        EnsureFolder(OutputRoot);

        var allActions = (CharacterAction[])System.Enum.GetValues(typeof(CharacterAction));

        foreach (var ca in allActions)
        {
            if (ca == CharacterAction.Any)
                continue;

            SyncSingleAction(inputAsset, ca);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=#5cf>[InputCfg]</color> Синхронизация (все CharacterAction) завершена.");
    }

    [MenuItem("Tools/Input/Sync InputActionConfigs (selected configs)")]
    public static void SyncSelectedConfigs()
    {
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
        if (inputAsset == null)
        {
            Debug.LogError($"[InputCfg] Не найден InputActionAsset по пути: {InputActionsAssetPath}");
            return;
        }

        EnsureFolder(OutputRoot);

        var selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[InputCfg] Нет выделенных объектов. Выдели один или несколько InputActionConfigBase.");
            return;
        }

        int count = 0;

        foreach (var obj in selected)
        {
            var cfg = obj as InputActionConfigBase;
            if (cfg == null)
                continue;

            var ca = cfg.Action;
            SyncSingleAction(inputAsset, ca);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#5cf>[InputCfg]</color> Синхронизировано выделенных конфигов: {count}");
    }

    private static void SyncSingleAction(InputActionAsset inputAsset, CharacterAction characterAction)
    {
        var actionName = characterAction.ToString();

        var inputAction = inputAsset.FindAction(actionName, throwIfNotFound: false);
        if (inputAction == null)
        {
            Debug.LogWarning($"[InputCfg] В {InputActionsAssetPath} не найден InputAction с именем '{actionName}'. " +
                             "Config всё равно будет создан/обновлён, но InputReference, возможно, придётся выставить вручную.");
        }

        var cfgPath = $"{OutputRoot}/{actionName}.asset";
        var cfg = AssetDatabase.LoadAssetAtPath<InputActionConfigBase>(cfgPath);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<InputActionConfigBase>();
            AssetDatabase.CreateAsset(cfg, cfgPath);
        }

        var so = new SerializedObject(cfg);

        var actionProp = so.FindProperty("<Action>k__BackingField");
        if (actionProp != null)
        {
            actionProp.intValue = (int)characterAction;
        }
        else
        {
            Debug.LogWarning($"[InputCfg] Не найдено поле <Action>k__BackingField у {cfgPath}");
        }

        var inputRefProp = so.FindProperty("<InputReference>k__BackingField");
        if (inputRefProp != null && inputRefProp.objectReferenceValue == null && inputAction != null)
        {
            InputActionReference existingRef = null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(cfgPath);
            foreach (var o in subAssets)
            {
                if (o is InputActionReference iar)
                {
                    existingRef = iar;
                    break;
                }
            }

            if (existingRef == null)
            {
                existingRef = InputActionReference.Create(inputAction);
                existingRef.name = actionName;               
                AssetDatabase.AddObjectToAsset(existingRef, cfg);
            }

            inputRefProp.objectReferenceValue = existingRef;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cfg);
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
