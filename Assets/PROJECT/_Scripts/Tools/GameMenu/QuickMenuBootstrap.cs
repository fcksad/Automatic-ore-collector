using Service;
using UnityEngine;

namespace Menu
{

    [DefaultExecutionOrder(-650)]
    public class QuickMenuBootstrap : MonoBehaviour
    {
        private SceneServiceLocator _scene;

        private void Awake()
        {
            _scene = SceneServiceLocator.Current;

            _scene.BindFromScene<QuickMenuView>();
            _scene.BindComponent<QuickMenuController>();

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
            scene.Unbind<QuickMenuController>();
            scene.Unbind<QuickMenuView>();
        }
    }
}
