using FSM;
using UnityEngine;

public class EnemyAttack : State<EnemyBase>
{
    private float _nextTime;

    public EnemyAttack(EnemyBase o) : base(o) { }
    public override void OnEnter()
    {
        // attack pose/anim
        _nextTime = Time.time + 0.05f;
    }

    public override void Tick()
    {
        var t = Owner.Target;
        if (!t) { Owner.FSM.Set(new EnemyIdle(Owner)); return; }

        float sep = Owner.SeparationTo(t.transform);
        if (sep > Owner.AttackRange + 0.1f)
        {
            Owner.FSM.Set(new EnemyMoveTo(Owner));
            return;
        }

        Vector3 dir = Owner.DirToClosest(t.transform);
        if (dir.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            Owner.transform.rotation = Quaternion.RotateTowards(Owner.transform.rotation, look, Owner.RotSpeedDeg);
        }

        if (Time.time >= _nextTime)
        {
            _nextTime = Time.time + 1f / Mathf.Max(0.001f, Owner.Config.AttackSpeed);

            var dmg = t.GetComponent<IDamageable>();
            if (dmg != null) dmg.ApplyDamage(Owner.Config.Damage);

            // FX/SFX Ч по желанию

        }
    }
}