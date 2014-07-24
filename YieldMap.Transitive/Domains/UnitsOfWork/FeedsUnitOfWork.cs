using System;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class FeedsUnitOfWork : IEikonEntitiesUnitOfWork , INotifier {
        private bool _notifications = true;
        public event EventHandler<IDbEventArgs> Notify;
        public void DisableNotifications() {
            _notifications = false;
        }
        public void EnableNotifications() {
            _notifications = true;
        }
        
        
        public FeedsUnitOfWork() {
            Context = new FeedsContext();
        }

        public FeedsUnitOfWork(FeedsContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }


        public int Save() {
            var p = Context.ExtractEntityChanges<Feed>();
            var res = Context.SaveChanges();
            if (Notify != null && _notifications) Notify(this, new DbEventArgs(p.ExtractIds(), EventSource.Feed));
            return res;

        }

        public FeedsContext Context { get; private set; }
    }
}