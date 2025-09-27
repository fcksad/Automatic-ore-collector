using FSM;
using UnityEngine;

public class TurretTrack : State<TurretBase>
{
    public TurretTrack(TurretBase o) : base(o) { }

    public override void OnEnter()
    {
        base.OnEnter();
        Owner.AudioService.Play(Owner.Config.NoTargetSound, parent: Owner.transform, position: Owner.transform.position, maxSoundDistance: Owner.Config.MaxDistanceSound);
    }

    public override void Tick()
    {
        float dt = Time.deltaTime;

        if (Owner.Target == null)
        {
            Owner.FSM.Set(new TurretIdle(Owner));
            return;
        }

        if (!Owner.IsWithinAimAngles(Owner.Target.TargetTransform))
        {
            var newT = Owner.FindBestTargetInRadius(preferShootable: true, requireLoSForPrefer: false);
            if (newT != null && newT != Owner.Target)
            {
                Owner.SetTarget(newT);
                return;
            }

            Owner.SetTarget(null);
            Owner.FSM.Set(new TurretIdle(Owner));
            return;
        }

        Owner.AimAtTarget(dt);
        Owner.TryFireShot();
    }
}
