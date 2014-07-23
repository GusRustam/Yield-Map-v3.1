using System.Collections.Generic;

namespace YieldMap.Transitive.Events {
    public interface IManyTables : IDbEventArgs {
        IEnumerable<ISingleTable> Tables { get; }
    }
}