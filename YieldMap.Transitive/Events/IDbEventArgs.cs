using System;
using System.Collections.Generic;

namespace YieldMap.Transitive.Events {
    public interface IDbEventArgs {
        Type Source { get; }
        IEnumerable<long> Added { get; }
        IEnumerable<long> Changed { get; }
        IEnumerable<long> Removed { get; }
    }
}