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
    private float _halfHor, _halfVer;

    public IAudioService AudioService { get; private set; }
    public IParticleService ParticleService { get; private set; }

    protected State<TurretBase> Idle, Track;

    protected override void Awake()
    {
        AudioService = ServiceLocator.Get<IAudioService>();
        ParticleService = ServiceLocator.Get<IParticleService>();

        base.Awake();

        _baseInitialRotWorld = _basePivot.localRotation;
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

        if (!IsInDetectBand(tr.position)) return false;

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

        return IsInDetectBand(tr.position);
    }

    public void AimAtTarget(float dt)
    {
        if (Target == null) return;
        var tr = Target.TargetTransform;
        if (!tr) { SetTarget(null); return; }

        Vector3 headPos = _headPivot.position;
        Vector3 lookDirWorld = tr.position - headPos;
        if (lookDirWorld.sqrMagnitude <= 1e-6f) return;

        Transform parent = _headPivot.parent;
        Quaternion parentWorld = parent ? parent.rotation : Quaternion.identity;

        Quaternion desiredHeadWorld = Quaternion.LookRotation(lookDirWorld.normalized, Vector3.up);
        Quaternion desiredLocal = Quaternion.Inverse(parentWorld) * desiredHeadWorld;

        Quaternion delta = Quaternion.Inverse(_headInitialRotLocal) * desiredLocal;
        Vector3 deltaEuler = delta.eulerAngles;

        float yaw = NormalizeAngle(deltaEuler.y); 
        float pitch = NormalizeAngle(deltaEuler.x);

        yaw = Mathf.Clamp(yaw, -_halfHor, _halfHor);
        pitch = Mathf.Clamp(pitch, -_halfVer, _halfVer);

        Quaternion targetLocal = _headInitialRotLocal * Quaternion.Euler(pitch, yaw, 0f);

        _headPivot.localRotation = Quaternion.RotateTowards(
            _headPivot.localRotation,
            targetLocal,
            Config.VerticalRotationSpeed * dt 
        );

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
        _headPivot.localRotation = Quaternion.RotateTowards(_headPivot.localRotation, _headInitialRotLocal,Config.VerticalRotationSpeed * Config.ReturnSpeedMul * dt);

        if (_returning == false)
        {
            _returning = true;
            AudioService.Play(Config.NoTargetSound, parent: transform, position: transform.position);
        }
    }

    public void TryFireShot()
    {
        if (Time.time < NextFireTime) return;

        bool fired = false;

        if (Config.FireOnUnaimedTargets && (Config.FireWithoutLockedTarget || Target != null))
        {
            if (TryPreFireRay(out var hit))
            {
                var dmg = hit.collider.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    dmg.ApplyDamage(Config.Damage);
                    fired = true;
                    Debug.DrawLine(hit.point - hit.normal * 0.2f, hit.point, Color.yellow, 0.1f);

                    var t = hit.collider.GetComponent<ITargetable>();
                    if (t != null && t != Target) SetTarget(t);
                }
            }
        }

        if (!fired && Target != null)
        {
            Vector3 originHead = _headPivot.position;
            Vector3 toTarget = (Target.TargetTransform.position - originHead);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float aimErr = Vector3.Angle(_headPivot.forward, toTarget.normalized);
                if (aimErr <= Config.FireAngleTolerance)
                {
                    foreach (var muzzle in _muzzlePos)
                    {
                        if (!muzzle) continue;
                        Vector3 origin = muzzle.position;
                        Vector3 dir = muzzle.forward;

                        if (Physics.Raycast(origin, dir, out RaycastHit hit, Config.DetectionRadius, Config.TargetMask, QueryTriggerInteraction.Collide))
                        {
                            var dmg = hit.collider.GetComponent<IDamageable>();
                            if (dmg != null) dmg.ApplyDamage(Config.Damage);
                            fired = true;
                            Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
                        }
                    }
                }
            }
        }

        if (fired)
        {
            NextFireTime = Time.time + 1f / Mathf.Max(0.0001f, Config.FireRate);
            AudioService.Play(Config.ShotSound, parent: transform, position: transform.position);
            if (_muzzlePos != null)
                foreach (var muzzle in _muzzlePos)
                    if (muzzle) ParticleService.Play(Config.MuzzleParticle, muzzle, muzzle.position, muzzle.rotation);
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

        Transform parent = _headPivot.parent ? _headPivot.parent : _basePivot.parent;
        Quaternion parentWorld = parent ? parent.rotation : Quaternion.identity;

        Vector3 headPos = _headPivot.position;
        Vector3 lookDirWorld = tr.position - headPos;
        if (lookDirWorld.sqrMagnitude <= 1e-6f) return true;

        Quaternion desiredHeadWorld = Quaternion.LookRotation(lookDirWorld.normalized, Vector3.up);
        Quaternion desiredLocal = Quaternion.Inverse(parentWorld) * desiredHeadWorld;

        Quaternion delta = Quaternion.Inverse(_headInitialRotLocal) * desiredLocal;
        Vector3 deltaEuler = delta.eulerAngles;

        float yaw = NormalizeAngle(deltaEuler.y);
        float pitch = NormalizeAngle(deltaEuler.x);

        if (Mathf.Abs(yaw) > _halfHor + 1e-3f) return false;
        if (Mathf.Abs(pitch) > _halfVer + 1e-3f) return false;

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
        var hits = Physics.OverlapSphere(_basePivot.position,Config.DetectionRadius, Config.TargetMask, QueryTriggerInteraction.Ignore);
    
        if (hits == null || hits.Length == 0) return null;
   
        ITargetable bestShootable = null;
        float bestShootableSqr = float.PositiveInfinity;
        Transform bestClosestTr = null;
        float bestClosestSqr = float.PositiveInfinity;
        Vector3 origin = _basePivot.position;

        foreach (var h in hits)
        {
            if (!h) continue;

            var t = h.GetComponent<ITargetable>();
            if (t == null || !t.IsAlive) continue;

            var tr = t.TargetTransform;
            if (!tr) continue;
            if (!IsInDetectBand(tr.position)) continue;
            if (tr == transform) continue;

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
        if (bestClosestTr != null) return bestClosestTr.GetComponent<ITargetable>();
        return null;
    }

    private bool TryPreFireRay(out RaycastHit hit)
    {
        hit = default;
        if (!Config.FireOnUnaimedTargets || _muzzlePos == null || _muzzlePos.Count == 0) return false;

        for (int i = 0; i < _muzzlePos.Count; i++)
        {
            var muzzle = _muzzlePos[i];
            if (!muzzle) continue;

            Vector3 origin = muzzle.position;
            Vector3 dir = muzzle.forward;

            bool hitSmth;
            if (Config.PreFireRay > 0f)
                hitSmth = Physics.SphereCast(origin, Config.PreFireRay, dir, out hit, Config.DetectionRadius, Config.TargetMask, QueryTriggerInteraction.Collide);
            else
                hitSmth = Physics.Raycast(origin, dir, out hit, Config.DetectionRadius, Config.TargetMask, QueryTriggerInteraction.Collide); 

            if (!hitSmth) continue;

            return true;
        }
        return false;
    }

    private bool IsInDetectBand(Vector3 pos)
    {
        float sqr = (pos - _basePivot.position).sqrMagnitude;
        float minSqr = Config.MinDetectionRadius * Config.MinDetectionRadius;
        float maxSqr = Config.DetectionRadius * Config.DetectionRadius;
        return sqr >= minSqr && sqr <= maxSqr;
    }

    protected void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        var pivotPos = _basePivot ? _basePivot.position : transform.position;
        Gizmos.DrawWireSphere(pivotPos, Config.DetectionRadius);

        if (Config.MinDetectionRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(pivotPos, Config.MinDetectionRadius); 
        }

        if (_muzzlePos == null) return;
        Gizmos.color = Color.yellow;
        float maxDist = (Config.DetectionRadius > 0f) ? Config.DetectionRadius : (Config ? Config.DetectionRadius : 10f);
        foreach (var m in _muzzlePos)
        {
            if (!m) continue;
            if (Config.PreFireRay > 0f)
                Gizmos.DrawWireSphere(m.position + m.forward * Mathf.Min(0.2f, maxDist), Config.PreFireRay);
            Gizmos.DrawLine(m.position, m.position + m.forward * maxDist);
        }
    }

    protected void OnDisable()
    {
        SetTarget(null);
    }
}
