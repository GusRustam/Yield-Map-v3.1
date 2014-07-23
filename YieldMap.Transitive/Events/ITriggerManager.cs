namespace YieldMap.Transitive.Events {
    public interface ITriggerManager {
        ITriggerManager Next { get; }
        void Handle(IDbEventArgs args);
    }
}