using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneService : ISceneService
{
    public event Action OnSceneLoadEvent;
    public event Action OnSceneUnloadEvent;

    public async Task Transition(string sceneToLoad)
    {
        /* var activeScene = SceneManager.GetActiveScene().name;

         OnSceneUnloadEvent?.Invoke();
         //await Task.Delay(100); // todo

         await LoadScene(sceneToLoad);

         await UnloadScene(activeScene);
         OnSceneLoadEvent?.Invoke();*/

        OnSceneUnloadEvent?.Invoke();

        var op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();   

        OnSceneLoadEvent?.Invoke();
    }

    private async Task LoadScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task UnloadScene(string sceneName)
    {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        while (!asyncUnload.isDone)
        {
            await Task.Yield();
        }
    }   
}
