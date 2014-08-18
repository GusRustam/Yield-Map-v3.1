using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace YieldMap.Transitive.Native {
    public class NEntityCache : INEntityCache {
        private readonly Dictionary<Type, Dictionary<Operations, string>> _queries =
            new Dictionary<Type, Dictionary<Operations, string>>();

        public Dictionary<Type, Dictionary<Operations, string>> Queries {
            get { return _queries; }
        }

        public Dictionary<Type, PropertyRecord[]> Properties {
            get { return _properties; }
        }

        public Dictionary<Type, Func<SQLiteDataReader, object>> Readers {
            get { return _readers; }
        }

        public Dictionary<Type, Dictionary<string, Func<object, string>>> Rules {
            get { return _rules; }
        }

        private readonly Dictionary<Type, PropertyRecord[]> _properties =
            new Dictionary<Type, PropertyRecord[]>();

        private readonly Dictionary<Type, Func<SQLiteDataReader, object>> _readers =
            new Dictionary<Type, Func<SQLiteDataReader, object>>();

        private readonly Dictionary<Type, Dictionary<string, Func<object, string>>> _rules =
            new Dictionary<Type, Dictionary<string, Func<object, string>>>();

        public void PrepareProperties(Type type) {
            if (_properties.ContainsKey(type))
                return;

            _properties.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new { Descr = p.GetCustomAttribute<DbFieldAttribute>(), Property = p })
                    .Where(x => x.Descr != null)
                    .OrderBy(x => x.Descr.Order)
                    .Select(x => new PropertyRecord(x.Property, x.Descr.Name, GetTableName(type)))
                    .ToArray());
        }

        private static string GetTableName(Type type) {
            var name = type.Name;
            return 
                name.StartsWith("N")
                ? (name.Length > 1
                    ? (name[1] >= 'A' && name[1] <= 'Z' ? name.Substring(1) : name)
                    : name)
                : name;
        }


        public void PrepareReaders(Type type) {
            if (_readers.ContainsKey(type))
                return;

            var readerExp = Expression.Parameter(typeof(SQLiteDataReader));
            var properties = _properties[type].Select(r => r.Info).ToArray();

            var memberBindings = new List<MemberBinding>();
            for (var i = 0; i < properties.Length; i++) {
                var property = properties[i];
                var methodCall = SqliteReaderHelper.GetCall(property.PropertyType, readerExp, i);

                memberBindings.Add(Expression.Bind(property, methodCall));
            }

            var parser = Expression.Lambda(Expression.MemberInit(Expression.New(type), memberBindings), new[] { readerExp }).Compile() as Func<SQLiteDataReader, object>;
            if (parser != null)
                _readers.Add(type, parser);
        }


        public void PrepareRules(Type type) {
            if (_rules.ContainsKey(type))
                return;

            _rules.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => {
                        if (p.PropertyType == typeof(float?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableFloat) };
                        if (p.PropertyType == typeof(float))
                            return new { p.Name, Function = new Func<object, string>(ParseFloat) };
                        if (p.PropertyType == typeof(double?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableDouble) };
                        if (p.PropertyType == typeof(double))
                            return new { p.Name, Function = new Func<object, string>(ParseDouble) };
                        if (p.PropertyType == typeof(long?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableLong) };
                        if (p.PropertyType == typeof(long))
                            return new { p.Name, Function = new Func<object, string>(ParseLong) };
                        if (p.PropertyType == typeof(int?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableInt) };
                        if (p.PropertyType == typeof(int))
                            return new { p.Name, Function = new Func<object, string>(ParseInt) };
                        if (p.PropertyType == typeof(DateTime?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableDate) };
                        if (p.PropertyType == typeof(DateTime))
                            return new { p.Name, Function = new Func<object, string>(ParseDate) };
                        if (p.PropertyType == typeof(bool?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableBool) };
                        if (p.PropertyType == typeof(bool))
                            return new { p.Name, Function = new Func<object, string>(ParseBool) };
                        return p.PropertyType == typeof(string) ? new { p.Name, Function = new Func<object, string>(ParseString) } : null;
                        // todo other type converters
                    })
                    .Where(x => x != null)
                    .ToDictionary(x => x.Name, x => x.Function)
                );
        }

        private static string ParseString(object item) {
            return item != null ? //item.ToString() : String.Empty;
                "'" + ((string)item).Replace("'", "''").Replace("\"", "\"\"") + "'" : "''";
        }
        private static string ParseLong(object item) {
            var value = (long)item;
            return value.ToString(CultureInfo.InvariantCulture);
        }
        private static string ParseNullableLong(object item) {
            var value = (long?)item;
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
        }
        private static string ParseFloat(object item) {
            var value = (float)item;
            return value.ToString(CultureInfo.InvariantCulture);
        }
        private static string ParseNullableFloat(object item) {
            var value = (float?)item;
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
        }
        private static string ParseDouble(object item) {
            var value = (double)item;
            return value.ToString(CultureInfo.InvariantCulture);
        }
        private static string ParseNullableDouble(object item) {
            var value = (double?)item;
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
        }
        private static string ParseInt(object item) {
            var value = (int)item;
            return value.ToString(CultureInfo.InvariantCulture);
        }
        private static string ParseNullableInt(object item) {
            var value = (int?)item;
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
        }
        private static string ParseBool(object item) {
            var value = (bool)item;
            return value ? "1" : "0";
        }
        private static string ParseNullableBool(object item) {
            var value = (bool?)item;
            return value.HasValue ? (value.Value ? "1" : "0") : "NULL";
        }
        private static string ParseDate(object item) {
            var value = (DateTime)item;
            return String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", value.ToLocalTime());
        }
        private static string ParseNullableDate(object item) {
            var value = (DateTime?)item;
            return value.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", value.Value.ToLocalTime()) : "NULL";
        }
    }
}