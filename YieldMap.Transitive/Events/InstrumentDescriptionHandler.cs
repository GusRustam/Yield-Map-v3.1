using YieldMap.Tools.Logging;
using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Events {
    public class InstrumentDescriptionHandler : TriggerManagerBase {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.InstrumentDescriptionHandler");
        public InstrumentDescriptionHandler(ITriggerManager next)
            : base(next) {
        }

        public override void Handle(IDbEventArgs args) {
            Logger.Trace("Handle()");
            var singleArgs = args as ISingleTable;
            if (singleArgs != null && singleArgs.Source == EventSource.InstrumentDescription) {
                Logger.Info(singleArgs.ToString());
            } else {
                if (Next != null) Next.Handle(args);
            }
        }
    }
}