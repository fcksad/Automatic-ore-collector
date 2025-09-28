using UnityEngine;

public enum Mode { RTS, Follow }

public class CameraController : MonoBehaviour
{
    [Header("Mode")]
    public Mode Mode = Mode.RTS;

    [Header("Rig (Yaw → Pitch → Boom → Camera)")]
    [SerializeField] private Transform _pivotYaw;      // вращение по Y (горизонт)
    [SerializeField] private Transform _pivotPitch;    // наклон по X (для RTS)
    [SerializeField] private Transform _boom;          // локальный Z — «стрела» zoom для RTS
    [SerializeField] private Camera _cam;

    [Header("RTS Movement")]
    [SerializeField] private float _rtsMoveSpeed = 12f;
    [Range(0, 1f)][SerializeField] private float _rtsMoveDamping = 0.15f;

    [Header("RTS Bounds")]
    [SerializeField] private bool _useBounds = false;
    [SerializeField] private Vector2 _minXZ = new(-100, -100);
    [SerializeField] private Vector2 _maxXZ = new(100, 100);

    [Header("RTS Zoom (boom Z)")]
    [SerializeField] private float _rtsZoomSpeed = 25f;
    [SerializeField] private float _rtsMinBoom = -6f;    // ближе к 0 — ближе камера
    [SerializeField] private float _rtsMaxBoom = -40f;   // дальше от 0 — дальше камера
    [SerializeField] private float _rtsPitchDeg = 55f;   // наклон взгляда сверху

    [Header("Follow Target")]
    [SerializeField] private Transform _followTarget;
    [SerializeField] private Vector3 _followLocalOffset = new(0, 3f, -6f);
    [SerializeField] private float _followPosDamping = 0.15f;
    [SerializeField] private float _followRotDamping = 0.15f;

    [Header("Follow Zoom (distance)")]
    [SerializeField] private float _followMinDistance = 3f;
    [SerializeField] private float _followMaxDistance = 12f;
    [SerializeField] private float _followZoomSpeed = 8f;

    [Header("Common")]
    [SerializeField] private float _scrollSensitivity = 1f;

    // runtime
    private Vector2 _moveInput;
    private float _zoomInput;
    private float _followDist01 = 0.5f; // [0..1] для интерполяции Min..Max
    private Vector3 _rtsVel;

    public float RtsMoveSpeed => _rtsMoveSpeed;

    private void Awake()
    {
        if (!_pivotYaw) _pivotYaw = transform;
        if (!_pivotPitch) _pivotPitch = _pivotYaw;
        if (!_boom) _boom = _pivotPitch;
        if (!_cam) _cam = Camera.main;

        if (Mode == Mode.RTS)
        {
            var e = _pivotPitch.localEulerAngles;
            e.x = _rtsPitchDeg;
            _pivotPitch.localEulerAngles = e;
        }
    }

    private void Update()
    {
        ApplyZoom();
        if (Mode == Mode.RTS) TickRTS(); else TickFollow();
        _zoomInput = 0f;
    }

    // API для драйвера
    public void FeedMove(Vector2 move) => _moveInput = move;
    public void FeedZoom(float scrollDelta) => _zoomInput += scrollDelta * _scrollSensitivity;

    public void SetRTSMode()
    {
        Mode = Mode.RTS;
        var e = _pivotPitch.localEulerAngles; e.x = _rtsPitchDeg; _pivotPitch.localEulerAngles = e;
    }

    public void SetFollowMode(Transform target) { _followTarget = target; Mode = Mode.Follow; }
    public void SetFollowTarget(Transform target) => _followTarget = target;

    // ===== RTS =====
    private void TickRTS()
    {
        Vector3 right = _pivotYaw.right; right.y = 0; right.Normalize();
        Vector3 fwd = _pivotYaw.forward; fwd.y = 0; fwd.Normalize();

        Vector3 desiredDelta = (right * _moveInput.x + fwd * _moveInput.y) * _rtsMoveSpeed * Time.deltaTime;
        Vector3 targetPos = transform.position + desiredDelta;

        if (_useBounds) targetPos = ClampXZ(targetPos, _minXZ, _maxXZ);

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _rtsVel, _rtsMoveDamping);
    }

    private void ApplyZoom()
    {
        if (Mathf.Abs(_zoomInput) <= Mathf.Epsilon) return;

        if (Mode == Mode.RTS)
        {
            Vector3 lp = _boom.localPosition;
            lp.z += -_zoomInput * _rtsZoomSpeed * Time.deltaTime; // колесо «вверх» — приблизить
            lp.z = Mathf.Clamp(lp.z, _rtsMaxBoom, _rtsMinBoom);  // т.к. boom.z отрицательный
            _boom.localPosition = lp;
        }
        else // Follow
        {
            float t = Mathf.InverseLerp(_followMinDistance, _followMaxDistance, CurrentFollowDistance());
            t = Mathf.Clamp01(t - _zoomInput * (_followZoomSpeed * 0.1f) * Time.deltaTime);
            _followDist01 = t;
        }
    }

    // ===== Follow =====
    private void TickFollow()
    {
        if (!_followTarget) return;

        float dist = Mathf.Lerp(_followMinDistance, _followMaxDistance, _followDist01);
        Vector3 wantedOffset = _followLocalOffset.normalized * dist;
        Vector3 wantedPos = _followTarget.TransformPoint(wantedOffset);

        float kPos = 1f - Mathf.Exp(-_followPosDamping * 60f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, wantedPos, kPos);

        float kRot = 1f - Mathf.Exp(-_followRotDamping * 60f * Time.deltaTime);
        Quaternion look = Quaternion.LookRotation((_followTarget.position - transform.position).normalized, Vector3.up);
        _pivotYaw.rotation = Quaternion.Slerp(_pivotYaw.rotation, look, kRot);
    }

    private float CurrentFollowDistance() => Mathf.Max(0.01f, _followLocalOffset.magnitude);

    private static Vector3 ClampXZ(Vector3 p, Vector2 minXZ, Vector2 maxXZ)
    {
        p.x = Mathf.Clamp(p.x, minXZ.x, maxXZ.x);
        p.z = Mathf.Clamp(p.z, minXZ.y, maxXZ.y);
        return p;
    }
}
