using System;

namespace YieldMap.Transitive.Events {
    public interface INotifier {
        event EventHandler<IDbEventArgs> Notify;
        void DisableNotifications();
        void EnableNotifications();
    }
}
