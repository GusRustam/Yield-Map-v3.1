using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class InstrumentTypeCrud : CrudBase<NInstrumentType>, IInstrumentTypeCrud {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.InstrumentTypeCrud");

        public InstrumentTypeCrud(SQLiteConnection connection)
            : base(connection) {
        }

        public InstrumentTypeCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}