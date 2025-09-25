using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Scene")]
public class SceneConfig : ScriptableObject
{
    public bool LoadingOnInitialization { get; private set; } = false;

    [field:SerializeField] public SceneField Scene {  get; private set; }
}
