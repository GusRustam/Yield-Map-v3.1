using System;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Transitive.Domains;

namespace YieldMap.Transitive.Native {
    public abstract class BaseReadOnlyRepository<T> : IReadOnlyRepository<T> {
        protected readonly SQLiteConnection Connection;

        protected BaseReadOnlyRepository(SQLiteConnection connection) {
            Connection = connection;
            Connection.Open();
        }

        protected BaseReadOnlyRepository(IConnector connector) {
            Connection = connector.GetConnection();
            Connection.Open();
        }

        public void Dispose() {
            Connection.Close();
            Connection.Dispose();
        }
        public abstract IQueryable<T> FindAll();
        public abstract IQueryable<T> FindAllIncluding(params Expression<Func<T, object>>[] inc);
        public abstract IQueryable<T> FindBy(Func<T, bool> predicate);
        public abstract T FindById(long id);
    }
}