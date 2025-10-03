using Inventory;
using Service;
using UnityEngine;

[DefaultExecutionOrder(-650)]
public class ShipSceneBootstrap : MonoBehaviour
{
    private SceneServiceLocator _scene;

    private void Awake()
    {
        _scene = SceneServiceLocator.Current;

        _scene.BindFromScene<InventoryGridController>();

/*        _scene.BindFromScene<EnemyController>();
        _scene.BindFromScene<VehicleController>();
        _scene.BindFromScene<CameraController>();*/

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

        scene.Unbind<InventoryGridController>();

/*        scene.Unbind<EnemyController>();
        scene.Unbind<VehicleController>();*/
    }
}
