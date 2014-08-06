using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class ChainCrud : CrudBase<NChain> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.ChainCrud");

        public ChainCrud(SQLiteConnection connection)
            : base(connection) {
        }

        public ChainCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}