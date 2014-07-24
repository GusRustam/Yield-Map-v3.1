using System;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class ChainRicUnitOfWork : IChainRicUnitOfWork, INotifier {
        private bool _notifications = true;
        public event EventHandler<IDbEventArgs> Notify;
        public void DisableNotifications() {
            _notifications = false;
        }

        public void EnableNotifications() {
            _notifications = true;
        }

        public ChainRicUnitOfWork() {
            Context = new ChainRicContext();
        }

        public ChainRicUnitOfWork(ChainRicContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }



        public int Save() {
            var chains = Context.ExtractEntityChanges<Chain>();
            var rics = Context.ExtractEntityChanges<Ric>();
            
            var res = Context.SaveChanges();

            if (Notify != null && _notifications) {
                Notify(this, new DbEventArgs(chains.ExtractIds(), EventSource.Chain));
                Notify(this, new DbEventArgs(rics.ExtractIds(), EventSource.Ric));
            }

            return res;
        }

        public ChainRicContext Context { get; private set; }
    }
}