using System.Collections.Generic;

namespace YieldMap.Transitive.Tools {
    public static class SetHelper {
        public static Set<T> ToSet<T>(this IEnumerable<T> items) {
            return new Set<T>(items);
        }
    }
}