using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintImage : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _text;

    public void SetImage(Sprite icon)
    {
        _image.sprite = icon;
    }

    public void SetText(string controlButton)
    {
        _text.SetText(controlButton.ToUpperInvariant());
    }
}
