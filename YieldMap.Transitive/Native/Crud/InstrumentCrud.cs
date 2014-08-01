using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class InstrumentCrud : CrudBase<NInstrument>, IInstrumentCrud {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.InstrumentCrud");

        public InstrumentCrud(SQLiteConnection connection) : base(connection) {
        }

        public InstrumentCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }

        public IEnumerable<NInstrument> FindByRic(IEnumerable<string> rics) {
            var helper = Container.Resolve<INEntityHelper>();
            var sql = "SELECT " + helper.AllFields<NInstrument>() + " FROM " +
                      "Instrument INNER JOIN Ric ON (Instrument.id_Ric = Ric.id) " +
                      "WHERE Ric.Name IN (" + string.Join(", ", rics) + ")";
            var connector = Container.Resolve<IConnector>();
            var res = new List<NInstrument>();
            using (var cnn = connector.GetConnection()) {
                try {
                    cnn.Open();
                    var query = cnn.CreateCommand();
                    query.CommandText = sql;
                    using (var reader = query.ExecuteReader()) {
                        var item = helper.Read<NInstrument>(reader) as NInstrument;
                        while (item != null) {
                            res.Add(item);
                            item = helper.Read<NInstrument>(reader) as NInstrument;
                        }
                    }
                } finally {
                    cnn.Close();
                }
            }
            return res;
        }
    }
}