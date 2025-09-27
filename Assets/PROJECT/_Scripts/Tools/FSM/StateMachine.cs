using System;

namespace FSM
{
    public class StateMachine<TOwner>
    {
        public State<TOwner> Current { get; private set; }
        public event Action<State<TOwner>, State<TOwner>> Changed;

        private readonly TOwner _owner;
        public StateMachine(TOwner owner) => _owner = owner;

        public void Set(State<TOwner> newState, bool force = false)
        {
            if (!force && newState == Current) return;
            var prev = Current;
            prev?.OnExit();
            Current = newState;
            Current?.OnEnter();
            Changed?.Invoke(prev, Current);
        }

        public void Tick() => Current?.Tick();
        public void FixedTick() => Current?.FixedTick();
        public void LateTick() => Current?.LateTick();
    }

}
