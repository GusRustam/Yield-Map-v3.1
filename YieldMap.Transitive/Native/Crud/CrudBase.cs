using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public abstract class CrudBase<T> : IDisposable, ICrud<T> where T : class, IIdentifyable, IEquatable<T> {
        protected Dictionary<T, Operations> Entities = new Dictionary<T, Operations>();
        abstract protected  Logging.Logger Logger { get; }
        protected readonly SQLiteConnection Connection;
        private readonly INEntityHelper _helper;

        protected CrudBase(SQLiteConnection connection) {
            Connection = connection;
            Connection.Open();
        }

        protected CrudBase(Func<IContainer> containerF) {
            var container = containerF.Invoke();
            var connector = container.Resolve<IConnector>();
            _helper = container.Resolve<INEntityHelper>();
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
            Operate(item, item.id == default(long) ? Operations.Create : Operations.Update);
        }

        public void Delete(T item) {
            Operate(item, Operations.Delete);
        }

        public void Save() {
            // Operations.Create (have ids only)
            Execute(Operations.Create, _helper.BulkInsertSql);
            // todo RETRIEVE IDS AND MOVE TO [Read] STATE

            // Operations.Update (have ids only)
            Execute(Operations.Update, _helper.BulkUpdateSql);
            // todo RETRIEVE IDS AND MOVE TO [Read] STATE

            // Operations.Delete 
            // - by id 
            // - by fields (todo)
            Execute(Operations.Delete, _helper.BulkDeleteSql); 

            foreach (var entity in Entities.Where(entity => entity.Value == Operations.Delete)) {
                Entities.Remove(entity.Key);
            }

        }

        private void Execute(Operations operations, Func<IEnumerable<IIdentifyable>, IEnumerable<string>> generator) {
            var enumerable = Entities
                .Where(x => x.Value == operations)
                .Select(x => x.Key);
            
            generator(enumerable)
                .ToList()
                .ForEach(ExecuteSql);
        }

        private void ExecuteSql(string sql) {
            try {
                var cmd = Connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            } catch (Exception e) {
                Logger.ErrorEx("", e);
            }
        }

        public IEnumerable<T> FindAll() {
            var res = new List<T>();
            var query = Connection.CreateCommand();
            query.CommandText = _helper.SelectSql<T>();
            using (var r = query.ExecuteReader()) {
                T read;
                do {
                    read = _helper.Read<T>(r) as T;
                    if (read != null) res.Add(read);
                } while (read != null);
            }
            return res;
        }

        public IEnumerable<T> FindBy(Func<T, bool> predicate) {
            return
            FindAll()
            .Where(predicate)
            .ToList()
            .AsQueryable();    
        }

        public T FindById(long id) {
            var query = Connection.CreateCommand();
            query.CommandText = string.Format(_helper.SelectSql<T>() + " WHERE id = {0}", id);
            using (var r = query.ExecuteReader()) {
                return _helper.Read<T>(r) as T;
            }
        }
    }
}