using System.Collections.Generic;

namespace YieldMap.Transitive.Registry {
    public interface IFunctionRegistry {
        int Clear();
        int Add(long propId, string expr);
        Dictionary<long, Evaluatable> Items { get; }
    }
}