using Service;
using UnityEngine;


public class DialogueSceneController : MonoBehaviour
{
    [SerializeField] private DialogueConfig _config;

    private IDialogueService _dialogueService;

    private bool _isShowing = false;

    private void Start()
    {
        _dialogueService = ServiceLocator.Get<IDialogueService>();
    }

    [ContextMenu("Start Dialogue")]
    public void StartDialogue()
    {
        _isShowing = true;

        _dialogueService.Show(_config, OnDialogueComplete);
    }

    private void OnDialogueComplete()
    {
        _isShowing = false;
    }

    private void OnDisable()
    {
        if (_isShowing)
        {
            _dialogueService.Stop();
        }
    }
}
