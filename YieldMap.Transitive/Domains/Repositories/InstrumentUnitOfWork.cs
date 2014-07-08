using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
    public class InstrumentUnitOfWork : IInstrumentUnitOfWork {
        public InstrumentUnitOfWork() {
            Context = new BondAdditionContext();
        }

        public InstrumentUnitOfWork(BondAdditionContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public int Save() {
            return Context.SaveChanges();
        }

        public BondAdditionContext Context { get; private set; }
}
}