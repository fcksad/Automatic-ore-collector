using System;
using System.Threading.Tasks;

public interface ISceneService 
{
    event Action OnSceneLoadEvent;
    event Action OnSceneUnloadEvent;

    Task Transition(string sceneToLoad);
}
