

using System;

namespace Service.Tweener
{
    public interface ITweener
    {
        bool IsPlaying { get; }
        bool IsComplete { get; }

        ITweener Pause();
        ITweener Resume();
        ITweener Kill(bool complete = false);

        ITweener OnComplete(Action cb);
        ITweener OnUpdate(Action cb);
    }
}

