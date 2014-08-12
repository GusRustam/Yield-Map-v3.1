using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Autofac;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Native {
    public abstract class ReaderBase<T> : IReader<T> where T : class, INotIdentifyable {
        abstract protected Logging.Logger Logger { get; }
        protected readonly SQLiteConnection Connection;
        private readonly bool _ownsConnection;
        private readonly INEntityReaderHelper _helper;

        protected ReaderBase(SQLiteConnection connection, INEntityReaderHelper helper) {
            Connection = connection;
            _ownsConnection = false;
            _helper = helper;
        }

        protected ReaderBase(Func<IContainer> containerF) {
            var container = containerF.Invoke();
            var connector = container.Resolve<IConnector>();
            _helper = container.Resolve<INEntityReaderHelper>();
            Connection = connector.GetConnection();
            Connection.Open();
            _ownsConnection = true;
        }

        public void Dispose() {
            if (_ownsConnection) {
                Connection.Close();
                Connection.Dispose();
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
}