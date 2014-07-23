using System;
using System.Data;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

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
            var instruments = Context.ExtractChanges<Instrument>();
            
           
            var res = Context.SaveChanges();

            if (Notify != null)
                Notify(this, new SingleTable(instruments, EventSource.InstrumentDescription));

            return res;
        }

        public BondAdditionContext Context { get; private set; }
}
}