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
                    Logger.Info(args.ToString());
                    break;
                case EventSource.Ric:
                    Logger.Info(args.ToString());
                    break;
                default:
                    if (Next != null)
                        Next.Handle(args);
                    break;
            }
        } 
    }
}