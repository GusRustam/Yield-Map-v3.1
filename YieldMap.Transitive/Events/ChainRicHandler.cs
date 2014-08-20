using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Events {
    public class ChainRicHandler : TriggerManagerBase {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.ChainRicHandler");

        public ChainRicHandler(ITriggerManager next)
            : base(next) {
        }

        public override void Handle(object source, IDbEventArgs args) {
            Logger.Trace("Handle()");
            if (args.Source == typeof (NChain)) {
                Logger.Info(args.ToString());
            } else if (args.Source == typeof (NRic)) {
                Logger.Info(args.ToString());
            } else {
                if (Next != null)
                    Next.Handle(source, args);
            }
        } 
    }
}