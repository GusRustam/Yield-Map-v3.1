using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public interface IVariable {
        Dictionary<string, object> Variable();
    }
}