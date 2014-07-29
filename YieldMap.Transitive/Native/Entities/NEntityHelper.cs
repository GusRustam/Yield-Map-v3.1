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
        IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : IIdentifyable;
        IEnumerable<string> BulkUpdateSql<T>(IEnumerable<T> instruments) where T : IIdentifyable;
        IEnumerable<string> BulkDeleteSql<T>(IEnumerable<T> instruments) where T : IIdentifyable;
        string SelectSql<T>() where T : IIdentifyable;
        void PrepareProperties<T>();
        void PrepareRules<T>();
        IIdentifyable Read<T>(SQLiteDataReader reader) where T : IIdentifyable;
    }

    public class NEntityHelper : INEntityHelper {
        private readonly Dictionary<Type, Dictionary<Operations, string>> _queries;
        private readonly Dictionary<Type, Dictionary<string, Func<object, string>>> _rules 
            = new Dictionary<Type, Dictionary<string, Func<object, string>>>();
        private readonly Dictionary<Type, PropertyInfo[]> _properties 
            = new Dictionary<Type, PropertyInfo[]>();

        public IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : IIdentifyable {
            PrepareProperties<T>();
            PrepareRules<T>();

            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var addition = instrumentChunk.Aggregate(" SELECT ", (unitedSql, instrument) => {
                        var valuesList =
                            _properties[type]
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

        public IEnumerable<string> BulkUpdateSql<T>(IEnumerable<T> instruments) where T : IIdentifyable {
            PrepareProperties<T>();
            PrepareRules<T>();

            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var updateCommands = instrumentChunk.Aggregate(" ", (unitedSql, instrument) => {
                        var valuesList =
                            _properties[type]
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

        public IEnumerable<string> BulkDeleteSql<T>(IEnumerable<T> instruments) where T : IIdentifyable {
            PrepareProperties<T>();
            PrepareRules<T>();

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
                            var valuesList = _properties[type].Aggregate("", (condition, p) => {
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
            PrepareProperties<T>();
            PrepareRules<T>();
            var type = typeof(T);

            return _queries[type][Operations.Read];
        }

        public void PrepareProperties<T>() {
            var type = typeof(T);
            if (_properties.ContainsKey(type)) return;

            _properties.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new {Descr = p.GetCustomAttribute<DbFieldAttribute>(), Property = p})
                    .Where(x => x.Descr != null)
                    .OrderBy(x => x.Descr.Order)
                    .Select(x => x.Property)
                    .ToArray());
        }

        public void PrepareRules<T>() {
            var type = typeof (T);
            if (_rules.ContainsKey(type)) return;

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
                            return new {p.Name, Function = new Func<object, string>(ParseNullableLong)};
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
                        return p.PropertyType == typeof(string) ? new {p.Name, Function = new Func<object, string>(ParseString)} : null;
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
            _queries = new Dictionary<Type, Dictionary<Operations, string>>();

            // todo this can be automated too
            var instrumentQueries = new Dictionary<Operations, string> {
                {Operations.Create, "INSERT INTO Instrument(Name, id_InstrumentType, id_Description) "},
                {Operations.Read, "SELECT id, Name, id_InstrumentType, id_Description FROM Instrument "},
                {Operations.Update, "UPDATE Instrument SET "},
                {Operations.Delete, "DELETE FROM Instrument "}
            };

            _queries.Add(typeof(NInstrument), instrumentQueries);
        }

        public IIdentifyable Read<T>(SQLiteDataReader reader) where T : IIdentifyable {
            if (reader.Read()) {
                if (typeof (T) == typeof (NInstrument))
                    return  new NInstrument {
                        id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        id_InstrumentType = reader.GetInt32(2),
                        id_Description = reader.GetInt32(3)
                    };
                throw new ArgumentException("what");
            }
            return null;
        }
    }
}