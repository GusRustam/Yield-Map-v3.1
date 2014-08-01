using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public abstract class ReaderBase<T> : IReader<T> where T : class, INotIdentifyable {
        abstract protected Logging.Logger Logger { get; }
        protected readonly SQLiteConnection Connection;
        private readonly INEntityReaderHelper _helper;

        protected ReaderBase(SQLiteConnection connection) {
            Connection = connection;
            Connection.Open();
        }

        protected ReaderBase(Func<IContainer> containerF) {
            var container = containerF.Invoke();
            var connector = container.Resolve<IConnector>();
            _helper = container.Resolve<INEntityReaderHelper>();
            Connection = connector.GetConnection();
            Connection.Open();
        }

        public void Dispose() {
            Connection.Close();
            Connection.Dispose();
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
                    if (read != null)
                        res.Add(read);
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
            var sql = string.Format(_helper.SelectSql<T>() + " WHERE id = {0}", id);
            Logger.Debug(sql);
            query.CommandText = sql;
            using (var r = query.ExecuteReader()) {
                return _helper.Read<T>(r);
            }
        }
    }

    internal class PropertyRecord {
        private readonly PropertyInfo _info;
        private readonly string _dbName;

        public PropertyRecord(PropertyInfo info, string dbName) {
            _info = info;
            _dbName = dbName;
        }

        public string DbName {
            get { return string.IsNullOrEmpty(_dbName) ? _info.Name : _dbName; }
        }

        public PropertyInfo Info {
            get { return _info; }
        }
    }
}