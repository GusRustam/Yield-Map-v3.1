using YieldMap.Transitive.Domains.Contexts;

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

        public int Save() {
            return Context.SaveChanges();
        }

        public BondAdditionContext Context { get; private set; }
}
}