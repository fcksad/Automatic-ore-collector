using UnityEngine;

public class EnemyBase : MonoBehaviour, IDamageable, ITargetable
{
    [SerializeField] private EnemyConfigBase _config;

    public Transform TargetTransform => transform;
    public bool IsAlive => _alive;
    public event System.Action<ITargetable> BecameUnavailable;

    private bool _alive;

    private EnemyController _controller;
    private IAudioService _audio;
    private IParticleService _particles;

    private VehicleBase _target;
    private float _health;

    public void Initialize(EnemyController controller, IAudioService audio, IParticleService particles)
    {
        _controller = controller;
        _audio = audio;
        _particles = particles;

        _health = _config ? _config.Health : 10f;
        _alive = true;
    }

    public void AssignTarget(VehicleBase target) => _target = target;

    public void OnDespawn()
    {
        _target = null;
        // сбросить эффекты/анимации/таймеры
    }


    public void Tick()
    {
        if (!_target)
        {
            _controller.RefreshTargets();
            _target = _controller.GetNearestVehicle(transform.position);
            if (!_target) return;
        }

        Vector3 to = _target.transform.position - transform.position;
        to.y = 0;
        if (to.sqrMagnitude > 0.01f)
        {
            float rotSpd = _config ? _config.RotationSpeed : 1f;
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotSpd * 60f * Time.deltaTime);

            float spd = _config ? _config.Speed : 5f;
            transform.position += transform.forward * (spd * Time.deltaTime);
        }

        float dmg = _config ? _config.Damage : 1f;
        float atkRange = 1.6f;
        if (to.magnitude <= atkRange)
        {
            // здесь твой удар/эффект/звук
            // _audio.Play(...); _particles.Play(...);
        }
    }

    public void ApplyDamage(float damage)
    {
        if (!_alive) return;
        _health -= damage;
        if (_health <= 0f) Die();
    }

    private void Die()
    {
        if (!_alive) return;
        _alive = false;
        BecameUnavailable?.Invoke(this);        
        // эффекты/звук...
        _controller.Release(this);
    }

    private void OnDisable()
    {
        if (_alive) { _alive = false; BecameUnavailable?.Invoke(this); }
    }
}
