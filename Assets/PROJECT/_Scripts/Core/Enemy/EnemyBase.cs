using FSM;
using UnityEditor;
using UnityEngine;

public class EnemyBase : MonoStateMachine<EnemyBase>, IDamageable, ITargetable
{
    [field: SerializeField] public EnemyConfigBase Config { get; private set; }

    public EnemyController Controller { get; private set; }
    public IAudioService AudioService { get; private set; }
    public IParticleService ParticleService { get; private set; }

    public Transform TargetTransform => transform;
    public bool IsAlive => _alive;
    public event System.Action<ITargetable> BecameUnavailable;

    [field: SerializeField] public VehicleBase Target {  get; private set; }
    [field: SerializeField] public float Health;
    protected bool _alive;

    protected State<EnemyBase> Idle, MoveTo, Attack;

    [SerializeField] private Collider _selfCol;
    private Collider[] _selfCols;
    private Collider _targetCol;
    [SerializeField] private LayerMask _alliesMask;
    private static readonly Collider[] _allyBuf = new Collider[12];
    private readonly RaycastHit[] _hitsBuf = new RaycastHit[6];
    public enum ObstaclePolicy { Hold, SkirtLeft, SkirtRight }
    [SerializeField] private ObstaclePolicy _obstaclePolicy;
    private float _skirtSince; 
    private float _lastBlockTime;
    private float _slotAngleDeg;


    public float AttackRange => Config.AttackRange + 0.1f;
    public float MoveSpeed => Config.Speed;
    public float RotSpeedDeg => Config.RotationSpeed * Time.deltaTime;

    protected override void Awake()
    {
        base.Awake();
        if (!_selfCol) _selfCol = GetComponentInChildren<Collider>();
        _selfCols = GetComponentsInChildren<Collider>(includeInactive: true);

        _slotAngleDeg = (Mathf.Abs(GetInstanceID()) * 0.6180339f % 1f) * 360f;
    }

    public void Initialize(EnemyController controller, IAudioService audio, IParticleService fx)
    {
        Controller = controller;
        AudioService = audio;
        ParticleService = fx;

        Health = Config.Health;
        _alive = true;

        float r = Random.value;
        if (r < 0.33f) _obstaclePolicy = ObstaclePolicy.Hold;
        else if (r < 0.66f) _obstaclePolicy = ObstaclePolicy.SkirtLeft;
        else _obstaclePolicy = ObstaclePolicy.SkirtRight;

        _skirtSince = 0f;
        _lastBlockTime = -999f;
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

    protected virtual void OnDisable()
    {
        if (_alive) { _alive = false; BecameUnavailable?.Invoke(this); }
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

    public Vector3 DirToClosest(Transform target, Collider targetCol = null)
    {
        if (!_selfCol || !target) return (target.position - transform.position).normalized;
        targetCol ??= _targetCol;

        Vector3 pSelf = _selfCol.ClosestPoint(target.position);
        Vector3 pTarget = targetCol ? targetCol.ClosestPoint(transform.position) : target.position;
        Vector3 dir = pTarget - pSelf; dir.y = 0f;
        return dir.sqrMagnitude > 1e-6f ? dir.normalized : transform.forward;
    }

    private static readonly RaycastHit[] _oneHit = new RaycastHit[1];

    public bool FrontBlocked(out RaycastHit hit)
    {
        hit = default;
        if (Config == null) return false;

        var b = _selfCol ? _selfCol.bounds : new Bounds(transform.position, Vector3.one * 0.5f);
        var rot = transform.rotation;
        var half = Config.BumperHalfExtents;
        float len = Mathf.Max(0.01f, Config.BumperLength);

        float pushForward = b.extents.z + half.z + 0.02f;
        Vector3 start = b.center + transform.forward * pushForward;

        int n = Physics.BoxCastNonAlloc(start, half, transform.forward, _hitsBuf, rot, len,
                                        Config.ObstacleMask, QueryTriggerInteraction.Ignore);
        if (n <= 0) return false;

        for (int i = 0; i < n; i++)
        {
            var h = _hitsBuf[i];
            var c = h.collider; if (!c) continue;

            bool isSelf = c.transform.IsChildOf(transform);
            bool isTarget = _targetCol && c.transform.IsChildOf(Target.transform);
            if (isSelf || isTarget || c.isTrigger) continue;

            hit = h;
            return true;
        }
        return false;
    }

    public bool HandleObstacleBlocked(ref Vector3 outMoveDir)
    {
        if (Config == null) return true;

        if (Time.time - _lastBlockTime > 0.2f) _skirtSince = 0f; 
        _lastBlockTime = Time.time;
        _skirtSince += Time.deltaTime;

        switch (_obstaclePolicy)
        {
            case ObstaclePolicy.Hold:

                return true; 

            case ObstaclePolicy.SkirtLeft:
            case ObstaclePolicy.SkirtRight:
                {
                    Vector3 side = (_obstaclePolicy == ObstaclePolicy.SkirtLeft) ? -transform.right : transform.right;
                    Vector3 steer = (transform.forward * Config.SkirtFwdWeight + side * Config.SkirtSideWeight).normalized;

                    if (!ProbeFree(steer, Config.SkirtProbeLen))
                    {
                        return true;
                    }

                    outMoveDir = steer;
                    return false; 
                }
        }

        return true;
    }

    private static readonly RaycastHit[] _probeHit = new RaycastHit[1];
    private bool ProbeFree(Vector3 dir, float length)
    {
        Vector3 pos = transform.position + Vector3.up * 0.1f;
        int n = Physics.BoxCastNonAlloc(
            pos, Config.BumperHalfExtents, dir.normalized, _probeHit, transform.rotation,
            length, Config.ObstacleMask, QueryTriggerInteraction.Ignore);
        return n == 0;
    }

    public void MaybeFlipObstaclePolicy()
    {
        if (_skirtSince < (Config ? Config.SkirtMaxTime : 2f)) return;

        if (_obstaclePolicy == ObstaclePolicy.SkirtLeft) _obstaclePolicy = ObstaclePolicy.SkirtRight;
        else if (_obstaclePolicy == ObstaclePolicy.SkirtRight) _obstaclePolicy = ObstaclePolicy.SkirtLeft;
        else _obstaclePolicy = (Random.value < 0.5f) ? ObstaclePolicy.SkirtLeft : ObstaclePolicy.SkirtRight;

        _skirtSince = 0f;
    }

    public Vector3 AlliesRepulsion(float radius = 1.2f, float weight = 0.6f)
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, _allyBuf, _alliesMask, QueryTriggerInteraction.Ignore);
        Vector3 push = Vector3.zero;

        for (int i = 0; i < n; i++)
        {
            var c = _allyBuf[i];
            if (!c || c == _selfCol) continue;
            if (_targetCol && (c == _targetCol || c.transform.IsChildOf(Target.transform))) continue;

            Vector3 away = transform.position - c.ClosestPoint(transform.position);
            away.y = 0f;
            float d = away.magnitude;
            if (d > 0.001f)
            {
                float k = 1f - Mathf.Clamp01(d / radius); 
                push += away.normalized * k;
            }
        }

        if (push.sqrMagnitude > 1e-6f) push = push.normalized * weight;
        return push;
    }

    public Vector3 GetTargetSlotPosition(float extraRadius = 0.6f)
    {
        if (!Target) return transform.position;
        float r = AttackRange * 0.8f + extraRadius;
        Quaternion q = Quaternion.Euler(0f, _slotAngleDeg, 0f);
        return Target.transform.position + q * (Vector3.forward * r);
    }

    private void OnDrawGizmosSelected()
    {
        if (Config == null) return;

        var b = _selfCol ? _selfCol.bounds : new Bounds(transform.position, Vector3.one * 0.5f);
        Vector3 pos = b.center + Vector3.up * 0.02f;   
        Quaternion rot = transform.rotation;
        Vector3 half = Config.BumperHalfExtents;
        float len = Mathf.Max(0.01f, Config.BumperLength);

        DrawWireBox(pos, rot, half, new Color(0f, 1f, 0f, 0.6f));

        Vector3 endPos = pos + transform.forward * len;
        DrawWireBox(endPos, rot, half, new Color(1f, 1f, 0f, 0.6f));

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
        DrawSweptWire(pos, endPos, rot, half);

#if UNITY_EDITOR
        Handles.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        Vector3 sideL = (-transform.right * Config.SkirtSideWeight + transform.forward * Config.SkirtFwdWeight).normalized;
        Vector3 sideR = (transform.right * Config.SkirtSideWeight + transform.forward * Config.SkirtFwdWeight).normalized;
        Handles.ArrowHandleCap(0, pos, Quaternion.LookRotation(sideL), 0.8f, EventType.Repaint);
        Handles.ArrowHandleCap(0, pos, Quaternion.LookRotation(sideR), 0.8f, EventType.Repaint);
#endif

        var hits = new RaycastHit[4];
        int n = Physics.BoxCastNonAlloc(pos, half, transform.forward, hits, rot, len, Config.ObstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (!h.collider) continue;

            bool isSelf = _selfCol && h.collider.transform.IsChildOf(transform);
            bool isTarget = _targetCol && h.collider.transform.IsChildOf(Target ? Target.transform : null);
            if (isSelf || isTarget) continue;

            Vector3 hitCenter = h.point;
            DrawWireBox(hitCenter, rot, half * 0.6f, new Color(1f, 0f, 0f, 0.9f));

#if UNITY_EDITOR
            Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Handles.ArrowHandleCap(0, hitCenter, Quaternion.LookRotation(h.normal), 0.6f, EventType.Repaint);
#endif
            break; 
        }
    }

    private static void DrawWireBox(Vector3 center, Quaternion rotation, Vector3 halfExtents, Color color)
    {
        Gizmos.color = color;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        Gizmos.matrix = old;
    }

    private static void DrawSweptWire(Vector3 start, Vector3 end, Quaternion rot, Vector3 half)
    {
        Vector3[] corners =
        {
        new(+half.x, +half.y, +half.z), new(-half.x, +half.y, +half.z),
        new(-half.x, -half.y, +half.z), new(+half.x, -half.y, +half.z),
        new(+half.x, +half.y, -half.z), new(-half.x, +half.y, -half.z),
        new(-half.x, -half.y, -half.z), new(+half.x, -half.y, -half.z),
    };

        Matrix4x4 mStart = Matrix4x4.TRS(start, rot, Vector3.one);
        Matrix4x4 mEnd = Matrix4x4.TRS(end, rot, Vector3.one);

        void L(int i)
        {
            Vector3 a = mStart.MultiplyPoint3x4(corners[i]);
            Vector3 b = mEnd.MultiplyPoint3x4(corners[i]);
            Gizmos.DrawLine(a, b);
        }

        for (int i = 0; i < corners.Length; i++) L(i);
    }

}
