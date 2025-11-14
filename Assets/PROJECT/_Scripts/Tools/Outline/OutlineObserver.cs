using UnityEngine;
using UnityEngine.InputSystem;

//мб сделать сервисом
public class OutlineObserver : MonoBehaviour
{
    [Header("Raycast")]
    public Camera Cam;
    public LayerMask HoverMask = ~0;    
    public float MaxDistance = 1000f;

    private Outline _currentOutline;

    private void Awake()
    {
        if (!Cam) Cam = Camera.main;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        var screenPos = mouse.position.ReadValue();

        if (UIChecker.IsOverUI(screenPos))
        {
            SetCurrentOutline(null);
            return;
        }

        var ray = Cam.ScreenPointToRay(screenPos);

        Outline hitOutline = null;

        if (Physics.Raycast(ray, out var hit, MaxDistance, HoverMask))
        {
            hitOutline = hit.collider.GetComponentInParent<Outline>();
        }

        SetCurrentOutline(hitOutline);
    }

    private void SetCurrentOutline(Outline newOutline)
    {
        if (_currentOutline == newOutline)
            return;

        if (_currentOutline != null)
            _currentOutline.OutlineMode = Outline.Mode.Hidden;

        _currentOutline = newOutline;

        if (_currentOutline != null)
            _currentOutline.OutlineMode = Outline.Mode.Enabled;
    }

    private void OnDisable()
    {
        if (_currentOutline != null)
            _currentOutline.OutlineMode = Outline.Mode.Hidden;

        _currentOutline = null;
    }
}