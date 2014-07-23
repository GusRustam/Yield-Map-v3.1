using System;
using System.Data;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class BondAdditionUnitOfWork : IBondAdditionUnitOfWork {
        public BondAdditionUnitOfWork() {
            Context = new BondAdditionContext();
        }

        public BondAdditionUnitOfWork(BondAdditionContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public event EventHandler<IDbEventArgs> Notify;

        public int Save() {
            var entityEntries = Context.ChangeTracker.Entries<Instrument>().ToList();
            
            var addedInstrumentIds = entityEntries
                .Where(x => x.State == EntityState.Added)
                .Select(x => new[] {x.Entity.id})
                .ToList();

            var changedInstrumentIds = entityEntries
                .Where(x => x.State == EntityState.Modified)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var removedInstrumentIds = entityEntries
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => new[] { x.Entity.id })
                .ToList();

            var res = Context.SaveChanges();

            if (Notify != null)
                Notify(this,
                    new DbEventArgs(
                        new EventDescription(addedInstrumentIds),
                        new EventDescription(changedInstrumentIds), 
                        new EventDescription(removedInstrumentIds),
                        EventSource.InstrumentDescription));

            return res;
        }

        public BondAdditionContext Context { get; private set; }
}
}