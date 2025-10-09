using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class VehicleBase : MonoBehaviour, IControllable , IDamageable
{
    [field: SerializeField] public Collider MainCollider {  get; private set; }

    [Header("Configurations")]
    [SerializeField] private VehicleConfig _vehicleConfig;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private VehicleController _controller;

    [Header("Decals")]
    [SerializeField] private TrackStampsInstanced _trackStampsInstanced;
    [SerializeField] private List<Transform> _decalPoints = new List<Transform>();
    private Vector3[] _lastDecalPos;
    private float _moveInput, _turnInput;

    private void Awake()
    {
        _trackStampsInstanced.ApplyConfig(_vehicleConfig.TrackStamps);

        _lastDecalPos = new Vector3[_decalPoints.Count];
        for (int i = 0; i < _decalPoints.Count; i++)
            _lastDecalPos[i] = _decalPoints[i] ? _decalPoints[i].position : transform.position;

        SetToControl();
    }

    public void Move(float value) => _moveInput = Mathf.Clamp(value, -1f, 1f);
    public void Rotate(float value) => _turnInput = Mathf.Clamp(-value, -1f, 1f);

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        Vector3 delta = transform.forward * (_moveInput * _vehicleConfig.MoveSpeed * dt);
        delta.y = 0f; 
        _rb.MovePosition(_rb.position + delta);

        float deltaYaw = -_turnInput * _vehicleConfig.RotateSpeed * dt;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));

        TrySpawnDecals();
    }

    private void TrySpawnDecals()
    {
        var cfg = _vehicleConfig.TrackStamps;
        if (cfg == null) return;

        float spacing = Mathf.Max(0.01f, cfg.Spacing);

        for (int i = 0; i < _decalPoints.Count; i++)
        {
            var p = _decalPoints[i];
            if (!p) continue;

            Vector3 prev = _lastDecalPos[i];
            Vector3 curr = p.position;

            Vector3 delta = curr - prev; delta.y = 0f;
            float dist = delta.magnitude;
            if (dist <= Mathf.Epsilon || dist < spacing) continue;

            Vector3 dir = delta.normalized;
            float remaining = dist;
            Vector3 spawnPos = prev;

            int safety = 0;
            const int MAX_PER_POINT_PER_FRAME = 16;

            while (remaining >= spacing && safety++ < MAX_PER_POINT_PER_FRAME)
            {
                spawnPos += dir * spacing;

                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                if (cfg.Randomize) yaw += Random.Range(-cfg.RandomRot, cfg.RandomRot);

                Vector3 pos = spawnPos;
                if (cfg.Randomize)
                {
                    var off = Random.insideUnitCircle * cfg.RandomOffset;
                    pos += new Vector3(off.x, 0f, off.y);
                }

                Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
                _trackStampsInstanced.Add(pos, rot);

                remaining -= spacing;
            }

            _lastDecalPos[i] = curr - dir * remaining;
        }
    }

    [ContextMenu("Set to controll")]
    public void SetToControl() => _controller.SetVehicle(this);

    public void ApplyDamage(float damage)
    {
        Debug.Log("Taked" +  damage);
    }


#if UNITY_EDITOR


private static readonly Color _stampFill = new Color(0f, 0.8f, 1f, 0.10f);
private static readonly Color _stampLine = new Color(0f, 0.8f, 1f, 0.90f);
private static GUIStyle _labelStyle;

private void OnDrawGizmos()
{
    if (_vehicleConfig == null) return;
    var cfg = _vehicleConfig.TrackStamps;
    if (cfg == null) return;

    if (_decalPoints == null || _decalPoints.Count == 0) return;

    var tsi = _trackStampsInstanced;
    bool proj = tsi != null && tsi.enabled && tsi.gameObject.activeInHierarchy && tsi.GroundOnly;

    if (_labelStyle == null)
    {
        _labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0f, 0.95f, 1f, 0.95f) }
        };
    }

    Handles.zTest = CompareFunction.LessEqual; 

    int previewCount = 4; 
    float spacing = Mathf.Max(0.01f, cfg.Spacing);
    Vector2 size = cfg.StampSize; 

    foreach (var p in _decalPoints)
    {
        if (!p) continue;


        Vector3 pos = p.position;
        Quaternion rot = transform.rotation;

        if (proj)
        {
            Vector3 rayStart = pos + Vector3.up * tsi.RaycastStart;
            float dist = tsi.RaycastDistance + tsi.RaycastStart;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, dist, tsi.GroundMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + hit.normal * tsi.GroundStick;
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(hit.normal, Vector3.right).normalized;
                rot = Quaternion.LookRotation(fwd, hit.normal);
            }
            else
            {

                pos += Vector3.up * (tsi ? tsi.GroundStick : 0f);
                rot = Quaternion.LookRotation(transform.forward, Vector3.up);
            }
        }
        else
        {
            pos += Vector3.up * (tsi ? tsi.GroundStick : 0f);
            rot = Quaternion.LookRotation(transform.forward, Vector3.up);
        }


        for (int i = 0; i < previewCount; i++)
        {
            float alphaMul = Mathf.Lerp(1f, 0.2f, i / (float)(previewCount - 1));
            Vector3 pStamp = pos + (rot * Vector3.forward) * spacing * i;
            DrawStampRect(pStamp, rot, size, _stampFill * alphaMul, _stampLine);

            if (i == 0)
            {

                Vector3 right = rot * Vector3.right * (size.x * 0.5f);
                Vector3 fwd = rot * Vector3.forward * (size.y * 0.5f);

                Handles.DrawLine(pStamp - right, pStamp + right);
                Handles.DrawLine(pStamp - fwd, pStamp + fwd);

                Handles.Label(pStamp + right + Vector3.up * 0.02f, $"Width X: {size.x:F2}m", _labelStyle);
                Handles.Label(pStamp + fwd + Vector3.up * 0.02f, $"Length Y: {size.y:F2}m", _labelStyle);

                Handles.ArrowHandleCap(0, pStamp, rot, size.y * 0.75f, EventType.Repaint);
            }
        }
    }
}

private static void DrawStampRect(Vector3 center, Quaternion rot, Vector2 size, Color fill, Color line)
{
    Vector3 right = rot * Vector3.right * (size.x * 0.5f);
    Vector3 fwd = rot * Vector3.forward * (size.y * 0.5f);

    Vector3 a = center - right - fwd;
    Vector3 b = center - right + fwd;
    Vector3 c = center + right + fwd;
    Vector3 d = center + right - fwd;

    Handles.DrawSolidRectangleWithOutline(new[] { a, b, c, d }, fill, line);
    Handles.DrawAAPolyLine(3f, a, b, c, d, a);
}
#endif
}
