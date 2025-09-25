#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoBuildVersionIncrement : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string currentVersion = PlayerSettings.bundleVersion; 
        string[] parts = currentVersion.Split('.');

        if (parts.Length < 3)
        {
            Debug.LogError($"[Build] bundleVersion format invalid: {currentVersion}. Expected format: MAJOR.MINOR.BUILD");
            return;
        }

        string prefix = $"{parts[0]}.{parts[1]}";

        int oldBuild = 0;
        int.TryParse(parts[2].Split(' ')[0], out oldBuild);
        int newBuild = oldBuild + 1;

        string newVersion = $"{prefix}.{newBuild}";

        PlayerSettings.bundleVersion = newVersion;
        EditorPrefs.SetInt("BuildNumber", newBuild);

        Debug.Log($"[Build] Updated version: {newVersion}");
    }
}
#endif
