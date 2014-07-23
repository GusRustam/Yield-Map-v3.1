using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace YieldMap.Transitive.Tools {
    public static class DbContextHelper {
        public static IReadOnlyDictionary<EntityAction, IEnumerable<long>> ExtractChanges<T>(this DbContext context) where T : class {
            var entityChains = context.ChangeTracker.Entries<T>().ToList();

            var addedChains = entityChains
                .Where(x => x.State == EntityState.Added)
                .Select(x => (long)((dynamic)x.Entity).id )
                .ToList();

            var changedChains = entityChains
                .Where(x => x.State == EntityState.Modified)
                .Select(x => (long)((dynamic)x.Entity).id )
                .ToList();

            var removedChains = entityChains
                .Where(x => x.State == EntityState.Deleted)
                .Select(x =>  (long)((dynamic)x.Entity).id )
                .ToList();


            var res = new Dictionary<EntityAction, IEnumerable<long>> {
                {EntityAction.Added, addedChains},
                {EntityAction.Updated, changedChains},
                {EntityAction.Removed, removedChains}
            };
            return res;
        }
    }
}