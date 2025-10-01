using System.Collections.Generic;
using UnityEngine;

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

    public void Move(Vector2 value) => _moveInput = Mathf.Clamp(value.y, -1f, 1f);
    public void Rotate(Vector2 value) => _turnInput = Mathf.Clamp(-value.x, -1f, 1f);

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
}
