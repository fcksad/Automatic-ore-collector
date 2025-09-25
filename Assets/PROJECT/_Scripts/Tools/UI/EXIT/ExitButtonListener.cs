using UnityEngine;

public class ExitButtonListener : CustomButton
{
    private void Start()
    {
        Button.onClick.AddListener(ExitGame);
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
