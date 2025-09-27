using FSM;
using UnityEngine;

public class EnemyMoveTo : State<EnemyBase>
{
    public EnemyMoveTo(EnemyBase o) : base(o) { }
    public override void OnEnter() { /* run anim */ }
    public override void Tick()
    {
        var t = Owner.Target;
        if (!t) { Owner.FSM.Set(new EnemyIdle(Owner)); return; }

        float dt = Time.deltaTime;

        const float attackHysteresis = 0.05f;
        float sep = Owner.SeparationTo(t.transform);
        if (sep <= Owner.AttackRange - attackHysteresis)
        {
            Owner.FSM.Set(new EnemyAttack(Owner));
            return;
        }

        Vector3 slotPos = Owner.GetTargetSlotPosition();
        Vector3 dirTo = slotPos - Owner.transform.position; dirTo.y = 0f;
        Vector3 repel = Owner.AlliesRepulsion(1.2f, 0.6f);
        Vector3 dir = (dirTo.normalized + repel).normalized;

        if (dir.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Owner.transform.rotation = Quaternion.RotateTowards(
                Owner.transform.rotation, look, Owner.RotSpeedDeg);
        }

        if (Owner.FrontBlocked(out _))
        {
            Vector3 steerDir = Vector3.zero;
            bool stillBlocked = Owner.HandleObstacleBlocked(ref steerDir);

            if (!stillBlocked)
            {
                float spdTurn = (Owner.Config ? Owner.Config.SkirtTurnSpeed : 180f) * dt;
                Quaternion look = Quaternion.LookRotation(steerDir, Vector3.up);
                Owner.transform.rotation = Quaternion.RotateTowards(Owner.transform.rotation, look, spdTurn);

                Owner.transform.position += steerDir.normalized * (Owner.MoveSpeed * dt);
                return;
            }
            else
            {
                Owner.MaybeFlipObstaclePolicy();
            }
            return;
        }

        {
            float step = Owner.MoveSpeed * dt;

            const float brakeEps = 0.05f;
            float desiredSep = Owner.AttackRange + brakeEps;

            if (sep - step < desiredSep)
                step = Mathf.Max(0f, sep - desiredSep);

            Vector3 to = t.transform.position - Owner.transform.position; to.y = 0f;
            float angle = Vector3.Angle(Owner.transform.forward, to);
            if (angle < 12f && sep <= desiredSep) step = 0f;

            Vector3 moveDir = (dir.sqrMagnitude > 1e-6f) ? dir : Owner.transform.forward;
            if (step > 0f)
                Owner.transform.position += moveDir.normalized * step;
        }

        if (sep <= Owner.AttackRange + attackHysteresis)
            Owner.FSM.Set(new EnemyAttack(Owner));
    }
}
