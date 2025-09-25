using Service;
using UnityEngine;

public class TurretBase : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private TurretConfig _turretConfig;
    [SerializeField] private Transform _pivot;
    [SerializeField] private Transform[] _muzzles;
    [SerializeField] private float _scanInterval = 0.12f;


    [SerializeField] private IInstantiateFactoryService _factory;

    private float _shootCooldown;
    private float _scanTimer;
    private Transform _currentTarget;

    private readonly Collider2D[] _scanBuffer = new Collider2D[64];

    private void Awake()
    {
        if (_pivot == null) _pivot = transform;
        _factory ??= ServiceLocator.Get<IInstantiateFactoryService>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1) Переодический поиск целей
        _scanTimer -= dt;
        if (_scanTimer <= 0f)
        {
            _scanTimer = _scanInterval;
            _currentTarget = FindBestTarget();
        }

        // 2) Наведение с ограничением дуги, где база считается от корпуса // CHANGED
        float baseYaw = GetBaseYawFromParent(); // центр полудуги
        float desiredWorldYaw = baseYaw;

        if (_currentTarget != null && IsTargetValid(_currentTarget))
        {
            Vector2 toTarget = _currentTarget.position - _pivot.position;
            // CHANGED: -90f (up-система) и минус офсет спрайта
            desiredWorldYaw = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f - _turretConfig.SpriteForwardOffsetDeg;
        }
        else
        {
            desiredWorldYaw = baseYaw;
        }

        float clampedWorldYaw = ClampWorldYawToLimits(baseYaw, desiredWorldYaw); // CHANGED
        float newYaw = Mathf.MoveTowardsAngle(_pivot.eulerAngles.z, clampedWorldYaw, _turretConfig.RotationSpeed * dt);
        _pivot.rotation = Quaternion.Euler(0f, 0f, newYaw);

        // 3) Стрельба при сведении
        _shootCooldown -= dt;
        if (_currentTarget != null && _shootCooldown <= 0f)
        {
            float aimError = Mathf.Abs(Mathf.DeltaAngle(newYaw, desiredWorldYaw));
            if (aimError <= _turretConfig.FireAngleTolerance && IsTargetWithinArc(_currentTarget.position)) // IsTargetWithinArc уже использует новую базу
            {
                Fire();
                _shootCooldown = (_turretConfig.FireRate > 0f) ? (1f / _turretConfig.FireRate) : 0f;
            }
        }
    }

    private float GetBaseYawFromParent()
    {
        float parentYaw = _pivot.parent ? _pivot.parent.eulerAngles.z : 0f;
        float anchorOffset = _turretConfig.Anchor switch
        {
            ArcAnchor.Front => 0f,
            ArcAnchor.Back => 180f,
            ArcAnchor.Left => -90f,
            ArcAnchor.Right => 90f,
            ArcAnchor.Custom => _turretConfig.CustomAnchorOffsetDeg,
            _ => 0f
        };
        return parentYaw + anchorOffset - _turretConfig.SpriteForwardOffsetDeg;
    }

    private Transform FindBestTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(_pivot.position, _turretConfig.DetectionRadius, _scanBuffer, _turretConfig.DamageableMask);

        float bestDistSq = float.PositiveInfinity;
        Transform best = null;

        for (int i = 0; i < count; i++)
        {
            var col = _scanBuffer[i];
            if (col == null) continue;
            if (!col.TryGetComponent(out IDamageable dmg)) continue;

            Vector3 pos = col.transform.position;
            if (!IsTargetWithinArc(pos)) continue; // фильтр по полудуге

            float d2 = (pos - _pivot.position).sqrMagnitude;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                best = col.transform;
            }
        }
        return best;
    }

    private bool IsTargetValid(Transform t)
    {
        if (t == null) return false;
        if (((1 << t.gameObject.layer) & _turretConfig.DamageableMask.value) == 0) return false;
        if (!t.TryGetComponent<IDamageable>(out _)) return false;
        float dist = Vector2.Distance(_pivot.position, t.position);
        if (dist > _turretConfig.DetectionRadius) return false;
        return IsTargetWithinArc(t.position); // CHANGED: сразу учитываем полудугу
    }

    // полудуга вокруг GetBaseYawFromParent // CHANGED
    private bool IsTargetWithinArc(Vector3 worldPos)
    {
        float baseYaw = GetBaseYawFromParent();
        Vector2 dir = worldPos - _pivot.position;
        // CHANGED: - офсет
        float worldYaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f - _turretConfig.SpriteForwardOffsetDeg;
        float rel = Mathf.DeltaAngle(baseYaw, worldYaw);
        return rel >= _turretConfig.YawMinDeg && rel <= _turretConfig.YawMaxDeg;
    }

    // clamp к дуге, центрированной на базе (родительский курс + якорь) // CHANGED
    private float ClampWorldYawToLimits(float baseYaw, float desiredWorldYaw)
    {
        float rel = Mathf.DeltaAngle(baseYaw, desiredWorldYaw);
        float clampedRel = Mathf.Clamp(rel, _turretConfig.YawMinDeg, _turretConfig.YawMaxDeg);
        return baseYaw + clampedRel;
    }

    private void Fire()
    {
        if (_turretConfig.ProjectilePrefab == null) return;

        // общий «боевой вперёд» с офсетом
        Vector2 forward = Quaternion.Euler(0, 0, _turretConfig.SpriteForwardOffsetDeg) * _pivot.up;

        if (_muzzles == null || _muzzles.Length == 0)
        {
            SpawnProjectile(_pivot.position, forward);
            return;
        }

        for (int i = 0; i < _muzzles.Length; i++)
        {
            var mz = _muzzles[i];
            if (mz == null) continue;

            float spread = (_turretConfig.SpreadDeg > 0f)
                ? Random.Range(-_turretConfig.SpreadDeg, _turretConfig.SpreadDeg)
                : 0f;

            // CHANGED: спред добавляем к «боевому вперёд»
            Vector2 dir = Quaternion.Euler(0, 0, spread) * forward;
            SpawnProjectile(mz.position, dir);
        }
    }

    private void SpawnProjectile(Vector3 pos, Vector2 dir)
    {
        Projectile proj;
        if (_factory != null)
            proj = _factory.Create(_turretConfig.ProjectilePrefab, position: pos, rotation: Quaternion.LookRotation(Vector3.forward, dir));
        else
            proj = Instantiate(_turretConfig.ProjectilePrefab, pos, Quaternion.LookRotation(Vector3.forward, dir));

        proj.Launch(
            dir: dir.normalized,
            speed: _turretConfig.ProjectileSpeed,
            damage: _turretConfig.Damage,
            lifeTime: 5f,
            _factory,
            owner: this.transform
        );
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_turretConfig == null) return;

        var pivot = _pivot ? _pivot : transform;

        float designBase =
            (pivot.parent ? pivot.parent.eulerAngles.z : 0f) +
            (_turretConfig.Anchor == ArcAnchor.Custom ? _turretConfig.CustomAnchorOffsetDeg :
             _turretConfig.Anchor == ArcAnchor.Right ? 90f :
             _turretConfig.Anchor == ArcAnchor.Left ? -90f :
             _turretConfig.Anchor == ArcAnchor.Back ? 180f : 0f)
            // CHANGED:
            - _turretConfig.SpriteForwardOffsetDeg;

        UnityEditor.Handles.color = new Color(0f, 1f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(pivot.position, Vector3.forward, _turretConfig.DetectionRadius);

        UnityEditor.Handles.color = new Color(1f, 0.7f, 0f, 0.9f);
        float start = designBase + _turretConfig.YawMinDeg;
        float sweep = _turretConfig.YawMaxDeg - _turretConfig.YawMinDeg;
        UnityEditor.Handles.DrawWireArc(pivot.position, Vector3.forward,
            Quaternion.Euler(0, 0, start) * Vector3.up, sweep, _turretConfig.DetectionRadius);
    }
#endif
}
