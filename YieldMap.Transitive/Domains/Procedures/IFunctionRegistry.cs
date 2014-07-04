using System;
using System.Collections.Generic;
using YieldMap.Language;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IFunctionRegistry {
        void Clear();
        void Add(IEnumerable<Tuple<long, string>> items);
        Lexan.Value Eval(long propertyId, Dictionary<string, object> env);
        IEnumerable<Tuple<long, Lexan.Value>> EvaluateAll(Dictionary<string, object> env);
    }
}