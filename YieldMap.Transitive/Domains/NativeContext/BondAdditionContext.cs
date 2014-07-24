using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Native;

namespace YieldMap.Transitive.Domains.NativeContext {
    public class InstrumentReader : BaseReadOnlyRepository<Instrument>, IInstrumentReader {
        const string SelectAll = "SELECT id, Name, id_InstrumentType, id_Description FROM Instrument";

        public InstrumentReader(SQLiteConnection connection)
            : base(connection) {
        }

        public InstrumentReader(IConnector connector) : base(connector) {
        }

        public override IQueryable<Instrument> FindAll() {
            var res = new List<Instrument>();
            var query = Connection.CreateCommand();
            query.CommandText = SelectAll;
            using (var r = query.ExecuteReader()) {
                while (r.Read()) {
                    res.Add(new Instrument {
                        id = r.GetInt32(0),
                        Name = r.GetString(1),
                        id_InstrumentType = r.GetInt32(2),
                        id_Description = r.GetInt32(3)
                    });
                }
            }
            return res.AsQueryable();
        }

        public override IQueryable<Instrument> FindAllIncluding(params Expression<Func<Instrument, object>>[] inc) {
            throw new NotImplementedException();
        }

        public override IQueryable<Instrument> FindBy(Func<Instrument, bool> predicate) {
            return 
                FindAll()
                .Where(x => predicate(x))
                .ToList()
                .AsQueryable();
        }

        public override Instrument FindById(long id) {
            Instrument res = null;
            var query = Connection.CreateCommand();
            query.CommandText = string.Format(SelectAll + " WHERE id = {0}", id);
            using (var r = query.ExecuteReader()) {
                while (r.Read()) {
                    res = new Instrument {
                        id = r.GetInt32(0),
                        Name = r.GetString(1),
                        id_InstrumentType = r.GetInt32(2),
                        id_Description = r.GetInt32(3)
                    };
                    break;
                }
            }
            return res;
        }
    }
}
