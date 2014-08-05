using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class OrdinaryFrnReader : ReaderBase<NOrdinaryFrn> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.OrdinaryFrnReader");

        public OrdinaryFrnReader(SQLiteConnection connection)
            : base(connection) {
        }

        public OrdinaryFrnReader(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}