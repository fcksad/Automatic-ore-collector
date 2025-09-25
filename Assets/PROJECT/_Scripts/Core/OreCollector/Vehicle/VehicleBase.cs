using Service;
using System.Collections.Generic;
using UnityEngine;

public class VehicleBase : MonoBehaviour, IControllable
{

    [Header("Configurations")]
    [SerializeField] private VehicleConfig _vehicleConfig;
    [SerializeField] private Rigidbody2D _rb;
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

        _rb ??= GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        _lastDecalPos = new Vector3[_decalPoints.Count];
        for (int i = 0; i < _decalPoints.Count; i++)
            _lastDecalPos[i] = _decalPoints[i] ? _decalPoints[i].position : transform.position;

        SetToControl();
    }

    public void Move(Vector2 value) => _moveInput = Mathf.Clamp(-value.y, -1f, 1f);
    public void Rotate(Vector2 value) => _turnInput = Mathf.Clamp(value.x, -1f, 1f);

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        _rb.linearVelocity = (Vector2)transform.up * (_moveInput * _vehicleConfig.MoveSpeed);

        float deltaDeg = -_turnInput * _vehicleConfig.RotateSpeed * deltaTime;
        _rb.MoveRotation(_rb.rotation + deltaDeg);

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

            Vector2 delta = (Vector2)(curr - prev);
            float dist = delta.magnitude;
            if (dist < spacing) continue;

            Vector2 dir = delta.normalized;

            float remaining = dist;
            Vector3 spawnPos = prev;

            int safety = 0;
            const int MAX_PER_POINT_PER_FRAME = 16;

            while (remaining >= spacing && safety++ < MAX_PER_POINT_PER_FRAME)
            {
                spawnPos += (Vector3)(dir * spacing);

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Vector3 pos = spawnPos;
                if (_vehicleConfig.DecalRandomize)
                {
                    angle += Random.Range(-_vehicleConfig.DecalRandomRot, _vehicleConfig.DecalRandomRot);
                    Vector2 off = Random.insideUnitCircle * _vehicleConfig.DecalRandomOffset;
                    pos += new Vector3(off.x, off.y, 0f);
                }

                SpawnDecal(pos, angle);
                remaining -= spacing;
            }

            _lastDecalPos[i] = curr - (Vector3)(dir * remaining);
        }
    }

    private void SpawnDecal(Vector3 worldPos, float worldAngleDeg)
    {
        var decal = _instantiateFactoryService.Create(_vehicleConfig.DecalPrefab, position: worldPos, rotation: Quaternion.Euler(0f, 0f, worldAngleDeg), parent: _decalParent);

        decal.Init(_vehicleConfig.DecalLifeTime, _vehicleConfig.DecalFadeTime, _instantiateFactoryService);
    }

    [ContextMenu("Set to controll")]
    public void SetToControl() => _controller.SetVehicle(this);


}
