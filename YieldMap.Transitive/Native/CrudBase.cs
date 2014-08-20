using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Native {
    public abstract class CrudBase<T> : ICrud<T> where T : class, IIdentifyable, IEquatable<T>, new() {
        public event EventHandler<IDbEventArgs> Notify;
        
        protected Dictionary<T, Operations> Entities = new Dictionary<T, Operations>();
        protected readonly SQLiteConnection Connection;
        protected readonly IContainer Container;
        
        private readonly bool _ownsConnection;
        private readonly INEntityHelper _helper;
        
        // ReSharper disable StaticFieldInGenericType
        private static bool _muted = true;
        private static bool _unmute;
        private static long[] _added;
        private static long[] _changed;
        private static long[] _removed;
        // ReSharper restore StaticFieldInGenericType

        abstract protected Logging.Logger Logger { get; }

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

        public int DeleteById(long id) {
            var item = new T {id = id};
            return Delete(item);
        }

        public void Wipe() {
            ExecuteSql(_helper.DeleteAllSql<T>());
            Entities.Clear();
        }


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

            // Operations.Update (have ids only)
            res += Execute<T>(Operations.Update, _helper.BulkUpdateSql);

            // Operations.Delete 
            // - by id 
            // - by fields (todo)
            res += Execute<T>(Operations.Delete, _helper.BulkDeleteSql);

            _added = Entities.Where(kvp => kvp.Value == Operations.Create).Select(kvp => kvp.Key.id).ToArray();
            _changed = Entities.Where(kvp => kvp.Value == Operations.Update).Select(kvp => kvp.Key.id).ToArray();
            _removed = Entities.Where(kvp => kvp.Value == Operations.Delete).Select(kvp => kvp.Key.id).ToArray();

            if (!_muted && Notify != null) {
                Notify(this, new DbEventArgs(_added,_changed,_removed, typeof(T)));
            }
            if (_muted && _unmute) Unmute();
        
            Entities.Clear();

            return res;
        }

        public DbEventArgs GetUpdates() {
            return new DbEventArgs(_added, _changed, _removed, typeof(T));
        }

        private void RetrieveIds2(Operations operation) {
            var items = Entities.Where(kvp => kvp.Value == operation).Select(kvp => kvp.Key).ToArray();
            if (!items.Any()) return;
            
            items.ChunkedForEach(entities => {
                var arr = entities.ToArray();
                var allSql = _helper.FindIdSql<T>(arr);
                Logger.Debug(allSql);
                try {
                    var cmd = Connection.CreateCommand();
                    cmd.CommandText = allSql;
                    using (var reader = cmd.ExecuteReader()) {
                        long nextId;
                        var i = 0;
                        while ((nextId = _helper.ReadId(reader)) != default(long))
                            arr[i++].id = nextId;

                        if (i != arr.Length)
                            Logger.Error(string.Format(" i = {0} while len = {1}", i, arr.Length));
                    }

                } catch (Exception e) {
                    Logger.ErrorEx("", e);
                    throw;
                }
            }, 500);
        }

        private int Execute<TEntity>(Operations operations, Func<IEnumerable<TEntity>, IEnumerable<string>> generator) {
            var entitries = ((IEnumerable<TEntity>) Entities
                .Where(x => x.Value == operations)
                .Select(x => x.Key)).ToArray();
            
            generator(entitries)
                .ToList()
                .ForEach(ExecuteSql);

            if ((operations == Operations.Create || operations == Operations.Update))
                RetrieveIds2(operations);

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