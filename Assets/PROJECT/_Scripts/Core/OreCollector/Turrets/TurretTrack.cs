using FSM;
using UnityEngine;

public class TurretTrack : State<TurretBase>
{
    public TurretTrack(TurretBase o) : base(o) { }

    private float _reacquireAt;

    public override void OnEnter()
    {
        base.OnEnter();
        _reacquireAt = 0f;
        Owner.AudioService.Play(Owner.Config.NoTargetSound, parent: Owner.transform, position: Owner.transform.position);
    }

    public override void Tick()
    {
        float dt = Time.deltaTime;

        if (!Owner.IsTargetInRadiusAlive(Owner.Target))
        {
            Owner.SetTarget(null);
            Owner.FSM.Set(new TurretIdle(Owner));
            return;
        }

        bool anglesOk = Owner.IsWithinAimAngles(Owner.Target.TargetTransform);

        if (!anglesOk && Owner.Config.ReacquireIfOutOfAngles)
        {
            if (_reacquireAt == 0f)
                _reacquireAt = Time.time + Owner.Config.ReacquireDelay;

            if (Time.time >= _reacquireAt)
            {
                var newT = Owner.FindBestTargetInRadius(preferShootable: true, requireLoSForPrefer: true);
                if (newT != null && newT != Owner.Target)
                    Owner.SetTarget(newT);

                _reacquireAt = 0f;
            }
        }
        else
        {
            _reacquireAt = 0f;
        }

        Owner.AimAtTarget(dt);

        if (anglesOk)
            Owner.TryFireShot();
    }
}
