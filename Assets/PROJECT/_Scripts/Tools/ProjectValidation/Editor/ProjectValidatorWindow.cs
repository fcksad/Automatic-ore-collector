#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class ProjectValidatorWindow : EditorWindow
{
    private const string ConfigAssetSearch = "t:ProjectValidationConfig";
    private ProjectValidationConfig _config;

    private Vector2 _scroll;
    private string _status = "";
    private bool _busy = false;

    // PM requests
    private ListRequest _listReq;
    private Queue<Action> _pendingFixes = new();

    [MenuItem("Tools/Project Validator")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<ProjectValidatorWindow>("Project Validator");
        wnd.minSize = new Vector2(480, 380);
        wnd.Focus();
    }

    private void OnEnable()
    {
        if (_config == null)
            _config = LoadOrCreateConfig();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                ValidateAll();

            using (new EditorGUI.DisabledScope(_busy))
            {
                if (GUILayout.Button("Fix All", EditorStyles.toolbarButton))
                    FixAll();
            }

            GUILayout.FlexibleSpace();

            _config = (ProjectValidationConfig)EditorGUILayout.ObjectField(_config, typeof(ProjectValidationConfig), false, GUILayout.Width(280));
        }

        EditorGUILayout.Space();

        if (_config == null)
        {
            EditorGUILayout.HelpBox("Создай/укажи ProjectValidationConfig (Create → Tools → Project Validation Config)", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawBrandingSection();
        EditorGUILayout.Space();

        DrawPackagesSection();
        EditorGUILayout.Space();

        DrawExecutionOrderSection();
        EditorGUILayout.Space();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(_status, _busy ? MessageType.Info : MessageType.None);
    }

    #region Sections

    private void DrawBrandingSection()
    {
        EditorGUILayout.LabelField("Branding", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            var okCompany = PlayerSettings.companyName == _config.companyName;
            var okProduct = PlayerSettings.productName == _config.productName;

            EditorGUILayout.LabelField($"Company Name: {(okCompany ? "OK" : $"Mismatch (now: '{PlayerSettings.companyName}')")}");
            EditorGUILayout.LabelField($"Product Name: {(okProduct ? "OK" : $"Mismatch (now: '{PlayerSettings.productName}')")}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Branding"))
                {
                    PlayerSettings.companyName = _config.companyName;
                    PlayerSettings.productName = _config.productName;
                    _status = $"Branding applied: {PlayerSettings.companyName} / {PlayerSettings.productName}";
                }

                if (GUILayout.Button("Ping Config")) EditorGUIUtility.PingObject(_config);
            }
        }
    }

    private void DrawPackagesSection()
    {
        EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (GUILayout.Button("Refresh Installed Packages"))
                StartListPackages();

            if (_listReq == null)
                EditorGUILayout.HelpBox("Нажми \"Refresh Installed Packages\" чтобы сверить версии.", MessageType.None);

            if (_listReq != null && !_listReq.IsCompleted)
                EditorGUILayout.HelpBox("Загружаю список пакетов…", MessageType.Info);

            if (_listReq != null && _listReq.IsCompleted && _listReq.Status == StatusCode.Success)
            {
                var installed = _listReq.Result.ToDictionary(p => p.name, p => p.version);

                foreach (var req in _config.requiredPackages)
                {
                    var has = installed.TryGetValue(req.id, out var ver);
                    var ok = has && string.Equals(ver, req.version, StringComparison.OrdinalIgnoreCase);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{req.id}", GUILayout.Width(260));
                    EditorGUILayout.LabelField($"Need: {req.version}", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"Installed: {(has ? ver : "—")}", GUILayout.Width(140));
                    using (new EditorGUI.DisabledScope(ok || _busy))
                    {
                        if (GUILayout.Button(ok ? "OK" : "Install/Update", GUILayout.Width(120)))
                            EnqueueInstall(req);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

    private void DrawExecutionOrderSection()
    {
        EditorGUILayout.LabelField("Script Execution Order", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            foreach (var eo in _config.executionOrder)
            {
                var ms = FindMonoScriptByClass(eo.className);
                var haveScript = ms != null;
                var current = haveScript ? MonoImporter.GetExecutionOrder(ms) : int.MinValue;
                var ok = haveScript && current == eo.order;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(eo.className, GUILayout.Width(220));
                EditorGUILayout.LabelField($"Need: {eo.order}", GUILayout.Width(90));
                EditorGUILayout.LabelField($"Now: {(haveScript ? current.ToString() : "not found")}", GUILayout.Width(120));

                using (new EditorGUI.DisabledScope(ok || !haveScript))
                {
                    if (GUILayout.Button(ok ? "OK" : "Apply", GUILayout.Width(100)) && haveScript)
                    {
                        MonoImporter.SetExecutionOrder(ms, eo.order);
                        _status = $"Set execution order: {eo.className} → {eo.order}";
                    }
                }

                if (haveScript && GUILayout.Button("Ping", GUILayout.Width(60)))
                    EditorGUIUtility.PingObject(ms);

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    #endregion

    #region Validate / Fix

    private void ValidateAll()
    {
        _status = "Validating…";

        var brandingOk = PlayerSettings.companyName == _config.companyName &&
                         PlayerSettings.productName == _config.productName;

        StartListPackages();

        bool eoOk = true;
        foreach (var eo in _config.executionOrder)
        {
            var ms = FindMonoScriptByClass(eo.className);
            if (ms == null) { eoOk = false; continue; }
            if (MonoImporter.GetExecutionOrder(ms) != eo.order) eoOk = false;
        }

        _status = $"Branding: {(brandingOk ? "OK" : "Need Apply")}  |  " +
                  $"Packages: see table  |  " +
                  $"Execution Order: {(eoOk ? "OK" : "Need Apply")}";
    }

    private void FixAll()
    {
        if (_busy) return;

        PlayerSettings.companyName = _config.companyName;
        PlayerSettings.productName = _config.productName;

        foreach (var eo in _config.executionOrder)
        {
            var ms = FindMonoScriptByClass(eo.className);
            if (ms != null && MonoImporter.GetExecutionOrder(ms) != eo.order)
                MonoImporter.SetExecutionOrder(ms, eo.order);
        }

        StartListPackages(() =>
        {
            var installed = _listReq.Result.ToDictionary(p => p.name, p => p.version);
            foreach (var req in _config.requiredPackages)
            {
                var has = installed.TryGetValue(req.id, out var ver);
                var ok = has && string.Equals(ver, req.version, StringComparison.OrdinalIgnoreCase);
                if (!ok) EnqueueInstall(req);
            }
            ProcessNextFix(); 
        });

        _status = "Fixing…";
    }

    #endregion

    #region Packages

    private void StartListPackages(Action onDone = null)
    {
        if (_busy) return;
        _busy = true;
        _status = "Listing packages…";
        _listReq = Client.List(true); 
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;

        void OnEditorUpdate()
        {
            if (_listReq.IsCompleted)
            {
                EditorApplication.update -= OnEditorUpdate;
                _busy = false;

                if (_listReq.Status == StatusCode.Success)
                {
                    _status = $"Packages listed: {_listReq.Result}";
                    onDone?.Invoke();
                    Repaint();
                }
                else
                {
                    _status = $"Package list failed: {_listReq.Error.message}";
                    onDone?.Invoke();
                    Repaint();
                }
            }
        }
    }

    private void EnqueueInstall(RequiredPackage req)
    {
        _pendingFixes.Enqueue(() => InstallOrUpdate(req));
        if (!_busy) ProcessNextFix();
    }

    private AddRequest _addReq;

    private void InstallOrUpdate(RequiredPackage req)
    {
        _busy = true;
        _status = $"Installing {req.IdWithVersion} …";
        _addReq = Client.Add(req.IdWithVersion);

        EditorApplication.update -= OnAddUpdate;
        EditorApplication.update += OnAddUpdate;

        void OnAddUpdate()
        {
            if (_addReq.IsCompleted)
            {
                EditorApplication.update -= OnAddUpdate;
                _busy = false;

                if (_addReq.Status == StatusCode.Success)
                {
                    _status = $"Installed/Updated: {req.IdWithVersion}";
                }
                else
                {
                    _status = $"Install failed for {req.IdWithVersion}: {_addReq.Error?.message}";
                }

                StartListPackages(ProcessNextFix);
            }
        }
    }

    private void ProcessNextFix()
    {
        if (_pendingFixes.Count == 0) { _status = "All fixes applied."; Repaint(); return; }
        var action = _pendingFixes.Dequeue();
        action.Invoke();
    }

    #endregion

    #region Helpers

    private static ProjectValidationConfig LoadOrCreateConfig()
    {
        var guids = AssetDatabase.FindAssets(ConfigAssetSearch);
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ProjectValidationConfig>(path);
        }

        var asset = ScriptableObject.CreateInstance<ProjectValidationConfig>();
        var savePath = "Assets/ProjectValidationConfig.asset";
        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Validator] Created default config at {savePath}");
        return asset;
    }

    private static MonoScript FindMonoScriptByClass(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return null;
        foreach (var ms in Resources.FindObjectsOfTypeAll<MonoScript>())
        {
            var t = ms.GetClass();
            if (t != null && t.Name == className) return ms;
        }
        return null;
    }

    #endregion
}
#endif
