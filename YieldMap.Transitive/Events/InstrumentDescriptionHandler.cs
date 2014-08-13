using System;
using System.Linq;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Procedures;

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
                    var updater = DatabaseBuilder.Container.Resolve<INewFunctionUpdater>();
                    updater.Recalculate<NBondDescriptionView>(view => args.Added.Contains(view.id_Instrument));
                    updater.Recalculate<NBondDescriptionView>(view => args.Changed.Contains(view.id_Instrument));
                    updater.Recalculate<NFrnDescriptionView>(view => args.Added.Contains(view.id_Instrument));
                    updater.Recalculate<NFrnDescriptionView>(view => args.Changed.Contains(view.id_Instrument));
                } catch (Exception e) {
                    Logger.ErrorEx("Failed to recalculate", e);
                }
            } else {
                if (Next != null) Next.Handle(source, args);
            }
        }
    }
}