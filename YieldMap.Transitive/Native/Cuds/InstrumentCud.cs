using System.Data.SQLite;
using System.Linq;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Cuds {
    public class InstrumentCud : CudBase<NInstrument> {
        public InstrumentCud(SQLiteConnection connection) : base(connection) {
        }

        public InstrumentCud(IConnector connector) : base(connector) {
        }

        public override void Save() {
            // Operations.Create (have ids only)
            Entities
                .Where(x => x.Value == Operations.Create)
                .Select(x => x.Key)
                .BulkInsertSql()
                .ToList()
                .ForEach(sql => {
                    var cmd = Connection.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                });

            // todo should I retrive ids from DB?

            // Operations.Update (have ids only)
            Entities
                .Where(x => x.Value == Operations.Create)
                .Select(x => x.Key)
                .BulkUpdateSql()
                .ToList()
                .ForEach(sql => {
                    var cmd = Connection.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                });

            // Operations.Delete 
            // - by id
            // - by fields
            Entities
                .Where(x => x.Value == Operations.Create)
                .Select(x => x.Key)
                .BulkDeleteSql()
                .ToList()
                .ForEach(sql => {
                    var cmd = Connection.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                });
        }
    }
}