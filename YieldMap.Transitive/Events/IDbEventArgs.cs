using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Events {
    /// <summary>
    /// Exposes three 
    /// </summary>
    public interface IDbEventArgs {
        EventSource Source { get; }
        IEventDescription Added { get; }
        IEventDescription Changed { get; }
        IEventDescription Removed { get; }
    }
}