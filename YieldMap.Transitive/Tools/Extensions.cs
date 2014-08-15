using System;
using System.Collections.Generic;
using System.Linq;

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

        public static void ChunkedForEach<T>(this IEnumerable<T> items, Action<IEnumerable<T>> action, int chunkSize) {
            var list = items.ToList();
            var length = list.Count();

            if (length == 0) return;

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

        public static IEnumerable<TResult> ChunkedSelect<TItem, TResult>(this IEnumerable<TItem> items, Func<IEnumerable<TItem>, TResult> action, int chunkSize) {
            var res = new List<TResult>();

            var list = items.ToList();
            var length = list.Count();

            if (length == 0)
                return res;

            var iteration = 0;
            bool finished;

            do {
                var minRange = iteration * chunkSize;
                finished = minRange + chunkSize > length;
                var maxRange = (finished ? length : minRange + chunkSize) - 1;
                res.Add(action(list.GetRange(minRange, maxRange - minRange + 1)));
                iteration = iteration + 1;
            } while (!finished);
            return res;
        }
    }
}