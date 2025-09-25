using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CustomButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    public Button Button
    {
        get
        {
            if (this == null) return null;
            if (!this) return null; 

            if (!_button)
            {
                if (!TryGetComponent(out _button))
                    return null;
            }
            return _button;
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }
    }
#endif

}
