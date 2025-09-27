using FSM;
using Service;
using System.Collections.Generic;
using UnityEngine;

public class TurretBase : MonoStateMachine<TurretBase>
{
    [Header("Config")]
    [field: SerializeField] public TurretConfig Config { get; private set; }

    [Header("Parts")]
    [Tooltip("Rotate around Y (base yaw)")]
    [SerializeField] private Transform _basePivot;   
    [Tooltip("Rotate around X (head pitch) - local X rotates head up/down")]
    [SerializeField] private Transform _headPivot;  
    [Tooltip("Where raycast starts and muzzle particle is placed")]
    [SerializeField] private List<Transform> _muzzlePos;

    public ITargetable Target { get; private set; }
    public float NextFireTime { get; private set; }
    public float NextScanTime { get; private set; }

    private bool _returning = false;

    private Quaternion _baseInitialRotWorld;
    private Quaternion _headInitialRotLocal;
    private float _halfHor;   
    private float _halfVer;

    public IAudioService AudioService { get; private set; }
    public IParticleService ParticleService { get; private set; }

    protected State<TurretBase> Idle, Track;

    protected override void Awake()
    {
        AudioService = ServiceLocator.Get<IAudioService>();
        ParticleService = ServiceLocator.Get<IParticleService>();

        base.Awake();

        _baseInitialRotWorld = _basePivot.rotation;
        _headInitialRotLocal = _headPivot.localRotation;

        _halfHor = Mathf.Max(0f, Config.MaxHorizontalAngle * 0.5f);
        _halfVer = Mathf.Max(0f, Config.MaxVerticalAngle * 0.5f);

    }

    protected override void BuildStates()
    {
        Idle = new TurretIdle(this);
        Track = new TurretTrack(this);
        FSM.Set(Idle);
#if UNITY_EDITOR
        FSM.Changed += (f, t) => Debug.Log($"[TurretFSM] {name}: {f?.Name ?? "<null>"} -> {t?.Name}");
#endif
    }

    public void SetTarget(ITargetable t)
    {
        if (Target == t) return;

        if (Target != null)
            Target.BecameUnavailable -= OnTargetUnavailable;

        Target = t;

        if (Target != null)
            Target.BecameUnavailable += OnTargetUnavailable;
    }

    public void OnTargetUnavailable(ITargetable t)
    {
        if (Target == t)
            SetTarget(null);
    }

    public bool IsTargetValid(ITargetable t)
    {
        if (t == null) return false;
        if (!t.IsAlive) return false;

        var tr = t.TargetTransform;
        if (!tr || !tr.gameObject.activeInHierarchy) return false;

        if ((tr.position - _basePivot.position).sqrMagnitude > Config.DetectionRadius * Config.DetectionRadius)
            return false;

        Vector3 origin = _headPivot.position;
        Vector3 dir = (tr.position - origin);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return true;

        dir /= dist;
        if (Physics.Raycast(origin, dir, out var hit, Mathf.Min(dist, Config.DetectionRadius), ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(tr)) return false;
        }
        return true;
    }

    public void AcquireTargetInRadius()
    {
        var preferred = FindBestTargetInRadius(preferShootable: true, requireLoSForPrefer: false);
        if (preferred != null)
        {
            SetTarget(preferred);
            return;
        }

        var fallback = FindBestTargetInRadius(preferShootable: false);
        SetTarget(fallback);
    }

    public bool IsTargetInRadiusAlive(ITargetable t)
    {
        if (t == null || !t.IsAlive) return false;
        var tr = t.TargetTransform;
        if (!tr || !tr.gameObject.activeInHierarchy) return false;

        return (tr.position - _basePivot.position).sqrMagnitude <= Config.DetectionRadius * Config.DetectionRadius;
    }

    public void AimAtTarget(float dt)
    {
        if (Target == null) return;
        var tr = Target.TargetTransform;
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

            _basePivot.rotation = Quaternion.RotateTowards(_basePivot.rotation, targetBaseRot, Config.HorizontalRotationSpeed * dt);
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
            _headPivot.localRotation = Quaternion.RotateTowards(_headPivot.localRotation, targetLocalRot, Config.VerticalRotationSpeed * dt);
        }

        _returning = false;
    }

    public bool ScanTick()
    {
        if (Time.time < NextScanTime) return false;
        NextScanTime = Time.time + Config.ScanInterval;
        AcquireTargetInRadius();
        return Target != null;
    }

    public void ReturnToRest(float dt)
    {
        _basePivot.rotation = Quaternion.RotateTowards(
        _basePivot.rotation, _baseInitialRotWorld, Config.HorizontalRotationSpeed * Config.ReturnSpeedMul * dt);

        _headPivot.localRotation = Quaternion.RotateTowards(
            _headPivot.localRotation, _headInitialRotLocal, Config.VerticalRotationSpeed * Config.ReturnSpeedMul * dt);

        if (_returning == false)
        {
            _returning = true;
            AudioService.Play(Config.NoTargetSound, parent: transform, position: transform.position, maxSoundDistance: Config.MaxDistanceSound);
        }
    }

    public void TryFireShot()
    {
        if (Time.time < NextFireTime) return;

        if (Target != null)
        {
            Vector3 originHead = _headPivot.position;
            Vector3 toTarget = (Target.TargetTransform.position - originHead);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float aimErr = Vector3.Angle(_headPivot.forward, toTarget.normalized);
                if (aimErr > Config.FireAngleTolerance) return;
            }
        }

        bool fired = false;
        foreach (var muzzle in _muzzlePos)
        {
            if (!muzzle) continue;

            Vector3 origin = muzzle.position;
            Vector3 dir = muzzle.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, Config.DetectionRadius,Config.TargetMask, QueryTriggerInteraction.Ignore))
            {
                fired = true;

                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null) dmg.ApplyDamage(Config.Damage);

                Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
            }
        }

        if (fired)
        {
            NextFireTime = Time.time + 1f / Mathf.Max(0.0001f, Config.FireRate);

            AudioService.Play(Config.ShotSound, parent: transform, position: transform.position, maxSoundDistance: Config.MaxDistanceSound);

            foreach (var muzzle in _muzzlePos)
            {
                if (!muzzle) continue;
                ParticleService.Play(Config.MuzzleParticle, muzzle, muzzle.position, muzzle.rotation);
            }
        }
    }

    public float NormalizeAngle(float euler) => Mathf.Repeat(euler + 180f, 360f) - 180f;
    public float DeltaAngle(float from, float to) => Mathf.DeltaAngle(from, to);
    public float WrapAngle(float signed) => (signed % 360f + 360f) % 360f;

    public float SignedAngleOnPlane(Vector3 vFrom, Vector3 vTo, Vector3 planeN)
    {
        vFrom = Vector3.ProjectOnPlane(vFrom, planeN).normalized;
        vTo = Vector3.ProjectOnPlane(vTo, planeN).normalized;
        if (vFrom.sqrMagnitude < 1e-6f || vTo.sqrMagnitude < 1e-6f) return 0f;
        float unsigned = Vector3.Angle(vFrom, vTo);
        float sign = Mathf.Sign(Vector3.Dot(planeN, Vector3.Cross(vFrom, vTo)));
        return unsigned * sign;
    }

    public bool IsWithinAimAngles(Transform tr)
    {
        if (tr == null) return false;

        Vector3 origin = _basePivot.position;
        Vector3 to = tr.position - origin;
        Vector3 toFlat = to; toFlat.y = 0f;
        if (toFlat.sqrMagnitude <= 1e-6f) return true;
        toFlat.Normalize();

        Vector3 baseInitialFwd = _baseInitialRotWorld * Vector3.forward;
        float desiredYaw = SignedAngleOnPlane(baseInitialFwd, toFlat, Vector3.up);
        if (Mathf.Abs(desiredYaw) > _halfHor + 1e-3f) return false;

        Vector3 headPos = _headPivot.position;
        Vector3 lookDir = tr.position - headPos;
        if (lookDir.sqrMagnitude <= 1e-6f) return true;

        Quaternion desiredHeadWorld = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        Transform parent = _headPivot.parent ? _headPivot.parent : _basePivot.parent;
        Quaternion parentWorld = parent ? parent.rotation : Quaternion.identity;
        Quaternion desiredLocal = Quaternion.Inverse(parentWorld) * desiredHeadWorld;

        float initPitch = NormalizeAngle(_headInitialRotLocal.eulerAngles.x);
        float desiredPitch = NormalizeAngle(desiredLocal.eulerAngles.x);
        float deltaFromInit = DeltaAngle(initPitch, desiredPitch);

        if (Mathf.Abs(deltaFromInit) > _halfVer + 1e-3f) return false;

        return true;
    }

    public bool HasLineOfFireTo(Transform tr)
    {
        if (tr == null) return false;
        if (_muzzlePos == null || _muzzlePos.Count == 0)
        {
            Vector3 origin = _headPivot.position;
            Vector3 dir = (tr.position - origin);
            if (dir.sqrMagnitude < 1e-6f) return true;
            float dist = Mathf.Min(dir.magnitude, Config.DetectionRadius);
            dir.Normalize();

            if (Physics.Raycast(origin, dir, out var h, dist, Config.TargetMask, QueryTriggerInteraction.Ignore))
                return h.collider.transform.IsChildOf(tr);
            return true;
        }

        foreach (var muzzle in _muzzlePos)
        {
            if (!muzzle) continue;
            Vector3 origin = muzzle.position;
            Vector3 dir = tr.position - origin;
            float dist = Mathf.Min(dir.magnitude, Config.DetectionRadius);
            if (dist < 1e-4f) return true;
            dir.Normalize();

            if (Physics.Raycast(origin, dir, out var hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(tr)) return true;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public bool CanShootTarget(ITargetable t, bool requireLoS = false)
    {
        if (t == null || !t.IsAlive) return false;
        var tr = t.TargetTransform;
        if (!tr) return false;

        if (!IsWithinAimAngles(tr)) return false;
        if (requireLoS && !HasLineOfFireTo(tr)) return false;
        return true;
    }

    public ITargetable FindBestTargetInRadius(bool preferShootable = true, bool requireLoSForPrefer = false)
    {
        var hits = Physics.OverlapSphere(_basePivot.position, Config.DetectionRadius, Config.TargetMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return null;

        ITargetable bestShootable = null;
        float bestShootableSqr = float.PositiveInfinity;

        Transform bestClosestTr = null;
        float bestClosestSqr = float.PositiveInfinity;

        Vector3 origin = _basePivot.position;
        foreach (var h in hits)
        {
            if (!h) continue;
            var t = h.GetComponentInParent<ITargetable>();
            if (t == null || !t.IsAlive) continue;
            if (t.TargetTransform && t.TargetTransform == transform) continue;

            Vector3 to = h.bounds.center - origin;
            to.y = 0f;
            float sqr = to.sqrMagnitude;

            if (preferShootable && CanShootTarget(t, requireLoSForPrefer))
            {
                if (sqr < bestShootableSqr)
                {
                    bestShootableSqr = sqr;
                    bestShootable = t;
                }
            }

            if (sqr < bestClosestSqr)
            {
                bestClosestSqr = sqr;
                bestClosestTr = t.TargetTransform;
            }
        }

        if (bestShootable != null) return bestShootable;

        if (bestClosestTr != null)
        {
            return bestClosestTr.GetComponentInParent<ITargetable>();
        }

        return null;
    }

    protected void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(_basePivot ? _basePivot.position : transform.position, Config.DetectionRadius);

        if (_headPivot)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_headPivot.position, _headPivot.position + _headPivot.forward * Config.DetectionRadius);
        }
    }

    protected void OnDisable()
    {
        SetTarget(null);
    }

}
