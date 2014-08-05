using System;
using System.Data.SQLite;
using Autofac;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Reader;

namespace YieldMap.Transitive.Native.Crud {
    public static class CrudHelper {
        public static ICrud<T> ResolveCrudWithConnection<T>(this IContainer container, SQLiteConnection connection) 
            where T : class, IIdentifyable, IEquatable<T> {
            return container.Resolve<ICrud<T>>(new NamedParameter("connection", connection));
        }
        public static IReader<T> ResolveReaderWithConnection<T>(this IContainer container, SQLiteConnection connection)
            where T : class, INotIdentifyable {
            return container.Resolve<IReader<T>>(new NamedParameter("connection", connection));
        }
    }
}
