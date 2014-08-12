using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class FieldDefinitionCrud : CrudBase<NFieldDefinition> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.FieldDefinitionCrud");

        public FieldDefinitionCrud(SQLiteConnection connection, INEntityHelper helper)
            : base(connection, helper) {
        }

        public FieldDefinitionCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}