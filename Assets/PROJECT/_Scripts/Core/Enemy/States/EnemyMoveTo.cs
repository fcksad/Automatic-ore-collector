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

        var rb = Owner.Rigidbody ? Owner.Rigidbody : Owner.GetComponent<Rigidbody>();
        if (!rb) return;

        if (dir.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            float angStep = Owner.Config.RotationSpeed * dt; 
            if (rb.isKinematic)
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, look, angStep));
            else
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, look, angStep));
        }

        const float brakeEps = 0.05f;
        float desiredSep = Owner.AttackRange + brakeEps;
        const float slowRadius = 0.8f;

        float speedK = Mathf.Clamp01((sep - desiredSep) / slowRadius);
        float targetSpeed = Owner.Config.MoveSpeed * speedK;

        Vector3 to = t.transform.position - Owner.transform.position; to.y = 0f;
        float angle = Vector3.Angle(Owner.transform.forward, to);
        if (angle < 12f && sep <= desiredSep) targetSpeed = 0f;

        Vector3 moveDir = (dir.sqrMagnitude > 1e-6f) ? dir : Owner.transform.forward;
        Vector3 desiredVel = moveDir.normalized * targetSpeed;

        Vector3 v = rb.linearVelocity;
        Vector3 vHor = new Vector3(v.x, 0f, v.z);
        Vector3 dv = desiredVel - vHor;

        float maxAccel = (Owner.Config ? Mathf.Max(10f, Owner.Config.MoveSpeed * 6f) : 30f);
        float maxDv = maxAccel * dt;
        if (dv.magnitude > maxDv) dv = dv.normalized * maxDv;

        if (rb.isKinematic)
        {
            Vector3 step = new Vector3(dv.x, 0f, dv.z) * dt; 
            rb.MovePosition(rb.position + step);
        }
        else
        {
            rb.AddForce(new Vector3(dv.x, 0f, dv.z), ForceMode.VelocityChange);
        }

        if (sep <= Owner.AttackRange + attackHysteresis)
            Owner.FSM.Set(new EnemyAttack(Owner));
    }
}
