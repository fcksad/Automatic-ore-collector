#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectImageExporter
{
    private static readonly string[] SourceRoots = {"Assets"};

    public static readonly string PathToSave;

    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".tga", ".gif", ".bmp", ".psd", ".tif", ".tiff", ".exr", ".hdr", ".svg", ".webp"
    };

    [MenuItem("Tools/Export All Images → Desktop")]
    public static void ExportAllImagesToDesktop()
    {
        try
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var exportRoot = Path.Combine(desktop, $"UnityProjectImages_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(exportRoot);

            var files = new List<string>();
            foreach (var root in SourceRoots)
            {
                var abs = Path.Combine(projectRoot, root);
                if (!Directory.Exists(abs)) continue;
                foreach (var f in Directory.EnumerateFiles(abs, "*.*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(f);
                    if (ImageExts.Contains(ext)) files.Add(f);
                }
            }

            int copied = 0, errors = 0;
            for (int i = 0; i < files.Count; i++)
            {
                var src = files[i];
                EditorUtility.DisplayProgressBar("Export Images", src, (float)(i + 1) / files.Count);

                try
                {
                    var uniqueDest = GetUniquePath(Path.Combine(exportRoot, Path.GetFileName(src)));
                    File.Copy(src, uniqueDest, overwrite: false);
                    copied++;
                }
                catch (Exception e)
                {
                    errors++;
                    Debug.LogWarning($"[Image Export] Skip '{src}': {e.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"[Image Export] Done. Copied: {copied}, errors: {errors}. → {exportRoot}");
            EditorUtility.RevealInFinder(exportRoot);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Image Export] Failed: {e}");
        }
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);

        int n = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({n++}){ext}");
        } while (File.Exists(candidate));
        return candidate;
    }

    static ProjectImageExporter()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string projectName = Path.GetFileName(projectRoot);

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string folderName = Sanitize($"{projectName} Images");
        PathToSave = Path.Combine(desktop, folderName);

        try { Directory.CreateDirectory(PathToSave); }
        catch (Exception e) { Debug.LogError($"[Exporter] Can't create export dir: {PathToSave}\n{e}"); }
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

}
#endif
