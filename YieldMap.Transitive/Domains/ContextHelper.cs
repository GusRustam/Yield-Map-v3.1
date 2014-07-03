using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains {
    public static class ContextHelper {
        /// Only for short-lived contexts (where no implicit changes in EntityState)
        public static void ApplyStateChanges(this DbContext context) {
            foreach (var entry in context.ChangeTracker.Entries<IObjectWithState>()) {
                var stateInfo = entry.Entity;
                entry.State = StateHelper.ToEntityState(stateInfo.State);
            }
        }
    }
}