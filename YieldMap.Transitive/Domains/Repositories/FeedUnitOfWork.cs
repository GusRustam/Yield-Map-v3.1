using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
    public class FeedUnitOfWork : IFeedUnitOfWork {
        public FeedUnitOfWork() {
            Context = new EikonEntitiesContext();
        }

        public FeedUnitOfWork(EikonEntitiesContext context) {
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