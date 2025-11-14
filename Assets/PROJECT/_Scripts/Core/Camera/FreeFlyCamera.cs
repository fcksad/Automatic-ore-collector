using UnityEngine;
using Service;
using System;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Settings")]
    public float MoveSpeed = 5f;
    public float FastMultiplier = 4f;
    public float LookSensitivity = 2f;

    private Camera _cam;
    private IInputService _input;

    private float _yaw;
    private float _pitch;

    private bool _freeFlyEnabled = false;

    private Action _onCameraModePerformed;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _input = ServiceLocator.Get<IInputService>();

        EnableFreeFly(true);

        var rot = transform.rotation.eulerAngles;
        _yaw = rot.y;
        _pitch = rot.x;
    }

    private void Start()
    {
        if (_input == null) return;

        _onCameraModePerformed = ToggleCameraMode;
        _input.AddActionListener(
            CharacterAction.CameraMode,
            onPerformed: _onCameraModePerformed
        );
    }

    private void OnDestroy()
    {
        if (_input != null && _onCameraModePerformed != null)
        {
            _input.RemoveActionListener(
                CharacterAction.CameraMode,
                onPerformed: _onCameraModePerformed
            );
        }
    }

    private void Update()
    {
        if (_input == null) return;

        if (!_freeFlyEnabled)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        Vector2 look = _input.GetVector2(CharacterAction.Look);

        _yaw += look.x * LookSensitivity;
        _pitch -= look.y * LookSensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandleMovement()
    {
        Vector2 move = _input.GetVector2(CharacterAction.Move);

        Vector3 dir =
            transform.forward * move.y +
            transform.right * move.x;

        float speed = MoveSpeed;

        if (_input.IsPressed(CharacterAction.CameraSprint))
            speed *= FastMultiplier;

        transform.position += dir * speed * Time.deltaTime;
    }

    private void ToggleCameraMode()
    {
        EnableFreeFly(!_freeFlyEnabled);
    }

    private void EnableFreeFly(bool enable)
    {
        _freeFlyEnabled = enable;

        if (enable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var rot = transform.rotation.eulerAngles;
            _yaw = rot.y;
            _pitch = rot.x;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
