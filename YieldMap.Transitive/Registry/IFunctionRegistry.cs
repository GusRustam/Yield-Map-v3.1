using System.Collections.Generic;

namespace YieldMap.Transitive.Registry {
    public interface IFunctionRegistry {
        int Clear();
        int Add(long propId, string expr);
        /// <summary>
        /// key - property Id
        /// value - grammar
        /// </summary>
        Dictionary<long, Evaluatable> Items { get; }
    }
}