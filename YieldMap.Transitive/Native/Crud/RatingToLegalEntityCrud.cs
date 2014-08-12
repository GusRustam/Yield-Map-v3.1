using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class RatingToLegalEntityCrud : CrudBase<NRatingToLegalEntity> {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.RatingToLegalEntityCrud");

        public RatingToLegalEntityCrud(SQLiteConnection connection, INEntityHelper helper)
            : base(connection, helper) {
        }

        public RatingToLegalEntityCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}