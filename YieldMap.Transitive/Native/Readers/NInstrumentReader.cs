using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Readers {
    public class NInstrumentReader : BaseReadOnlyRepository<NInstrument>, INInstrumentReader {
        const string SelectAll = "SELECT id, Name, id_InstrumentType, id_Description FROM Instrument";

        public NInstrumentReader(SQLiteConnection connection)
            : base(connection) {
        }

        public NInstrumentReader(IConnector connector) : base(connector) {
        }

        public override IQueryable<NInstrument> FindAll() {
            var res = new List<NInstrument>();
            var query = Connection.CreateCommand();
            query.CommandText = SelectAll;
            using (var r = query.ExecuteReader()) {
                while (r.Read()) {
                    res.Add(new NInstrument {
                        id = r.GetInt32(0),
                        Name = r.GetString(1),
                        id_InstrumentType = r.GetInt32(2),
                        id_Description = r.GetInt32(3)
                    });
                }
            }
            return res.AsQueryable();
        }

        public override IQueryable<NInstrument> FindAllIncluding(params Expression<Func<NInstrument, object>>[] inc) {
            throw new NotImplementedException();
        }

        public override IQueryable<NInstrument> FindBy(Func<NInstrument, bool> predicate) {
            return 
                FindAll()
                .Where(x => predicate(x))
                .ToList()
                .AsQueryable();
        }

        public override NInstrument FindById(long id) {
            NInstrument res = null;
            var query = Connection.CreateCommand();
            query.CommandText = string.Format(SelectAll + " WHERE id = {0}", id);
            using (var r = query.ExecuteReader()) {
                while (r.Read()) {
                    res = new NInstrument {
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
