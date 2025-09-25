using Service;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Collider2D _col;

    private float _damage;
    private float _life;
    private Transform _owner;

    private IInstantiateFactoryService _instantiateFactory;

    public void Launch(Vector2 dir, float speed, float damage, float lifeTime, IInstantiateFactoryService instantiateFactoryService, Transform owner = null)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_col == null) _col = GetComponent<Collider2D>();

        _owner = owner;
        _damage = damage;
        _life = lifeTime;

        _instantiateFactory = instantiateFactoryService;

        _rb.linearVelocity = dir.normalized * speed;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            DestroyOrRelease();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (_owner != null && other.transform == _owner) return;


        if (other.TryGetComponent<IDamageable>(out var dmg))
        {

            dmg.TakeDamage(_damage); 
            DestroyOrRelease();
            return;
        }

         DestroyOrRelease();
    }

    private void DestroyOrRelease()
    {
        _instantiateFactory.Release(this);
    }
}
