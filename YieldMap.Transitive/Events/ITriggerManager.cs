namespace YieldMap.Transitive.Events {
    public interface ITriggerManager {
        ITriggerManager Next { get; }
        void Handle(object source, IDbEventArgs args);
    }
}