using System.Collections.Generic;

namespace YieldMap.Transitive.Native.Variables {
    public interface IVariableHelper {
        Dictionary<string, object> ToVariable<T>(T obj);
    }
}