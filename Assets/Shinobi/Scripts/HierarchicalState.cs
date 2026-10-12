namespace Shinobi
{
    // An active parent owns one active child. Tick runs parent-first;
    // leaving a branch exits descendants before the parent.
    public abstract class HierarchicalState
    {
        public string Name { get; }
        protected HierarchicalState Child { get; private set; }
        public string ActivePath => Child == null ? Name : Name + "/" + Child.ActivePath;
        protected HierarchicalState(string name) { Name = name; }
        public void Enter() { OnEnter(); }
        public void Tick(float dt) { OnTick(dt); Child?.Tick(dt); }
        public void Exit()
        {
            Child?.Exit();
            Child = null;
            OnExit();
        }
        protected void SetChild(HierarchicalState next)
        {
            if (ReferenceEquals(Child, next)) return;
            Child?.Exit();
            Child = next;
            Child?.Enter();
        }
        protected virtual void OnEnter() { }
        protected virtual void OnTick(float dt) { }
        protected virtual void OnExit() { }
    }
}
