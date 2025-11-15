using UnityEngine;
using UnityEditor;
using System.IO;

public static class IconGeneratorFromModel
{
    [MenuItem("Tools/Generate Icon From Selected Model")]
    public static void GenerateIcon()
    {
        Object obj = Selection.activeObject;

        if (obj == null)
        {
            Debug.LogError("Ничего не выбрано!");
            return;
        }

        Texture2D preview = AssetPreview.GetAssetPreview(obj);

        if (preview == null)
        {
            Debug.LogError("Unity ещё не сгенерировала превью. Подождите секунду или шевельните мышкой.");
            return;
        }

        string folder = "Assets/Resources/Components/ART/2D/ModuleIcon/";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = obj.name + ".png";
        string path = Path.Combine(folder, fileName);

        File.WriteAllBytes(path, preview.EncodeToPNG());
        AssetDatabase.Refresh();
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.spritePixelsPerUnit = 100;
        settings.filterMode = FilterMode.Point;
        settings.mipmapEnabled = false;

        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();

        Debug.Log($"✔ Иконка сохранена и настроена: {path}");
    }
}
