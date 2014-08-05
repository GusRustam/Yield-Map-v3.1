using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public class RatingToInstrumentCrud : CrudBase<NRatingToInstrument>, IRatingToInstrumentCrud {
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.Native.RatingToInstrumentCrud");

        public RatingToInstrumentCrud(SQLiteConnection connection)
            : base(connection) {
        }

        public RatingToInstrumentCrud(Func<IContainer> containerF)
            : base(containerF) {
        }

        protected override Logging.Logger Logger {
            get { return TheLogger; }
        }
    }
}