using Service;
using UnityEngine;

public class TurretBase : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private TurretConfig _config;

    [Header("Parts")]
    [Tooltip("Rotate around Y (base yaw)")]
    [SerializeField] private Transform basePivot;   
    [Tooltip("Rotate around X (head pitch) - local X rotates head up/down")]
    [SerializeField] private Transform headPivot;  
    [Tooltip("Where raycast starts and muzzle particle is placed")]
    [SerializeField] private Transform muzzle;

    private Transform _target;
    private float _nextFireTime;
    private float _nextScanTime;

    private bool _returning = false;


    private Quaternion _baseInitialRotWorld;
    private Quaternion _headInitialRotLocal;
    private float _halfHor;   
    private float _halfVer;   


    private IAudioService _audioService;

    private void Awake()
    {
        if (!basePivot) basePivot = transform;
        if (!headPivot) headPivot = basePivot;
        if (!muzzle) muzzle = headPivot;

        _baseInitialRotWorld = basePivot.rotation;
        _headInitialRotLocal = headPivot.localRotation;

        _halfHor = Mathf.Max(0f, _config.MaxHorizontalAngle * 0.5f);
        _halfVer = Mathf.Max(0f, _config.MaxVerticalAngle * 0.5f);

        _audioService = ServiceLocator.Get<IAudioService>();
    }

    private void Update()
    {
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + _config.ScanInterval;
            AcquireTargetInRadius();
        }

        if (_target)
        {
            AimAtTarget(Time.deltaTime);
            TryFireShot();
        }
        else if (_config.ReturnToRest)
        {
            ReturnToRest(Time.deltaTime);
        }
    }

    private void AcquireTargetInRadius()
    {
        _target = null;

        Collider[] hits = Physics.OverlapSphere(basePivot.position, _config.DetectionRadius, _config.TargetMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        float bestSqr = float.PositiveInfinity;
        Transform best = null;

        Vector3 origin = basePivot.position;
        foreach (var h in hits)
        {
            if (!h || h.transform == transform) continue;

            Vector3 to = h.bounds.center - origin;
            to.y = 0f; 

            float sqr = to.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = h.transform;
            }
        }

        _target = best;
    }

    private void AimAtTarget(float dt)
    {
        if (!_target) return;

        Vector3 origin = basePivot.position;
        Vector3 toTarget = _target.position - origin;

        Vector3 toFlat = toTarget; toFlat.y = 0f;
        if (toFlat.sqrMagnitude > 0.0001f)
        {
            toFlat.Normalize();

            Vector3 baseInitialFwd = _baseInitialRotWorld * Vector3.forward;
            float desiredYaw = SignedAngleOnPlane(baseInitialFwd, toFlat, Vector3.up);
            float clampedYaw = Mathf.Clamp(desiredYaw, -_halfHor, _halfHor);

            Quaternion targetBaseRot = Quaternion.AngleAxis(clampedYaw, Vector3.up) * _baseInitialRotWorld;

            basePivot.rotation = Quaternion.RotateTowards(basePivot.rotation, targetBaseRot, _config.HorizontalRotationSpeed * dt);
        }

        Vector3 headPos = headPivot.position;
        Vector3 lookDir = (_target.position - headPos);
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredHeadWorld = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            Transform parent = headPivot.parent ? headPivot.parent : basePivot.parent;
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
            headPivot.localRotation = Quaternion.RotateTowards(headPivot.localRotation, targetLocalRot, _config.VerticalRotationSpeed * dt);
        }

        _returning = false;
    }

    private void ReturnToRest(float dt)
    {
        basePivot.rotation = Quaternion.RotateTowards(
            basePivot.rotation, _baseInitialRotWorld, _config.HorizontalRotationSpeed * _config.ReturnSpeedMul * dt);

        headPivot.localRotation = Quaternion.RotateTowards(
            headPivot.localRotation, _headInitialRotLocal, _config.VerticalRotationSpeed * _config.ReturnSpeedMul * dt);

        if (_returning == false)
        {
            _returning = true;
            _audioService.Play(_config.NoTargetSound, parent: transform, position: transform.position, maxSoundDistance: _config.MaxDistanceSound);
        }

    }

    // ----------- Firing -----------

    private void TryFireShot()
    {
        if (Time.time < _nextFireTime) return;
        if (!muzzle) return;

        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward;

        if (_target)
        {
            Vector3 toTarget = (_target.position - origin).normalized;
            float aimErr = Vector3.Angle(dir, toTarget);
            if (aimErr > _config.FireAngleTolerance)
                return;
        }

        if (Physics.Raycast(origin, dir, out RaycastHit hit, _config.DetectionRadius, _config.TargetMask, QueryTriggerInteraction.Ignore))
        {
            _nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, _config.FireRate);


            _audioService.Play(_config.ShotSound, parent: transform, position: transform.position, maxSoundDistance: _config.MaxDistanceSound);
            /*            if (_config.MuzzleParticle)
                            _config.MuzzleParticle.Play(true);*/

            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.ApplyDamage(_config.Damage);
                Debug.Log($"[Turret] Hit {hit.collider.name} damage={_config.Damage:0.##} at {hit.point}");
            }
            else
            {
                Debug.Log($"[Turret] Ray hit {hit.collider.name} (no IDamageable) at {hit.point}");
            }
        }
        else
        {

        }
    }

    private static float NormalizeAngle(float euler)
    {
        float a = Mathf.Repeat(euler + 180f, 360f) - 180f;
        return a;
    }

    private static float DeltaAngle(float from, float to)
    {
        return Mathf.DeltaAngle(from, to);
    }

    private static float WrapAngle(float signed)
    {
        return (signed % 360f + 360f) % 360f;
    }

    private static float SignedAngleOnPlane(Vector3 vFrom, Vector3 vTo, Vector3 planeN)
    {
        vFrom = Vector3.ProjectOnPlane(vFrom, planeN).normalized;
        vTo = Vector3.ProjectOnPlane(vTo, planeN).normalized;
        if (vFrom.sqrMagnitude < 1e-6f || vTo.sqrMagnitude < 1e-6f) return 0f;
        float unsigned = Vector3.Angle(vFrom, vTo);
        float sign = Mathf.Sign(Vector3.Dot(planeN, Vector3.Cross(vFrom, vTo)));
        return unsigned * sign;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(basePivot ? basePivot.position : transform.position, _config.DetectionRadius);

        if (muzzle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * _config.DetectionRadius);
        }
    }
   
}
