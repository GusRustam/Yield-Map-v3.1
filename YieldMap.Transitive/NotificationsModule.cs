using Autofac;
using Autofac.Core;
using YieldMap.Transitive.Events;

namespace YieldMap.Transitive {
    class NotificationsModule : Module {
        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration) {
            registration.Activated += (source, e) => {
                if (e.Instance is INotifier) {
                    var notifier = e.Instance as INotifier;
                    var hander = e.Context.Resolve<ITriggerManager>();
                    notifier.Notify += hander.Handle;
                }
            };
        }
    }
}