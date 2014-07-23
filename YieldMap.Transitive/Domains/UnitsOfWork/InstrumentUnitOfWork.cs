using System;
using System.Data;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class InstrumentUnitOfWork : IInstrumentUnitOfWork {
        public InstrumentUnitOfWork() {
            Context = new InstrumentContext();
        }

        public InstrumentUnitOfWork(InstrumentContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public event EventHandler<IDbEventArgs> Notify;

        public int Save() {
            var instruments = Context.ExtractEntityChanges<Instrument>();
           
            var res = Context.SaveChanges();

            if (Notify != null)
                Notify(this, new SingleTable(instruments.ExtractIds(), EventSource.InstrumentDescription));

            return res;
        }

        public InstrumentContext Context { get; private set; }
}
}