using System;
using System.Collections.Generic;

namespace YieldMap.Transitive.Registry {
    public interface IFunctionRegistry {
        void Clear();
        void Add(long propId, string expr);
        Dictionary<long, Evaluatable> Items { get; }
    }
}