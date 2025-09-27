using UnityEngine;

namespace FSM
{
    public abstract class MonoStateMachine<TOwner> : MonoBehaviour where TOwner : MonoStateMachine<TOwner>
    {
        public StateMachine<TOwner> FSM { get; private set; }

        protected virtual void Awake()
        {
            FSM = new StateMachine<TOwner>((TOwner)this);
            BuildStates();       
        }

        protected abstract void BuildStates();

        protected virtual void Update() => FSM.Tick();
        protected virtual void FixedUpdate() => FSM.FixedTick();
        protected virtual void LateUpdate() => FSM.LateTick();
    }
}