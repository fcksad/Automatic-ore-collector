using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CustomAnimBaker
{
    // можно зафиксировать FPS, если не хочется брать clip.frameRate
    private const bool UseClipFrameRate = true;
    private const int DefaultFps = 30;

    [MenuItem("Tools/CustomAnimBaker/Bake Selected AnimationClips")]
    public static void BakeSelected()
    {
        // 1) ищем выбранный риг (из Hierarchy)
        Transform rigRoot = Selection.transforms.FirstOrDefault();
        if (rigRoot == null)
        {
            EditorUtility.DisplayDialog("CustomAnimBaker",
                "Выдели в Hierarchy корень рига (GameObject) И клипы в Project.", "OK");
            return;
        }

        // 2) собираем клипы из выделения в Project
        var clips = Selection.objects
            .OfType<AnimationClip>()
            .Where(c => c != null && !c.legacy) // legacy можно, если нужно — убери фильтр
            .Distinct()
            .ToArray();

        if (clips.Length == 0)
        {
            // fallback: попробовать достать клипы из Animator на риге
            var animator = rigRoot.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
                clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
        }

        if (clips.Length == 0)
        {
            EditorUtility.DisplayDialog("CustomAnimBaker",
                "Клипы не найдены. Выдели один или несколько AnimationClip в Project (и риг в Hierarchy).",
                "OK");
            return;
        }

        try
        {
            AnimationMode.StartAnimationMode();
            int baked = 0;

            foreach (var clip in clips)
            {
                if (clip == null) continue;

                // 3) строим список путей костей по кривым клипа (T/R/S)
                var bonePaths = CollectBonePaths(clip, rigRoot);

                if (bonePaths.Count == 0)
                {
                    Debug.LogWarning($"[Baker] В клипе '{clip.name}' нет пригодных кривых (T/R/S). Пропуск.");
                    continue;
                }

                // 4) FPS/длина
                int fps = UseClipFrameRate ? Mathf.Max(1, Mathf.RoundToInt(clip.frameRate)) : DefaultFps;
                float length = Mathf.Max(1e-6f, clip.length);
                int frameCount = Mathf.Max(1, Mathf.CeilToInt(length * fps));
                float dt = 1f / fps;

                // 5) сопоставляем пути → Transform
                var boneTransforms = MapBones(rigRoot, bonePaths);

                // 6) собираем кадры
                var frames = new CustomAnimClip.Frame[frameCount];

                for (int f = 0; f < frameCount; f++)
                {
                    float t = Mathf.Min(f * dt, length - 1e-6f);
                    AnimationMode.SampleAnimationClip(rigRoot.gameObject, clip, t);

                    var pos = new Vector3[boneTransforms.Length];
                    var rot = new Quaternion[boneTransforms.Length];
                    var scl = new Vector3[boneTransforms.Length];

                    for (int i = 0; i < boneTransforms.Length; i++)
                    {
                        var tr = boneTransforms[i];
                        if (tr)
                        {
                            pos[i] = tr.localPosition;
                            rot[i] = tr.localRotation;
                            scl[i] = tr.localScale;
                        }
                        else
                        {
                            // если кость не найдена — пишем заглушки
                            pos[i] = Vector3.zero;
                            rot[i] = Quaternion.identity;
                            scl[i] = Vector3.one;
                        }
                    }

                    frames[f] = new CustomAnimClip.Frame
                    {
                        LocalPos = pos,
                        LocalRot = rot,
                        LocalScale = scl
                    };
                }

                // 7) конвертим AnimationEvent → CustimAnimEvent (нормализуем время)
                var evs = ConvertEvents(clip);

                // 8) создаём/сохраняем ScriptableObject
                var asset = ScriptableObject.CreateInstance<CustomAnimClip>();
                asset.BonePaths = bonePaths.ToArray();
                asset.Frames = frames;
                asset.Fps = fps;
                asset.Length = length;
                asset.Loop = true;
                asset.Events = evs;

                string clipPath = AssetDatabase.GetAssetPath(clip);
                string dir = System.IO.Path.GetDirectoryName(clipPath);
                string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(clipPath);
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{nameNoExt}_Mini.asset");

                AssetDatabase.CreateAsset(asset, assetPath);
                EditorUtility.SetDirty(asset);

                Debug.Log($"[Baker] Baked '{clip.name}' → {assetPath}  (frames:{frameCount}, fps:{fps})");
                baked++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("CustomAnimBaker",
                baked > 0 ? $"Готово. Запечено клипов: {baked}" : "Ничего не запекли.",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Baker] Ошибка: {ex}");
        }
        finally
        {
            AnimationMode.StopAnimationMode();
        }
    }

    // Собираем уникальные пути, у которых есть T/R/S кривые.
    private static List<string> CollectBonePaths(AnimationClip clip, Transform rigRoot)
    {
        var paths = new HashSet<string>();

        foreach (var b in AnimationUtility.GetCurveBindings(clip))
        {
            // интересуют локальные трансформы
            if (b.type == typeof(Transform))
            {
                // фильтруем только T/R/S
                if (b.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal) ||
                    b.propertyName.StartsWith("m_LocalRotation", StringComparison.Ordinal) ||
                    b.propertyName.StartsWith("localEulerAnglesRaw", StringComparison.Ordinal) ||
                    b.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal))
                {
                    paths.Add(b.path);
                }
            }
        }

        // оставляем только те пути, которые реально находятся под рутом
        var list = paths.Where(p => string.IsNullOrEmpty(p) || rigRoot.Find(p) != null).ToList();

        // стабильный порядок: по глубине и лексикографически
        list.Sort((a, b) =>
        {
            int da = string.IsNullOrEmpty(a) ? 0 : a.Count(c => c == '/');
            int db = string.IsNullOrEmpty(b) ? 0 : b.Count(c => c == '/');
            int cmp = da.CompareTo(db);
            return cmp != 0 ? cmp : string.CompareOrdinal(a, b);
        });

        return list;
    }

    private static Transform[] MapBones(Transform root, List<string> paths)
    {
        var arr = new Transform[paths.Count];
        for (int i = 0; i < paths.Count; i++)
        {
            string p = paths[i];
            arr[i] = string.IsNullOrEmpty(p) ? root : root.Find(p);
            if (!arr[i])
                Debug.LogWarning($"[Baker] Не найдена кость по пути '{p}' под '{root.name}'");
        }
        return arr;
    }

    private static CustimAnimEvent[] ConvertEvents(AnimationClip clip)
    {
        var evs = AnimationUtility.GetAnimationEvents(clip);
        if (evs == null || evs.Length == 0) return Array.Empty<CustimAnimEvent>();

        float len = Mathf.Max(1e-6f, clip.length);
        var list = new List<CustimAnimEvent>(evs.Length);
        foreach (var e in evs)
        {
            float norm = Mathf.Clamp01(e.time / len);
            list.Add(new CustimAnimEvent { Name = e.functionName, NormalizedTime = norm });
        }
        // стабильный порядок по времени
        list.Sort((a, b) => a.NormalizedTime.CompareTo(b.NormalizedTime));
        return list.ToArray();
    }
}
