using System.Collections.Generic;

namespace YieldMap.Transitive.Procedures {
    public interface IEventDescription {
        IEnumerable<long[]> Ids { get; }
    }
}