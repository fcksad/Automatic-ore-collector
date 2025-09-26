using Menu;
using Service;
using UnityEngine;


[DefaultExecutionOrder(-650)]
public class PlaySceneBootstrap : MonoBehaviour
{
    private SceneServiceLocator _scene;

    private void Awake()
    {
        _scene = SceneServiceLocator.Current;

        _scene.BindFromScene<EnemyController>();
        _scene.BindFromScene<VehicleController>();

    }

    private void OnApplicationQuit()
    {
        UnbindSafe(_scene);
    }

    private void OnDestroy()
    {
        UnbindSafe(_scene);
    }

    private static void UnbindSafe(SceneServiceLocator scene)
    {
        if (scene == null) return;
        scene.Unbind<EnemyController>();
        scene.Unbind<VehicleController>();
    }
}
