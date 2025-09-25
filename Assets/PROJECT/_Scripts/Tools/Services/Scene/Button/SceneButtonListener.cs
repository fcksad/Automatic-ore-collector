using Service;
using UnityEngine;
using UnityEngine.UI;

public class SceneButtonListener : CustomButton
{
    [SerializeField] private SceneConfig _sceneToLoad;

    private ISceneService _sceneService;

    private void Awake()
    {
        _sceneService = ServiceLocator.Get<ISceneService>();
        Button.onClick.AddListener(LoadScene);
    }

    private void LoadScene()
    {
        _sceneService.Transition(_sceneToLoad.Scene.SceneName);
    }

    private void OnDestroy()
    {
        Button.onClick.AddListener(LoadScene);
    }
}
