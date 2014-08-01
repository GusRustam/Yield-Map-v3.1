using System.Collections.Generic;

namespace YieldMap.Transitive.Registry {
    /// <summary>
    /// Stores pre-compiled expressions for properties
    /// </summary>
    public interface IFunctionRegistry {
        int Clear();
        int Add(long propId, long instrTypeId, string expr);
        /// <summary>
        /// key - property Id
        /// value - grammar
        /// </summary>
        Dictionary<long, Evaluatable> Items { get; }
    }
}