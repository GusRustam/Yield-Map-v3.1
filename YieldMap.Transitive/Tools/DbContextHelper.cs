using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace YieldMap.Transitive.Tools {
    public static class DbContextHelper {
        public static IReadOnlyDictionary<EntityAction, IEnumerable<T>> ExtractEntityChanges<T>(this DbContext context) where T : class {
            var entityChains = context.ChangeTracker.Entries<T>().ToList();

            var addedChains = entityChains
                .Where(x => x.State == EntityState.Added)
                .Select(x => x.Entity)
                .ToList();

            var changedChains = entityChains
                .Where(x => x.State == EntityState.Modified)
                .Select(x => x.Entity)
                .ToList();

            var removedChains = entityChains
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => x.Entity)
                .ToList();

            var res = new Dictionary<EntityAction, IEnumerable<T>> {
                {EntityAction.Added, addedChains},
                {EntityAction.Updated, changedChains},
                {EntityAction.Removed, removedChains}
            };
            return res;
        }

        private static IEnumerable<long> ExtractIds<T>(this IEnumerable<T> items) {
            return items.Select(c => (long)((dynamic)c).id);
        }

        public static IReadOnlyDictionary<EntityAction, IEnumerable<long>> ExtractIds<T>(
            this IReadOnlyDictionary<EntityAction, IEnumerable<T>> entities) {
            return 
                entities
                    .Select(t => new {t.Key, Value = t.Value.ExtractIds()})
                    .ToDictionary(t => t.Key, t => t.Value);
        }
    }
}