using System.Collections.Generic;
using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Events {
    public interface ISingleTable : IDbEventArgs {
        EventSource Source { get; }
        IEnumerable<long> Added { get; }
        IEnumerable<long> Changed { get; }
        IEnumerable<long> Removed { get; }        
    }
}