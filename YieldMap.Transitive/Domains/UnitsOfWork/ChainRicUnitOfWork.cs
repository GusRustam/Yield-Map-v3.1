using System;
using System.Data;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class ChainRicUnitOfWork : IChainRicUnitOfWork {
        public ChainRicUnitOfWork() {
            Context = new ChainRicContext();
        }

        public ChainRicUnitOfWork(ChainRicContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public event EventHandler<IDbEventArgs> Notify;

        public int Save() {
            var entityChains = Context.ChangeTracker.Entries<Chain>().ToList();
            var entityRics = Context.ChangeTracker.Entries<Ric>().ToList();

            var addedChains = entityChains
                .Where(x => x.State == EntityState.Added)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var changedChains = entityChains
                .Where(x => x.State == EntityState.Modified)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var removedChains = entityChains
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var addedRics = entityRics
                .Where(x => x.State == EntityState.Added)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var changedRics = entityRics
                .Where(x => x.State == EntityState.Modified)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var removedRics = entityRics
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var res = Context.SaveChanges();

            if (Notify != null) {
                Notify(this,
                    new DbEventArgs(
                        new EventDescription(addedChains),
                        new EventDescription(changedChains),
                        new EventDescription(removedChains),
                        EventSource.Chain));
                
                Notify(this,
                    new DbEventArgs(
                        new EventDescription(addedRics),
                        new EventDescription(changedRics),
                        new EventDescription(removedRics),
                        EventSource.Ric));
            }

            return res;
        }

        public ChainRicContext Context { get; private set; }
    }
}