using System;
using UnityEngine;

public interface ITargetable
{
    Transform TargetTransform { get; }
    bool IsAlive { get; }
    event Action<ITargetable> BecameUnavailable; 
}
