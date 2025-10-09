using FSM;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyBase : MonoStateMachine<EnemyBase>, IDamageable, ITargetable
{
    [field: SerializeField] public EnemyConfigBase Config { get; private set; }
    [field: SerializeField] public Rigidbody Rigidbody { get; private set; }

    public EnemyController Controller { get; private set; }
    public IAudioService AudioService { get; private set; }
    public IParticleService ParticleService { get; private set; }

    public Transform TargetTransform => transform;
    public bool IsAlive => _alive;
    protected bool _alive;
    public event System.Action<ITargetable> BecameUnavailable;

    [field: SerializeField] public VehicleBase Target {  get; private set; }
    [field: SerializeField] public float Health;

    protected State<EnemyBase> Idle, MoveTo, Attack;

    [SerializeField] private Collider _selfCol;
    private Collider[] _selfCols;
    private HashSet<Collider> _selfSet;

    private Collider _targetCol;
    [SerializeField] private LayerMask _alliesMask;
    private static readonly Collider[] _allyBuf = new Collider[12];

    private float _slotAngleDeg;
    private Vector3 _repelSmoothed;

    public float AttackRange => Config.AttackRange + 0.1f;
    protected override void Awake()
    {
        base.Awake();
        if (!_selfCol) _selfCol = GetComponentInChildren<Collider>();
        _selfCols = GetComponentsInChildren<Collider>(includeInactive: true);
        _selfSet = new HashSet<Collider>(_selfCols);
        _slotAngleDeg = (Mathf.Abs(GetInstanceID()) * 0.6180339f % 1f) * 360f;
    }

    public void Initialize(EnemyController controller, IAudioService audio, IParticleService fx)
    {
        Controller = controller;
        AudioService = audio;
        ParticleService = fx;

        Health = Config.Health;
        _alive = true;
    }

    public void Tick() => FSM.Tick();

    protected override void BuildStates()
    {
        Idle = new EnemyIdle(this);
        MoveTo = new EnemyMoveTo(this);
        Attack = new EnemyAttack(this);

        FSM.Set(Idle);
#if UNITY_EDITOR
        FSM.Changed += (from, to) => Debug.Log($"[EnemyFSM] {name}: {from?.Name ?? "<null>"} -> {to?.Name}");
#endif
    }

    public void AssignTarget(VehicleBase t)
    {
        Target = t;
        if (Target.MainCollider) _targetCol = Target.MainCollider;
        EvaluateTarget();
    }

    public void EvaluateTarget()
    {
        if (Target == null) { FSM.Set(Idle); return; }

        float dist = Vector3.Distance(Target.transform.position, transform.position);
        FSM.Set(dist <= Config.AttackRange ? Attack : MoveTo);
    }

    public void ApplyDamage(float damage)
    {
        if (!_alive) return;
        Health -= damage;
        if (Health <= 0) Die();
    }

    protected virtual void Die()
    {
        if (!_alive) return;
        _alive = false;
        BecameUnavailable?.Invoke(this);
        Controller.Release(this); 
    }

    public float SeparationTo(Transform target, Collider targetCol = null)
    {
        if (!_selfCol || !target) return Vector3.Distance(transform.position, target.position);
        targetCol ??= _targetCol;

        Vector3 pSelf = _selfCol.ClosestPoint(target.position);
        Vector3 pTarget = targetCol ? targetCol.ClosestPoint(transform.position) : target.position;
        pSelf.y = pTarget.y = 0f;
        return Vector3.Distance(pSelf, pTarget);
    }

    public Vector3 AlliesRepulsion(float radius = 1.2f, float weight = 0.6f, float dt = 0f)
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, _allyBuf, _alliesMask, QueryTriggerInteraction.Ignore);
        Vector3 push = Vector3.zero;

        for (int i = 0; i < n; i++)
        {
            var c = _allyBuf[i];
            if (!c) continue;
            if (IsSelfCollider(c)) continue;                              // ← ВАЖНО
            if (_targetCol && (c == _targetCol || c.transform.IsChildOf(Target.transform))) continue;

            Vector3 away = transform.position - c.ClosestPoint(transform.position);
            away.y = 0f;
            float d = away.magnitude;
            if (d > 0.001f)
            {
                float k = 1f - Mathf.Clamp01(d / radius);                 // линейное затухание
                push += away.normalized * k;
            }
        }

        if (push.sqrMagnitude > 1e-6f) push = Vector3.ClampMagnitude(push, 1f) * weight;
        return SmoothRepel(push, dt);                                     // ← сглаживание
    }

    public Vector3 GetTargetSlotPosition(float extraRadius = 0.6f)
    {
        if (!Target) return transform.position;
        float r = AttackRange * 0.8f + extraRadius;
        Quaternion q = Quaternion.Euler(0f, _slotAngleDeg, 0f);
        return Target.transform.position + q * (Vector3.forward * r);
    }

    protected virtual void OnDisable()
    {
        if (_alive) { _alive = false; BecameUnavailable?.Invoke(this); }
    }

    private Vector3 SmoothRepel(Vector3 raw, float dt)
    {
        float alpha = 1f - Mathf.Exp(-8f * dt);
        _repelSmoothed = Vector3.Lerp(_repelSmoothed, raw, alpha);
        return _repelSmoothed;
    }


    private bool IsSelfCollider(Collider c) =>
    c && (_selfSet != null && _selfSet.Contains(c)) || c.transform.IsChildOf(transform);
}
