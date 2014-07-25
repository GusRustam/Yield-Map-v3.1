using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using YieldMap.Database;
using YieldMap.Transitive.Native.Cuds;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Native.Entities {
    public static class NEntityHelper {
        private static readonly Dictionary<Type, Dictionary<Operations, string>> Queries;
        private static readonly Dictionary<Type, Dictionary<string, Func<object, string>>> Rules 
            = new Dictionary<Type, Dictionary<string, Func<object, string>>>();
        private static readonly Dictionary<Type, PropertyInfo[]> Properties 
            = new Dictionary<Type, PropertyInfo[]>();

        public static IEnumerable<string> BulkInsertSql<T>(this IEnumerable<T> instruments) where T : IIdentifyable {
            PrepareProperties<T>();
            PrepareRules<T>();

            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var addition = Enumerable.Aggregate<T, string>(instrumentChunk, " ", (unitedSql, instrument) => {
                        var valuesList =
                            Properties[type]
                                .Aggregate("", (allFields, p) => {
                                    var formattedField = Rules[type][p.Name](p.GetValue(instrument));
                                    return allFields + formattedField + ", ";
                                });
                        return unitedSql + valuesList + " UNION SELECT ";
                    });
                    return Queries[type][Operations.Create] + addition;
                }, 500);

            return res;
        }

        public static IEnumerable<string> BulkUpdateSql<T>(this IEnumerable<T> instruments) where T : IIdentifyable {
            PrepareProperties<T>();
            PrepareRules<T>();

            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var updateCommands = instrumentChunk.Aggregate(" ", (unitedSql, instrument) => {
                        var valuesList =
                            Properties[type]
                                .Aggregate(Queries[type][Operations.Update], (allFields, p) => {
                                    var formattedField = Rules[type][p.Name](p.GetValue(instrument));
                                    return allFields + p.Name + " = " + formattedField + ", ";
                                });
                        return unitedSql + valuesList + " WHERE id = " + instrument.id + ";\n";
                    });
                    return 
                        "BEGIN TRANSACTION;\n" + 
                        updateCommands + 
                        "END TRANSACTION;";
                }, 500);

            return res;
        }

        public static IEnumerable<string> BulkDeleteSql<T>(this IEnumerable<T> instruments) where T : IIdentifyable {
            PrepareProperties<T>();
            PrepareRules<T>();

            var type = typeof(T);
            var enumerable = instruments as IList<T> ?? instruments.ToList();

            var res =
                enumerable.ChunkedSelect(instrumentChunk => {
                    var deleteCommand = instrumentChunk
                        .Where(instrument => instrument.id != default(long))
                        .Aggregate(
                            Queries[type][Operations.Delete] + " WHERE id IN (",
                            (deleteSql, instrument) => deleteSql + instrument.id + ", ");
                    return deleteCommand + ")";
                }, 500).ToList();

            res.AddRange(
                enumerable.ChunkedSelect(instrumentChunk => {
                    var deleteCommands = instrumentChunk
                        .Where(instrument => instrument.id == default(long))
                        .Aggregate(
                            Queries[type][Operations.Delete] + " WHERE ",
                            (unitedSql, instrument) => {
                                var valuesList =
                                    Properties[type]
                                        .Aggregate("", (allFields, p) => {
                                            var formattedField = Rules[type][p.Name](p.GetValue(instrument));
                                            return allFields + p.Name + " = " + formattedField + " AND ";
                                        });
                                return unitedSql + valuesList;
                            });
                    return
                        "BEGIN TRANSACTION;\n" +
                        deleteCommands +
                        "END TRANSACTION;";
                }, 500)
                );

            return res;
        }

        private static void PrepareProperties<T>() {
            var type = typeof(T);
            if (Properties.ContainsKey(type)) return;

            Properties.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new {Descr = p.GetCustomAttribute<DbFieldAttribute>(), Property = p})
                    .Where(x => x.Descr != null)
                    .OrderBy(x => x.Descr.Order)
                    .Select(x => x.Property)
                    .ToArray());
        }

        private static void PrepareRules<T>() {
            var type = typeof (T);
            if (Rules.ContainsKey(type)) return;

            Rules.Add(type,
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
            return ((string) item).Replace("'", "''").Replace("\"", "\"\"");
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
        static NEntityHelper() {
            Queries = new Dictionary<Type, Dictionary<Operations, string>>();

            var instrumentQueries = new Dictionary<Operations, string> {
                {Operations.Create, "INSERT INTO Instrument(Name, id_InstrumentType, id_Description) "},
                {Operations.Read, "SELECT id, Name, id_InstrumentType, id_Description FROM Instrument "},
                {Operations.Update, "UPDATE Instrument SET "},
                {Operations.Delete, "DELETE FROM Instrument "}
            };

            Queries.Add(typeof(Instrument), instrumentQueries);
        }

        public static IIdentifyable Read<T>(this SQLiteDataReader reader) {
            if (typeof(T) == typeof(NInstrument))
                return new NInstrument {
                    id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    id_InstrumentType = reader.GetInt32(2),
                    id_Description = reader.GetInt32(3)
                };
            throw new ArgumentException("what");
        }
    }
}