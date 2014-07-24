using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Native;

namespace YieldMap.Transitive.Domains.NativeContext {
    public class NPropertiesReader : BaseReadOnlyRepository<Property>, INPropertiesReader {
        const string SelectAll = "SELECT id, Name, Description, Expression, id_InstrumentTpe FROM Property";
        public NPropertiesReader(SQLiteConnection connection)
            : base(connection) {
        }

        public NPropertiesReader(IConnector connector) : base(connector) {
        }

        public override IQueryable<Property> FindAll() {
            var res = new List<Property>();
            var query = Connection.CreateCommand();
            query.CommandText = SelectAll;
            using (var r = query.ExecuteReader()) {
                while (r.Read()) {
                    res.Add(new Property {
                        id = r.GetInt32(0),
                        Name = r.GetString(1),
                        Description = r.GetString(2),
                        Expression = r.GetString(3),
                        id_InstrumentTpe = r.GetInt32(4)
                    });
                }
            }
            return res.AsQueryable();
        }

        public override IQueryable<Property> FindAllIncluding(params Expression<Func<Property, object>>[] inc) {
            throw new NotImplementedException();
        }

        public override IQueryable<Property> FindBy(Func<Property, bool> predicate) {
            return
                FindAll()
                    .Where(x => predicate(x))
                    .ToList()
                    .AsQueryable();
        }

        public override Property FindById(long id) {
            Property res = null;
            var query = Connection.CreateCommand();
            query.CommandText = string.Format(SelectAll + " WHERE id = {0}", id);
            using (var r = query.ExecuteReader()) {
                while (r.Read()) {
                    res = new Property {
                        id = r.GetInt32(0),
                        Name = r.GetString(1),
                        Description = r.GetString(2),
                        Expression = r.GetString(3),
                        id_InstrumentTpe = r.GetInt32(4)
                    };
                    break;
                }
            }
            return res;
        }
    }
}