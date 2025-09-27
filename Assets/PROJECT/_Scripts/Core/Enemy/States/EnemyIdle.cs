using FSM;

public class EnemyIdle : State<EnemyBase>
{
    public EnemyIdle(EnemyBase o) : base(o) { }
    public override void OnEnter() { /* play idle anim */ }
    public override void Tick()
    {
        if (Owner.Target == null)
        {
            Owner.Controller.RefreshTargets();
            Owner.AssignTarget(Owner.Controller.GetNearestVehicle(Owner.transform.position));
        }
        Owner.EvaluateTarget();
    }
}