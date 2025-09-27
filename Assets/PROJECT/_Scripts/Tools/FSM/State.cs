namespace FSM
{
    public abstract class State<TOwner>
    {
        protected readonly TOwner Owner;
        protected State(TOwner owner) => Owner = owner;

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
        public virtual void LateTick() { }
        public virtual string Name => GetType().Name;
    }
}
