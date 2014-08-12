using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class InstrumentRicViewReader : ReaderBase<NInstrumentRicView> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.InstrumentRicViewReader");

        public InstrumentRicViewReader(SQLiteConnection connection, INEntityReaderHelper helper)
            : base(connection,helper) {
        }

        public InstrumentRicViewReader(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}