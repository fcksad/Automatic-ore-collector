using Service;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Image))]
public class HintImageListener : MonoBehaviour
{
    [SerializeField] private CharacterAction _action;
    [SerializeField] private Image _image;

    private IHintService _hintService;


    private void OnEnable()
    {
        _hintService = ServiceLocator.Get<IHintService>();
        Refresh();
    }

    private void OnControlsChanged(PlayerInput _)
    {
        Refresh();
    }

    private void OnDeviceChange(InputDevice dev, InputDeviceChange chg)
    {
        if (chg == InputDeviceChange.Added || chg == InputDeviceChange.Removed || chg == InputDeviceChange.Reconnected)
            Refresh();
    }

    public void Refresh()
    {
        if (_hintService == null) return;

        Sprite sprite = null;

        sprite = _hintService.GetHintSprite(_action);

        if (sprite != null)
        {
            _image.enabled = true;
            _image.sprite = sprite;

        }
        else
        {
            _image.sprite = null;
        }
    }
}
