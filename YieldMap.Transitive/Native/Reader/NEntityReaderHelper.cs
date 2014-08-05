using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class NEntityReaderHelper : INEntityReaderHelper {
        private readonly Dictionary<Type, string> _queries = 
            new Dictionary<Type, string>();
        private readonly Dictionary<Type, PropertyRecord[]> _properties = 
            new Dictionary<Type, PropertyRecord[]>();
        private readonly Dictionary<Type, Func<SQLiteDataReader, object>> _readers =
            new Dictionary<Type, Func<SQLiteDataReader, object>>();

        public NEntityReaderHelper() {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(INotIdentifyable)))
                .ToList();

            foreach (var type in types) {
                PrepareProperties(type);

                var typeName = type.Name;
                var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

                var allFields = string.Join(", ", _properties[type].Select(p => p.DbName));

                _queries.Add(type, string.Format("SELECT {1} FROM {0}", name, allFields));
            }
        }

        private void PrepareProperties(Type type) {
            if (_properties.ContainsKey(type))
                return;

            _properties.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new { Descr = p.GetCustomAttribute<DbFieldAttribute>(), Property = p })
                    .Where(x => x.Descr != null)
                    .OrderBy(x => x.Descr.Order)
                    .Select(x => new PropertyRecord(x.Property, x.Descr.Name))
                    .ToArray());
        }

        public string SelectSql<T>() {
            return _queries[typeof (T)];
        }

        public T Read<T>(SQLiteDataReader reader) where T : class, INotIdentifyable {
            var type = typeof(T);
            PrepareReaders(type);
            if (reader.Read())
                return _readers[type](reader) as T;
            return null;
        }

        private void PrepareReaders(Type type) {
            if (_readers.ContainsKey(type))
                return;

            var properties = _properties[type].Select(r => r.Info).ToArray();

            var memberBindings = new List<MemberBinding>();
            for (var i = 0; i < properties.Length; i++) {
                var property = properties[i];
                var methodCall = NEntityHelper.GetCall(property.PropertyType, i);

                memberBindings.Add(Expression.Bind(property, methodCall));
            }

            var parser = Expression.Lambda(Expression.MemberInit(Expression.New(type), memberBindings)).Compile() as Func<SQLiteDataReader, object>;
            if (parser != null)
                _readers.Add(type, parser);
        }
    }
}