using System;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

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
            var chains = Context.ExtractChanges<Chain>();
            var rics = Context.ExtractChanges<Ric>();
            
            var res = Context.SaveChanges();

            if (Notify != null) {
                Notify(this, new SingleTable(chains, EventSource.Chain));
                Notify(this, new SingleTable(rics, EventSource.Ric));
            }

            return res;
        }

        public ChainRicContext Context { get; private set; }
    }
}