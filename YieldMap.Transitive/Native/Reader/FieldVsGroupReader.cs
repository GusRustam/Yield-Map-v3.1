using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class FieldVsGroupReader : ReaderBase<NFieldVsGroup> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.FieldVsGroupReader");

        public FieldVsGroupReader(SQLiteConnection connection, INEntityReaderHelper helper)
            : base(connection, helper) {
        }

        public FieldVsGroupReader(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}