using System;

public interface IDialogueService
{
    void Show(DialogueConfig config, Action onCompleted);
    void Stop();
}
