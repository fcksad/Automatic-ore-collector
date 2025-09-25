using UnityEngine;

public class ToggleWindow : MonoBehaviour
{
    public void Toggle()
    {     
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void Toggle(bool value)
    {
        gameObject.SetActive(value);
    }

}
