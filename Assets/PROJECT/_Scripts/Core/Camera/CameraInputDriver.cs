using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;   // <-- Input System
using Service;                   // твой сервис ввода

public class CameraInputDriver : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private CameraController _controller;
    [SerializeField] private Transform _followTarget; // опционально, если уже есть цель

    [Header("Actions")]
    [SerializeField] private CharacterAction _actCamMove = CharacterAction.Move;
    [SerializeField] private CharacterAction _actCamZoom = CharacterAction.Scroll;

    [Header("RTS edge pan")]
    [SerializeField] private bool _edgePanEnabled = true;
    [SerializeField, Min(1f)] private float _edgeThickness = 12f;   // px
    [SerializeField, Min(0f)] private float _edgePanSpeed = 1f;     // множитель к RTS speed
    [SerializeField] private bool _ignoreWhenPointerOverUI = true;

    [Header("Zoom")]
    [SerializeField] private float _zoomMul = 1f;
    [SerializeField] private bool _invertZoom = false;

    private IInputService _input;

    private void Awake()
    {
        if (!_controller) _controller = GetComponent<CameraController>();
        _input = ServiceLocator.Get<IInputService>();
        if (_followTarget) _controller.SetFollowTarget(_followTarget);
    }

    private void Update()
    {
        Debug.LogError(_input.GetVector2(CharacterAction.Scroll));

        if (_input == null || _controller == null) return;

        switch (_controller.Mode)
        {
            case Mode.RTS: DriveRTS(); break;
            case Mode.Follow: DriveFollow(); break;
        }
    }

    private void DriveRTS()
    {
        Vector2 move = _input.GetVector2(_actCamMove);

        // Edge pan через Input System
        if (_edgePanEnabled && Application.isFocused)
        {
            bool overUI = _ignoreWhenPointerOverUI && EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            if (!overUI)
            {
                Vector2 edge = EdgePanVector(_edgeThickness);
                if (edge.sqrMagnitude > 0f)
                {
                    // нормируем относительно скорости перемещени€ контроллера
                    move += edge * _edgePanSpeed;
                }
            }
        }

        if (move.sqrMagnitude > 1f) move.Normalize();
        _controller.FeedMove(move);

        //  олесо
        float scroll = _input.GetVector2(_actCamZoom).y;
        scroll = (_invertZoom ? -scroll : scroll) * _zoomMul;
        if (!_ignoreWhenPointerOverUI || !EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
        {
            if (Mathf.Abs(scroll) > Mathf.Epsilon)
                _controller.FeedZoom(scroll);
        }
    }

    private void DriveFollow()
    {
        _controller.FeedMove(Vector2.zero);

        float scroll = _input.GetVector2(_actCamZoom).y;
        scroll = (_invertZoom ? -scroll : scroll) * _zoomMul;
        if (!_ignoreWhenPointerOverUI || !EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
        {
            if (Mathf.Abs(scroll) > Mathf.Epsilon)
                _controller.FeedZoom(scroll);
        }
    }

    private static Vector2 EdgePanVector(float thickness)
    {
        if (Mouse.current == null) return Vector2.zero;
        Vector2 mp = Mouse.current.position.ReadValue();
        float w = Screen.width, h = Screen.height;

        float x = 0f, y = 0f;
        if (mp.x <= thickness) x = -1f;
        else if (mp.x >= w - thickness) x = +1f;
        if (mp.y <= thickness) y = -1f;
        else if (mp.y >= h - thickness) y = +1f;

        Vector2 dir = new Vector2(x, y);
        return dir.sqrMagnitude > 0f ? dir.normalized : Vector2.zero;
    }

    public void SetFollowTarget(Transform t)
    {
        _followTarget = t;
        _controller.SetFollowTarget(t);
    }
}
