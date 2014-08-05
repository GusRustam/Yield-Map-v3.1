using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class InstrumentIBViewReader : ReaderBase<NInstrumentIBView> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.InstrumentIBViewReader");

        public InstrumentIBViewReader(SQLiteConnection connection)
            : base(connection) {
        }

        public InstrumentIBViewReader(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}