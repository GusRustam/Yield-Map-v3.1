using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using Autofac;

namespace YieldMap.Transitive.Native {
    public class NEntityReaderHelper : INEntityReaderHelper {
        private readonly INEntityCache _cache;

        public NEntityReaderHelper(Func<IContainer> containerFunc) {
            var container = containerFunc();

            _cache = container.Resolve<INEntityCache>();

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(INotIdentifyable)))
                .ToList();

            foreach (var type in types) {
                _cache.PrepareProperties(type);

                var typeName = type.Name;
                var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

                var allFields = string.Join(", ", _cache.Properties[type].Select(p => p.DbName));

                _cache.Queries.Add(type, new Dictionary<Operations, string> {
                    {Operations.Read, string.Format("SELECT {1} FROM {0}", name, allFields)}
                });
            }
        }


        public string SelectSql<T>() {
            return _cache.Queries[typeof (T)][Operations.Read];
        }

        public T Read<T>(SQLiteDataReader reader) where T : class, INotIdentifyable {
            var type = typeof(T);
            _cache.PrepareReaders(type);
            if (reader.Read())
                return _cache.Readers[type](reader) as T;
            return null;
        }

        
    }
}