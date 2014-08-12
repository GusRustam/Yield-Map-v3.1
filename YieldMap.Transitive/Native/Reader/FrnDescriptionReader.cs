using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class FrnDescriptionReader : ReaderBase<NFrnDescriptionView> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.FrnDescriptionReader");

        public FrnDescriptionReader(SQLiteConnection connection, INEntityReaderHelper helper)
            : base(connection, helper) {
        }

        public FrnDescriptionReader(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}