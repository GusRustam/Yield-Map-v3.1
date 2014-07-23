namespace YieldMap.Transitive.Events {
    public abstract class TriggerManagerBase : ITriggerManager {
        protected TriggerManagerBase( ITriggerManager next) {
            Next = next;
        }

        public ITriggerManager Next { get; private set; }

        public abstract void Handle(IDbEventArgs args);
    }
}