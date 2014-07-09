using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class EikonEntitiesUnitOfWork : IEikonEntitiesUnitOfWork {
        public EikonEntitiesUnitOfWork() {
            Context = new EikonEntitiesContext();
        }

        public EikonEntitiesUnitOfWork(EikonEntitiesContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public int Save() {
            return Context.SaveChanges();
        }

        public EikonEntitiesContext Context { get; private set; }
    }
}