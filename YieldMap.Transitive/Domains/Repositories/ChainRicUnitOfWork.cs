using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
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

        public int Save() {
            return Context.SaveChanges();
        }

        public ChainRicContext Context { get; private set; }
    }
}