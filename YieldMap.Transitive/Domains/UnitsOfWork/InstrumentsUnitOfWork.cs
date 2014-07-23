using System;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class InstrumentsUnitOfWork : IInstrumentsUnitOfWork {
        public InstrumentsUnitOfWork() {
            Context = new InstrumentContext();
        }

        public InstrumentsUnitOfWork(InstrumentContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public event EventHandler<IDbEventArgs> Notify;

        public int Save() {
            return Context.SaveChanges();
        }

        public InstrumentContext Context { get; private set; }
    }
}