using System;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class FeedsUnitOfWork : IEikonEntitiesUnitOfWork {
        public FeedsUnitOfWork() {
            Context = new FeedsContext();
        }

        public FeedsUnitOfWork(FeedsContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public event EventHandler<IDbEventArgs> Notify;

        public int Save() {
            var p = Context.ExtractEntityChanges<Feed>();
            var res = Context.SaveChanges();
            if (Notify != null) Notify(this, new SingleTable(p.ExtractIds(), EventSource.Feed));
            return res;

        }

        public FeedsContext Context { get; private set; }
    }
}