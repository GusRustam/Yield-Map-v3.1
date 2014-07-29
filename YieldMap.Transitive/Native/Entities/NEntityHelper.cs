using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Native.Entities {
    public interface INEntityHelper {
        IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T>;
        IEnumerable<string> BulkUpdateSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T>;
        IEnumerable<string> BulkDeleteSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T>;
        string SelectSql<T>() where T : IIdentifyable;
        string FindIdSql<T>(T instrument) where T : IIdentifyable;
        IIdentifyable Read<T>(SQLiteDataReader reader) where T : IIdentifyable;
        long ReadId(SQLiteDataReader reader);
    }

    public class NEntityHelper : INEntityHelper {
        private readonly Dictionary<Type, Dictionary<Operations, string>> _queries;
        private readonly Dictionary<Type, Dictionary<string, Func<object, string>>> _rules 
            = new Dictionary<Type, Dictionary<string, Func<object, string>>>();
        private readonly Dictionary<Type, PropertyInfo[]> _properties 
            = new Dictionary<Type, PropertyInfo[]>();

        public IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T> {
            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var addition = instrumentChunk.Aggregate(" SELECT ", (unitedSql, instrument) => {
                        var valuesList =
                            _properties[type]
                                .Where(p => p.Name != "id")
                                .Aggregate("", (allFields, p) => {
                                    var formattedField = _rules[type][p.Name](p.GetValue(instrument));
                                    return allFields + formattedField + ", ";
                                });
                        return unitedSql + valuesList.Substring(0, valuesList.Length - ", ".Length) + " UNION SELECT ";
                    });
                    return _queries[type][Operations.Create] + addition.Substring(0, addition.Length - " UNION SELECT ".Length);
                }, 500);

            return res;
        }

        public IEnumerable<string> BulkUpdateSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T> {
            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var updateCommands = instrumentChunk.Aggregate(" ", (unitedSql, instrument) => {
                        var valuesList =
                            _properties[type]
                                .Where(p => p.Name != "id")
                                .Aggregate(_queries[type][Operations.Update], (allFields, p) => {
                                    var formattedField = _rules[type][p.Name](p.GetValue(instrument));
                                    return allFields + p.Name + " = " + formattedField + ", ";
                                });
                        return unitedSql + valuesList.Substring(0, valuesList.Length - ", ".Length) + " WHERE id = " + instrument.id + ";\n";
                    });
                    return 
                        "BEGIN TRANSACTION;\n" + 
                        updateCommands + 
                        "END TRANSACTION;";
                }, 500);

            return res;
        }

        public IEnumerable<string> BulkDeleteSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T> {
            var type = typeof(T);
            var enumerable = instruments as IList<T> ?? instruments.ToList();

            var res = new List<string>();
            if (enumerable.Any(x => x.id != default(long)))
                res.AddRange(enumerable
                    .Where(instrument => instrument.id != default(long))
                    .ChunkedSelect(instrumentChunk => {
                        var deleteCommand = instrumentChunk.Aggregate(
                            _queries[type][Operations.Delete] + " WHERE id IN (",
                            (deleteSql, instrument) => deleteSql + instrument.id + ", ");
                    return deleteCommand.Substring(0, deleteCommand.Length - ", ".Length) + ")";
                }, 500));

            if (enumerable.Any(x => x.id == default(long))) // all
                res.AddRange(enumerable
                    .Where(instrument => instrument.id == default(long))
                    .ChunkedSelect(instrumentChunk => { // by 500
                        var deleteCommands = "";
                        foreach (var instrument in instrumentChunk) {
                            deleteCommands += _queries[type][Operations.Delete] + " WHERE ";
                            var valuesList = _properties[type]
                                .Where(p => p.Name != "id")
                                .Aggregate("", (condition, p) => {
                                    var formattedField = _rules[type][p.Name](p.GetValue(instrument));
                                    return condition + p.Name + " = " + formattedField + " AND ";
                                });
                            deleteCommands += valuesList.Substring(0, valuesList.Length - " AND ".Length) + ";\n";
                        }
                    return
                        "BEGIN TRANSACTION;\n" +
                        deleteCommands +
                        "END TRANSACTION;";
                }, 500));

            return res;
        }

        public string SelectSql<T>() where T : IIdentifyable {
            return _queries[typeof(T)][Operations.Read];
        }

        public string FindIdSql<T>(T instrument) where T : IIdentifyable {
            var type = typeof (T);
            var typeName = type.Name;
            var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

            var valuesList = _properties[type].Where(p => p.Name != "id").Aggregate("", (condition, p) => {
                var formattedField = _rules[type][p.Name](p.GetValue(instrument));
                return condition + p.Name + " = " + formattedField + " AND ";
            });

            return string.Format("SELECT id FROM {0} WHERE {1}", name, valuesList.Substring(0, valuesList.Length - " AND ".Length));
            
        }

        public void PrepareProperties(Type type) {
            if (_properties.ContainsKey(type))
                return;

            _properties.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new { Descr = p.GetCustomAttribute<DbFieldAttribute>(), Property = p })
                    .Where(x => x.Descr != null)
                    .OrderBy(x => x.Descr.Order)
                    .Select(x => x.Property)
                    .ToArray());
        }

        public void PrepareRules(Type type) {
            if (_rules.ContainsKey(type))
                return;

            _rules.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => {
                        if (p.PropertyType == typeof(float?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableDouble) };
                        if (p.PropertyType == typeof(float))
                            return new { p.Name, Function = new Func<object, string>(ParseDouble) };
                        if (p.PropertyType == typeof(double?))
                            return new { p.Name, Function = new Func<object, string>(ParseNullableFloat) };
                        if (p.PropertyType == typeof(double))
                            return new { p.Name, Function = new Func<object, string>(ParseFloat) };
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
            return "'" + ((string) item).Replace("'", "''").Replace("\"", "\"\"") + "'"; 
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
        
        public NEntityHelper() {
            var types = new[] {typeof(NInstrument), typeof(NProperty), typeof(NFeed)}; // todo automate

            _queries = new Dictionary<Type, Dictionary<Operations, string>>();

            foreach (var type in types) {
                PrepareProperties(type);
                PrepareRules(type);

                var typeName = type.Name;
                var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

                var allFields = string.Join(", ", _properties[type].Select(p => p.Name));
                var valueFields = string.Join(", ", _properties[type].Where(p => p.Name != "id").Select(p => p.Name));

                var queries = new Dictionary<Operations, string> {
                    {Operations.Create, string.Format("INSERT INTO {0}({1})", name, valueFields)},
                    {Operations.Read, string.Format("SELECT {1} FROM {0}", name, allFields)},
                    {Operations.Update, string.Format("UPDATE {0} SET ", name)},
                    {Operations.Delete, string.Format("DELETE FROM {0} ", name)}
                };

                _queries.Add(type, queries);
            }
        }

        public long ReadId(SQLiteDataReader reader) {
            return reader.Read() ? reader.GetInt32(0) : default(long);
        }

        public IIdentifyable Read<T>(SQLiteDataReader reader) where T : IIdentifyable {
            // todo this can be also automated via Reflection.Emit
            if (reader.Read()) {
                if (typeof (T) == typeof (NInstrument))
                    return new NInstrument {
                        id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        id_InstrumentType = reader.GetInt32(2),
                        id_Description = reader.GetInt32(3)
                    };
                if (typeof(T) == typeof(NProperty))
                    return new NProperty {
                        id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        Expression = reader.GetString(3),
                        id_InstrumentTpe = reader.GetInt32(4)
                    };
                if (typeof(T) == typeof(NFeed)) {
                    return new NFeed {
                        id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2)
                    };
                }
                throw new ArgumentException("what");
            }
            return null;
        }
    }
}