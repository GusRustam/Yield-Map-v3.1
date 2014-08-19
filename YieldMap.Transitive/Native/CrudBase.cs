using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native {
    public abstract class CrudBase<T> : ICrud<T> where T : class, IIdentifyable, IEquatable<T> {
        public event EventHandler<IDbEventArgs> Notify;
        protected Dictionary<T, Operations> Entities = new Dictionary<T, Operations>();
        abstract protected Logging.Logger Logger { get; }
        protected readonly SQLiteConnection Connection;
        private readonly bool _ownsConnection;
        private readonly INEntityHelper _helper;
        protected readonly IContainer Container;

        protected CrudBase(SQLiteConnection connection, INEntityHelper helper) {
            Connection = connection;
            _ownsConnection = false;
            _helper = helper;
        }

        protected CrudBase(Func<IContainer> containerF) {
            Container = containerF.Invoke();
            var connector = Container.Resolve<IConnector>();
            _helper = Container.Resolve<INEntityHelper>();
            Connection = connector.GetConnection();
            _ownsConnection = true;
            Connection.Open();
        }

        public void Dispose() {
            if (_ownsConnection) {
                Connection.Close();
                Connection.Dispose();
            }
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

        public int Create(T item) {
            Operate(item, item.id == default (long) ? Operations.Create : Operations.Update);
            return 0;
        }

        public int Update(T item) {
            Operate(item, item.id == default(long) ? Operations.Create : Operations.Update);
            return 0;
        }

        public int Delete(T item) {
            Operate(item, Operations.Delete);
            return 0;
        }

        public void DeleteAll() {
            ExecuteSql(_helper.DeleteAllSql<T>());
            Entities.Clear();
        }

        private bool _muted;
        private bool _unmute;

        public void MuteOnce() {
            _muted = true;
            _unmute = true;
        }

        public void Mute() {
            _muted = true;
            _unmute = false;
        }

        public void Unmute() {
            _muted = false;
            _unmute = false;
        }

        public bool Muted() {
            return _muted;
        }

        public int Save()  {
            var res = 0;

            // Operations.Create (have ids only)
            res += Execute<T>(Operations.Create, _helper.BulkInsertSql);
            RetrieveIds(Operations.Create);

            // Operations.Update (have ids only)
            res += Execute<T>(Operations.Update, _helper.BulkUpdateSql);
            RetrieveIds(Operations.Update);

            // Operations.Delete 
            // - by id 
            // - by fields (todo)
            res += Execute<T>(Operations.Delete, _helper.BulkDeleteSql);


            if (!_muted && Notify != null) {
                Notify(this,
                    new DbEventArgs(
                        Entities.Where(kvp => kvp.Value == Operations.Create).Select(kvp => kvp.Key.id).ToArray(),
                        Entities.Where(kvp => kvp.Value == Operations.Update).Select(kvp => kvp.Key.id).ToArray(),
                        Entities.Where(kvp => kvp.Value == Operations.Delete).Select(kvp => kvp.Key.id).ToArray(),
                        NativeSource<T>()));
            }
            if (_muted && _unmute) Unmute();
        
            //foreach (var entity in Entities.Where(entity => entity.Value != Operations.Read).ToList()) 
            //    Entities.Remove(entity.Key);
            Entities.Clear();

            return res;
        }

        private static EventSource NativeSource<TType>() {
            var type = typeof (TType);
            if (type == typeof(NInstrument))
                return EventSource.Instrument;
            if (type == typeof(NProperty))
                return EventSource.Property;
            if (type == typeof(NPropertyValue))
                return EventSource.PropertyValue;

            throw new ArgumentException();
        }

        private void RetrieveIds(Operations operation) {
            foreach (var kvp in Entities) {
                var item = kvp.Key;
                var state = kvp.Value;

                if (state == operation) {
                    var sql = _helper.FindIdSql(item);
                    var id = ExecuteSqlAndReadId(sql);
                    item.id = id;
                }
            }

            var entities = Entities.Where(kvp => kvp.Value == operation).Select(kvp => kvp.Key).ToList();
            foreach (var entity in entities) 
                Entities[entity] = Operations.Read;
        }

        private int Execute<TEntity>(Operations operations, Func<IEnumerable<TEntity>, IEnumerable<string>> generator)  {
            var entitries = ((IEnumerable<TEntity>) Entities
                .Where(x => x.Value == operations)
                .Select(x => x.Key)).ToArray();
            
            generator(entitries)
                .ToList()
                .ForEach(ExecuteSql);

            return entitries.Count();
        }

        private void ExecuteSql(string sql) {
            Logger.Debug(sql);
            try {
                var cmd = Connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            } catch (Exception e) {
                Logger.ErrorEx("", e);
            }
        }

        private long ExecuteSqlAndReadId(string sql) {
            Logger.Debug(sql);
            try {
                var cmd = Connection.CreateCommand();
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader()) return _helper.ReadId(reader);
            } catch (Exception e) {
                Logger.ErrorEx("", e);
                return default(long);
            }
        }

        public IEnumerable<T> FindAll() {
            var res = new List<T>();
            var query = Connection.CreateCommand();
            var sql = _helper.SelectSql<T>();
            Logger.Debug(sql);
            query.CommandText = sql;
            using (var r = query.ExecuteReader()) {
                T read;
                do {
                    read = _helper.Read<T>(r);
                    if (read != null) res.Add(read);
                } while (read != null);
            }
            return res;
        }

        public IEnumerable<T> FindBy(Func<T, bool> predicate) {
            var all = FindAll().ToList();
            return all.Any() ? all.Where(predicate) : new T[] {};
        }

        public T FindById(long id) {
            var query = Connection.CreateCommand();
            var sql = string.Format(_helper.SelectSql<T>() + " WHERE id = {0}", id);
            Logger.Debug(sql);
            query.CommandText = sql;
            using (var r = query.ExecuteReader()) {
                return _helper.Read<T>(r);
            }
        }
    }
}