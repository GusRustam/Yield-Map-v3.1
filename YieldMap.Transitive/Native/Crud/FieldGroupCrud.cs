using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class FieldGroupCrud : CrudBase<NFieldGroup> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.FieldGroupCrud");

        public FieldGroupCrud(SQLiteConnection connection)
            : base(connection) {
        }

        public FieldGroupCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}