using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Events {
    public class LastHandler : TriggerManagerBase {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Events.LastHandler");

        public LastHandler() : base(null) {
        }

        public override void Handle(object source, IDbEventArgs args) {
            Logger.Warn("Unhandled: " + args);
        }
    }
}