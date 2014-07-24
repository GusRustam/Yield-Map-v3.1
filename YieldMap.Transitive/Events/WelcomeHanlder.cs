using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Events {
    public class WelcomeHanlder : TriggerManagerBase {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Events.WelcomeHanlder");
        public WelcomeHanlder(ITriggerManager next)
            : base(next) {
        }

        public override void Handle(object source, IDbEventArgs args) {
            Logger.Debug(string.Format("Welcome data from {0}: {1}", source.GetType().Name, args));
            if (Next != null) Next.Handle(source, args);
        }
    }
}