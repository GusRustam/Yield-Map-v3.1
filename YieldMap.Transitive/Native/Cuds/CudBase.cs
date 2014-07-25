using System;
using System.Collections.Generic;
using System.Data.SQLite;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Cuds {
    public abstract class CudBase<T> : IDisposable, ICreateUpdateDelete<T> where T : IIdentifyable, IEquatable<T> {
        protected Dictionary<T, Operations> Entities = new Dictionary<T, Operations>();
        protected readonly SQLiteConnection Connection;

        protected CudBase(SQLiteConnection connection) {
            Connection = connection;
            Connection.Open();
        }

        protected CudBase(IConnector connector) {
            Connection = connector.GetConnection();
            Connection.Open();
        }

        public void Dispose() {
            Connection.Close();
            Connection.Dispose();
        }

        private bool Exists(T entity, out Operations? state) {
            if (Entities.ContainsKey(entity)) {
                state = Entities[entity];
                return true;
            }
            state = null;
            return false;
        }

        private void Operate(T item, Operations op) {
            Operations? state;
            if (Exists(item, out state) && state.HasValue)
                Entities.Remove(item);
            Entities.Add(item, op);
        }

        public void Create(T item) {
            Operate(item, item.id == default (long) ? Operations.Create : Operations.Update);
        }

        public void Update(T item) {
            Operate(item, item.id == default(long) ?  Operations.Create : Operations.Update);
        }

        public void Delete(T item) {
            Operate(item, Operations.Delete);
        }

        public abstract void Save();
    }
}