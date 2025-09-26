using Service;
using System.Collections.Generic;
using UnityEngine;

public class TurretBase : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private TurretConfig _config;

    [Header("Parts")]
    [Tooltip("Rotate around Y (base yaw)")]
    [SerializeField] private Transform _basePivot;   
    [Tooltip("Rotate around X (head pitch) - local X rotates head up/down")]
    [SerializeField] private Transform _headPivot;  
    [Tooltip("Where raycast starts and muzzle particle is placed")]
    [SerializeField] private List<Transform> _muzzlePos;

    private ITargetable _target;
    private float _nextFireTime;
    private float _nextScanTime;

    private bool _returning = false;


    private Quaternion _baseInitialRotWorld;
    private Quaternion _headInitialRotLocal;
    private float _halfHor;   
    private float _halfVer;   


    private IAudioService _audioService;
    private IParticleService _particleService;

    private void Awake()
    {
        _baseInitialRotWorld = _basePivot.rotation;
        _headInitialRotLocal = _headPivot.localRotation;

        _halfHor = Mathf.Max(0f, _config.MaxHorizontalAngle * 0.5f);
        _halfVer = Mathf.Max(0f, _config.MaxVerticalAngle * 0.5f);

        _audioService = ServiceLocator.Get<IAudioService>();
        _particleService = ServiceLocator.Get<IParticleService>();
    }

    private void Update()
    {
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + _config.ScanInterval;
            if (!IsTargetValid(_target))
                AcquireTargetInRadius();
        }

        if (_target != null)
        {
            AimAtTarget(Time.deltaTime);
            TryFireShot();
        }
        else if (_config.ReturnToRest)
        {
            ReturnToRest(Time.deltaTime);
        }
    }

    private void SetTarget(ITargetable t)
    {
        if (_target == t) return;

        if (_target != null)
            _target.BecameUnavailable -= OnTargetUnavailable;

        _target = t;

        if (_target != null)
            _target.BecameUnavailable += OnTargetUnavailable;
    }

    private void OnTargetUnavailable(ITargetable t)
    {
        if (_target == t)
            SetTarget(null);
    }

    private bool IsTargetValid(ITargetable t)
    {
        if (t == null) return false;
        if (!t.IsAlive) return false;

        var tr = t.TargetTransform;
        if (!tr || !tr.gameObject.activeInHierarchy) return false;

        if ((tr.position - _basePivot.position).sqrMagnitude > _config.DetectionRadius * _config.DetectionRadius)
            return false;

        Vector3 origin = _headPivot.position;
        Vector3 dir = (tr.position - origin);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return true;

        dir /= dist;
        if (Physics.Raycast(origin, dir, out var hit, Mathf.Min(dist, _config.DetectionRadius), ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(tr)) return false;
        }
        return true;
    }

    private void AcquireTargetInRadius()
    {
        SetTarget(null);

        var hits = Physics.OverlapSphere(_basePivot.position, _config.DetectionRadius,
                                         _config.TargetMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        float bestSqr = float.PositiveInfinity;
        ITargetable best = null;

        Vector3 origin = _basePivot.position;
        foreach (var h in hits)
        {
            if (!h) continue;

            // ищем ITargetable на коллайдере/родителях
            var t = h.GetComponentInParent<ITargetable>();
            if (t == null || !t.IsAlive) continue;

            // не берем саму турель
            if (t.TargetTransform && t.TargetTransform == transform) continue;

            Vector3 to = h.bounds.center - origin;
            to.y = 0f;
            float sqr = to.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        SetTarget(best);
    }

    private void AimAtTarget(float dt)
    {
        if (_target == null) return;
        var tr = _target.TargetTransform;
        if (!tr) { SetTarget(null); return; }

        Vector3 origin = _basePivot.position;
        Vector3 toTarget = tr.position - origin;

        Vector3 toFlat = toTarget; toFlat.y = 0f;
        if (toFlat.sqrMagnitude > 0.0001f)
        {
            toFlat.Normalize();
            Vector3 baseInitialFwd = _baseInitialRotWorld * Vector3.forward;
            float desiredYaw = SignedAngleOnPlane(baseInitialFwd, toFlat, Vector3.up);
            float clampedYaw = Mathf.Clamp(desiredYaw, -_halfHor, _halfHor);
            Quaternion targetBaseRot = Quaternion.AngleAxis(clampedYaw, Vector3.up) * _baseInitialRotWorld;

            _basePivot.rotation = Quaternion.RotateTowards(_basePivot.rotation, targetBaseRot,
                                                           _config.HorizontalRotationSpeed * dt);
        }

        Vector3 headPos = _headPivot.position;
        Vector3 lookDir = (tr.position - headPos);
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredHeadWorld = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            Transform parent = _headPivot.parent ? _headPivot.parent : _basePivot.parent;
            Quaternion parentWorld = parent ? parent.rotation : Quaternion.identity;
            Quaternion desiredLocal = Quaternion.Inverse(parentWorld) * desiredHeadWorld;

            Vector3 initLocalEuler = _headInitialRotLocal.eulerAngles;
            float initPitch = NormalizeAngle(initLocalEuler.x);
            float desiredPitch = NormalizeAngle(desiredLocal.eulerAngles.x);

            float deltaFromInit = DeltaAngle(initPitch, desiredPitch);
            float clampedDelta = Mathf.Clamp(deltaFromInit, -_halfVer, _halfVer);
            float finalPitch = initPitch + clampedDelta;

            Vector3 targetLocalEuler = desiredLocal.eulerAngles;
            targetLocalEuler.x = WrapAngle(finalPitch);

            Quaternion targetLocalRot = Quaternion.Euler(targetLocalEuler);
            _headPivot.localRotation = Quaternion.RotateTowards(_headPivot.localRotation, targetLocalRot, _config.VerticalRotationSpeed * dt);
        }

        _returning = false;
    }

    private void ReturnToRest(float dt)
    {
        _basePivot.rotation = Quaternion.RotateTowards(
        _basePivot.rotation, _baseInitialRotWorld, _config.HorizontalRotationSpeed * _config.ReturnSpeedMul * dt);

        _headPivot.localRotation = Quaternion.RotateTowards(
            _headPivot.localRotation, _headInitialRotLocal, _config.VerticalRotationSpeed * _config.ReturnSpeedMul * dt);

        if (_returning == false)
        {
            _returning = true;
            _audioService.Play(_config.NoTargetSound, parent: transform, position: transform.position, maxSoundDistance: _config.MaxDistanceSound);
        }
    }

    private void TryFireShot()
    {
        if (Time.time < _nextFireTime) return;

        if (_target != null)
        {
            Vector3 originHead = _headPivot.position;
            Vector3 toTarget = (_target.TargetTransform.position - originHead);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float aimErr = Vector3.Angle(_headPivot.forward, toTarget.normalized);
                if (aimErr > _config.FireAngleTolerance) return;
            }
        }

        bool fired = false;
        foreach (var muzzle in _muzzlePos)
        {
            if (!muzzle) continue;

            Vector3 origin = muzzle.position;
            Vector3 dir = muzzle.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, _config.DetectionRadius,
                                _config.TargetMask, QueryTriggerInteraction.Ignore))
            {
                fired = true;

                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null) dmg.ApplyDamage(_config.Damage);

                Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
            }
        }

        if (fired)
        {
            _nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, _config.FireRate);

            _audioService.Play(_config.ShotSound, parent: transform, position: transform.position,
                               maxSoundDistance: _config.MaxDistanceSound);

            foreach (var muzzle in _muzzlePos)
            {
                if (!muzzle) continue;
                _particleService.Play(_config.MuzzleParticle, muzzle, muzzle.position, muzzle.rotation);
            }
        }
    }
    private static float NormalizeAngle(float euler) => Mathf.Repeat(euler + 180f, 360f) - 180f;
    private static float DeltaAngle(float from, float to) => Mathf.DeltaAngle(from, to);
    private static float WrapAngle(float signed) => (signed % 360f + 360f) % 360f;

    private static float SignedAngleOnPlane(Vector3 vFrom, Vector3 vTo, Vector3 planeN)
    {
        vFrom = Vector3.ProjectOnPlane(vFrom, planeN).normalized;
        vTo = Vector3.ProjectOnPlane(vTo, planeN).normalized;
        if (vFrom.sqrMagnitude < 1e-6f || vTo.sqrMagnitude < 1e-6f) return 0f;
        float unsigned = Vector3.Angle(vFrom, vTo);
        float sign = Mathf.Sign(Vector3.Dot(planeN, Vector3.Cross(vFrom, vTo)));
        return unsigned * sign;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(_basePivot ? _basePivot.position : transform.position, _config.DetectionRadius);

        if (_headPivot)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_headPivot.position, _headPivot.position + _headPivot.forward * _config.DetectionRadius);
        }
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

}
