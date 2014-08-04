using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class OrdinaryBondReader : ReaderBase<NOrdinaryBond> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.OrdinaryBondReader");

        public OrdinaryBondReader(SQLiteConnection connection)
            : base(connection) {
        }

        public OrdinaryBondReader(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}