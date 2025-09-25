using Service;
using System.Collections.Generic;
using UnityEngine;

public class VehicleBase : MonoBehaviour, IControllable
{
    [Header("Configurations")]
    [SerializeField] private VehicleConfig _vehicleConfig;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private VehicleController _controller;

    [Header("Decals")]
    [SerializeField] private Transform _decalParent;
    [SerializeField] private List<Transform> _decalPoints = new List<Transform>();
    private Vector3[] _lastDecalPos;

    private float _moveInput;  
    private float _turnInput;

    private IInstantiateFactoryService _instantiateFactoryService;

    private void Awake()
    {
        _instantiateFactoryService = ServiceLocator.Get<IInstantiateFactoryService>();

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

        _rb.linearVelocity = transform.forward * (_moveInput * _vehicleConfig.MoveSpeed);

        float deltaYaw = -_turnInput * _vehicleConfig.RotateSpeed * dt;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));

        TrySpawnDecals();
    }


    private void TrySpawnDecals()
    {
        float spacing = Mathf.Max(0.01f, _vehicleConfig.DecalSpacing);

        for (int i = 0; i < _decalPoints.Count; i++)
        {
            var p = _decalPoints[i];
            if (p == null) continue;

            Vector3 prev = _lastDecalPos[i];
            Vector3 curr = p.position;

            Vector3 delta = curr - prev;
            delta.y = 0f;

            float dist = delta.magnitude;
            if (dist < spacing || dist <= Mathf.Epsilon) continue;

            Vector3 dir = delta.normalized;

            float remaining = dist;
            Vector3 spawnPos = prev;

            int safety = 0;
            const int MAX_PER_POINT_PER_FRAME = 16;

            while (remaining >= spacing && safety++ < MAX_PER_POINT_PER_FRAME)
            {
                spawnPos += dir * spacing;
       
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

                Vector3 pos = spawnPos;
                if (_vehicleConfig.DecalRandomize)
                    yaw += Random.Range(-_vehicleConfig.DecalRandomRot, _vehicleConfig.DecalRandomRot);

                Quaternion rot = Quaternion.Euler(90f, yaw, 0f);
                SpawnDecal(pos, rot);
                remaining -= spacing;
            }

            _lastDecalPos[i] = curr - dir * remaining;
        }
    }

    private void SpawnDecal(Vector3 worldPos, Quaternion worldRot)
    {
        var decal = _instantiateFactoryService.Create(
            _vehicleConfig.DecalPrefab,
            position: worldPos,
            rotation: worldRot,
            parent: _decalParent
        );

        decal.Init(_vehicleConfig.DecalLifeTime, _vehicleConfig.DecalFadeTime, _instantiateFactoryService);
    }

    [ContextMenu("Set to controll")]
    public void SetToControl() => _controller.SetVehicle(this);


}
