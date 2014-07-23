using YieldMap.Transitive.Procedures;

namespace YieldMap.Transitive.Events {
    public class DbEventArgs : IDbEventArgs {
        private readonly IEventDescription _added;
        private readonly IEventDescription _changed;
        private readonly IEventDescription _removed;

        public DbEventArgs(IEventDescription added, IEventDescription changed, IEventDescription removed, EventSource source) {
            _added = added;
            _changed = changed;
            _removed = removed;
            Source = source;
        }

        public EventSource Source { get; private set; }

        public IEventDescription Added {
            get { return _added; }
        }

        public IEventDescription Changed {
            get{ return _changed; }
        }

        public IEventDescription Removed { 
            get { return _removed; }
        }
    }
}