using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Events {
    public class LastHandler : ITriggerManager {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Events.LastHandler");
        public ITriggerManager Next {
            get { return null; } 
        }

        public void Handle(IDbEventArgs args) {
            Logger.Warn("Unhandled: " + args);
        }
    }
}