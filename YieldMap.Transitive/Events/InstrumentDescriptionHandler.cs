using System;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Events {
    public class InstrumentDescriptionHandler : TriggerManagerBase {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.InstrumentDescriptionHandler");
        public InstrumentDescriptionHandler(ITriggerManager next)
            : base(next) {
        }

        public override void Handle(object source, IDbEventArgs args) {
            Logger.Trace("Handle()");
            if (args != null && args.Source == EventSource.Instrument) {
                Logger.Debug("Recalculating properties for instruments");
                Logger.Debug(args.ToString());
                try {
                    var updater = DatabaseBuilder.Container.Resolve<IPropertyUpdater>();
                    updater.RecalculateAll(new Set<long>(args.Added) + new Set<long>(args.Changed));
                } catch (Exception e) {
                    Logger.ErrorEx("Failed to recalculate", e);
                }
            } else {
                if (Next != null) Next.Handle(source, args);
            }
        }
    }
}