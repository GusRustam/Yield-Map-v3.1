using System;
using System.Data.SQLite;
using Autofac;

namespace YieldMap.Transitive.Native {
    public static class CrudHelper {
        public static ICrud<T> ResolveCrudWithConnection<T>(this IContainer container, SQLiteConnection connection) 
            where T : class, IIdentifyable, IEquatable<T> {
                return container.ResolveKeyed<ICrud<T>>(ConnectionMode.Existing, new NamedParameter("connection", connection));
        }
        public static IReader<T> ResolveReaderWithConnection<T>(this IContainer container, SQLiteConnection connection)
            where T : class, INotIdentifyable {
                return container.ResolveKeyed<IReader<T>>(ConnectionMode.Existing, new NamedParameter("connection", connection));
        }
    }
}
