using System;
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
    }
}