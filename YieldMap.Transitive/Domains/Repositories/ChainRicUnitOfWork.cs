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
            //var deletedChains = Context.ChangeTracker.Entries<Chain>().Where(e => e.State == EntityState.Deleted);
            //var deletedRtcs = Context.ChangeTracker.Entries<RicToChain>().Where(e => e.State == EntityState.Deleted).ToList();
            //Debug.Assert(!deletedRtcs.Any());
            //deletedChains.ToList().ForEach(e => {
            //    var chain = e.Entity;
            //    var rtcs = Context.RicToChains.Where(rtc => rtc.Chain_id == chain.id).ToList();
            //    rtcs.ForEach(rtc => Context.RicToChains.Remove(rtc));
            //});
            //Debug.Assert(deletedRtcs.Any());
            return Context.SaveChanges();
        }

        public ChainRicContext Context { get; private set; }
    }
}