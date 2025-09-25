#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectValidationConfig", menuName = "Configs/Validation/Project Validation Config")]
public class ProjectValidationConfig : ScriptableObject
{
    [Header("Branding")]
    public string companyName = "RemoteRats";
    public string productName = "BlaBla";

    [Header("Packages (id@version)")]
    [Tooltip("Например: com.unity.inputsystem@1.11.2")]
    public List<RequiredPackage> requiredPackages = new()
    {
        new RequiredPackage { id = "com.unity.inputsystem", version = "1.11.2" }, 
    };

    [Header("Script Execution Order")]
    public List<ExecutionOrderEntry> executionOrder = new()
    {
        new ExecutionOrderEntry { className = "ServiceLocator",  order = -900 },
        new ExecutionOrderEntry { className = "ServiceBootstrap", order = -800 },
    };
}

[Serializable]
public class RequiredPackage
{
    public string id;
    public string version;
    public string IdWithVersion => string.IsNullOrEmpty(version) ? id : $"{id}@{version}";
}

[Serializable]
public class ExecutionOrderEntry
{
    [Tooltip("Имя класса MonoBehaviour (как в коде)")]
    public string className;
    public int order = 0;
}
#endif
