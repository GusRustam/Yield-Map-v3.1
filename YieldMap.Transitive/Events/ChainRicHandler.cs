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
            if (args is ISingleTable) {
                var singleArgs = args as ISingleTable;
                switch (singleArgs.Source) {
                    case EventSource.Chain:
                        Logger.Info(singleArgs.ToString());
                        break;
                    case EventSource.Ric:
                        Logger.Info(singleArgs.ToString());
                        break;
                    default:
                        if (Next != null)
                            Next.Handle(args);
                        break;
                }
            } else {
                if (Next != null)
                    Next.Handle(args);
            }
        } 
    }
}