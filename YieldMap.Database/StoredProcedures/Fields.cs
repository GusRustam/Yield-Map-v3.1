using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures {
    public static class Fields {
        public static Field GetDefaultFields(MainEntities ctx = null) {
            if (ctx != null) return DoGetDefaultFields(ctx);

            using (var context = DbConn.CreateContext()) {
                return DoGetDefaultFields(context);
            }
        }

        private static Field DoGetDefaultFields(MainEntities ctx) {
            return ctx.Fields.First(f => f.Default);
        }
    }
}
