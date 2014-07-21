using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    static class VariableExtensions {
        public static void VariableJoin(this Dictionary<string, object> lead,
            Dictionary<string, object> follower, string prefix = "") {
            foreach (var kvp in follower)
                lead.Add((string.IsNullOrWhiteSpace(prefix) ? "" : prefix) + kvp.Key, kvp.Value);
        }
    }
}