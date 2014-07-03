using System;
using System.Data;

namespace YieldMap.Database {
    public static class StateHelper {
        public static EntityState ToEntityState(State state) {
            switch (state) {
                case State.Added:
                    return EntityState.Added;
                case State.Unchanged:
                    return EntityState.Unchanged;
                case State.Modified:
                    return EntityState.Modified;
                case State.Deleted:
                    return EntityState.Deleted;
                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }
    }
}