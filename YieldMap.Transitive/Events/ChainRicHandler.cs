using YieldMap.Tools.Logging;
using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Events {
    public class ChainRicHandler : TriggerManagerBase {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.ChainRicHandler");

        public ChainRicHandler(ITriggerManager next)
            : base(next) {
        }

        public override void Handle(IDbEventArgs args) {
            Logger.Trace("Handle()");
            switch (args.Source) {
                case EventSource.Chain:
                    Logger.Info(
                        string.Format(
                            "Got updates on Chains with ids: added ({0}), changed({1}), removed({2})", 
                            args.Added,
                            args.Changed,
                            args.Removed
                        ));
                    break;
                case EventSource.Ric:
                    Logger.Info(
                        string.Format(
                            "Got updates on Rics with ids: added ({0}), changed({1}), removed({2})",
                            args.Added,
                            args.Changed,
                            args.Removed
                        ));  
                    break;
                default:
                    if (Next != null)
                        Next.Handle(args);
                    break;
            }
        }
    }
}