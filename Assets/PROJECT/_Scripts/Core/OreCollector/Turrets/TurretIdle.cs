using FSM;
using UnityEngine;

public class TurretIdle : State<TurretBase>
{
    public TurretIdle(TurretBase o) : base(o) { }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void Tick()
    {
        float dt = Time.deltaTime;

        if (Owner.Config.ReturnToRest)
            Owner.ReturnToRest(dt);

        bool found = Owner.ScanTick();

        if (found && Owner.IsTargetInRadiusAlive(Owner.Target))
            Owner.FSM.Set(new TurretTrack(Owner));
    }
}