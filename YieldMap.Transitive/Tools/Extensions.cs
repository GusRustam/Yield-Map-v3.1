using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Tools {
    internal static class Extensions {
        public static HashSet<T> Add<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Add(item));
            return res;
        }

        public static HashSet<T> Remove<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Remove(item));
            return res;
        }

        public static void Report(this Logging.Logger logger, string msg, DbEntityValidationException e) {
            logger.Error(msg);
            foreach (var eve in e.EntityValidationErrors) {
                logger.Error(
                    String.Format(
                        "Entity of type [{0}] in state [{1}] has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));

                foreach (var ve in eve.ValidationErrors)
                    logger.Error(String.Format("- Property: [{0}], Error: [{1}]", ve.PropertyName, ve.ErrorMessage));
            }
        }

        public static void ChunkedForEach<T>(this IEnumerable<T> items, Action<IEnumerable<T>> action, int chunkSize) {
            var list = items.ToList();
            var length = list.Count();
            var iteration = 0;
            bool finished;

            do {
                var minRange = iteration * chunkSize;
                finished = minRange + chunkSize > length;
                var maxRange = (finished ? length : minRange + chunkSize) - 1;
                action(list.GetRange(minRange, maxRange - minRange + 1));
                iteration = iteration + 1;
            } while (!finished);

        }
    }
}