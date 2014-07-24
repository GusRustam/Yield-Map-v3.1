using System;
using System.Data;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class InstrumentUnitOfWork : IInstrumentUnitOfWork, INotifier {
        public InstrumentUnitOfWork() {
            Context = new InstrumentContext();
        }

        public InstrumentUnitOfWork(InstrumentContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        private bool _notifications = true;
        public event EventHandler<IDbEventArgs> Notify;
        public void DisableNotifications() {
            _notifications = false;
        }
        public void EnableNotifications() {
            _notifications = true;
        }
        
        public int Save() {
            var instruments = Context.ExtractEntityChanges<Instrument>();
           
            var res = Context.SaveChanges();

            if (Notify != null && _notifications)
                Notify(this, new DbEventArgs(instruments.ExtractIds(), EventSource.Instrument));

            return res;
        }

        public InstrumentContext Context { get; private set; }
}
}