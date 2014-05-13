using System;
using System.Data.Entity.Validation;
using YieldMap.Tools.Logging;

namespace YieldMap.Database {
    public static class Tools {
        public static void Report(this Logging.Logger logger, DbEntityValidationException e) {
            foreach (var eve in e.EntityValidationErrors) {
                logger.Error(
                    String.Format(
                        "Entity of type [{0}] in state [{1}] has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));

                foreach (var ve in eve.ValidationErrors)
                    logger.Error(String.Format("- Property: [{0}], Error: [{1}]", ve.PropertyName, ve.ErrorMessage));
            }
        }
    }
}
