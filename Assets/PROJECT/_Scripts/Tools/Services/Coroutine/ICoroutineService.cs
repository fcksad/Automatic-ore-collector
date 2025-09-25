using System;
using System.Collections;

namespace Service.Coroutines
{
    public interface ICoroutineService : IInitializable, IDisposable
    {
        IRoutine Start(IEnumerator routine);
        IRoutine Start(Func<IEnumerator> factory);

        IRoutine Delay(float seconds, Action callback);
        IRoutine NextFrame(Action callback);
        IRoutine Every(float intervalSeconds, Action tick, bool invokeImmediately = false);

        void AddOnUpdate(Action<float> onUpdate);
        void RemoveOnUpdate(Action<float> onUpdate);


        void Stop(IRoutine routine);
        void StopAll();
    }
}
