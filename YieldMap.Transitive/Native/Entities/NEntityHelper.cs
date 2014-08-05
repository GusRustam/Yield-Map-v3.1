﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Reader;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Native.Entities {
    public class NEntityHelper : INEntityHelper {
        private readonly Dictionary<Type, Dictionary<Operations, string>> _queries;
        private readonly Dictionary<Type, Dictionary<string, Func<object, string>>> _rules 
            = new Dictionary<Type, Dictionary<string, Func<object, string>>>();
        private readonly Dictionary<Type, PropertyRecord[]> _properties
            = new Dictionary<Type, PropertyRecord[]>();
        private readonly Dictionary<Type, Func<SQLiteDataReader, object>> _readers = 
            new Dictionary<Type, Func<SQLiteDataReader, object>>();

        public IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T> {
            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var addition = instrumentChunk.Aggregate(" SELECT ", (unitedSql, instrument) => {
                        var valuesList =
                            _properties[type]
                                .Where(p => p.DbName != "id")
                                .Aggregate("", (allFields, p) => {
                                    var formattedField = _rules[type][p.Info.Name](p.Info.GetValue(instrument));
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
                                .Where(p => p.DbName != "id")
                                .Aggregate(_queries[type][Operations.Update], (allFields, p) => {
                                    var formattedField = _rules[type][p.Info.Name](p.Info.GetValue(instrument));
                                    return allFields + p.DbName + " = " + formattedField + ", ";
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
                                .Where(p => p.DbName != "id")
                                .Aggregate("", (condition, p) => {
                                    var formattedField = _rules[type][p.Info.Name](p.Info.GetValue(instrument));
                                    return condition + p.DbName + " = " + formattedField + " AND ";
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

        public string DeleteAllSql<T>() {
            return _queries[typeof (T)][Operations.Delete];
        }

        public string SelectSql<T>() where T : IIdentifyable {
            return _queries[typeof(T)][Operations.Read];
        }

        public string FindIdSql<T>(T instrument) where T : IIdentifyable {
            var type = typeof (T);
            var typeName = type.Name;
            var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

            var valuesList = _properties[type].Where(p => p.DbName != "id").Aggregate("", (condition, p) => {
                var formattedField = _rules[type][p.Info.Name](p.Info.GetValue(instrument));
                return condition + p.DbName + " = " + formattedField + " AND ";
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
                    .Select(x => new PropertyRecord(x.Property, x.Descr.Name))
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
            var types =
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(IIdentifyable)))
                    .ToList();

            _queries = new Dictionary<Type, Dictionary<Operations, string>>();

            foreach (var type in types) {
                PrepareProperties(type);
                PrepareRules(type);

                var typeName = type.Name;
                var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

                var allFields = string.Join(", ", _properties[type].Select(p => p.DbName));
                var valueFields = string.Join(", ", _properties[type].Where(p => p.DbName != "id").Select(p => p.DbName));

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

        public string AllFields<T>() where T : class, IIdentifyable {
            var type = typeof (T);
            return string.Join(", ", _properties[type].Select(p => p.DbName));
        }

        public static Expression GetCall(Type propertyType, int i) {
            var sqliteReaderType = typeof(SQLiteDataReader);
            var helperType = typeof (SqliteReaderHelper);

            var readerExp = Expression.Parameter(typeof(SQLiteDataReader));
            var iExp = Expression.Constant(i);

            if (propertyType == typeof(bool))
                return Expression.Call(readerExp, sqliteReaderType.GetMethod("GetBoolean"), new Expression[] { iExp });

            if (propertyType == typeof(bool?))
                return Expression.Call(helperType.GetMethod("GetNullableBoolean"), new Expression[] { readerExp, iExp });

            if (propertyType == typeof(float))
                return Expression.Call(readerExp, sqliteReaderType.GetMethod("GetGloat"), new Expression[] { iExp });

            if (propertyType == typeof(float?))
                return Expression.Call(helperType.GetMethod("GetNullableFloat"), new Expression[] { readerExp, iExp });

            if (propertyType == typeof(double))
                return Expression.Call(readerExp, sqliteReaderType.GetMethod("GetDouble"), new Expression[] { iExp });

            if (propertyType == typeof(double?))
                return Expression.Call(helperType.GetMethod("GetNullableDouble"), new Expression[] { readerExp, iExp });

            if (propertyType == typeof(int))
                return Expression.Call(readerExp, sqliteReaderType.GetMethod("GetInt16"), new Expression[] { iExp });

            if (propertyType == typeof(int?))
                return Expression.Call(helperType.GetMethod("GetNullableInt16"), new Expression[] { readerExp, iExp });

            if (propertyType == typeof(long))
                return Expression.Call(readerExp, sqliteReaderType.GetMethod("GetInt32"), new Expression[] { iExp });

            if (propertyType == typeof(long?))
                return Expression.Call(helperType.GetMethod("GetNullableInt32"), new Expression[] { readerExp, iExp });

            if (propertyType == typeof(DateTime))
                return Expression.Call(readerExp, sqliteReaderType.GetMethod("GetDateTime"), new Expression[] { iExp });

            if (propertyType == typeof(DateTime?))
                return Expression.Call(helperType.GetMethod("GetNullableDateTime"), new Expression[] { readerExp, iExp });

            if (propertyType == typeof(string))
                return Expression.Call(helperType.GetMethod("GetNullableString"), new Expression[] { readerExp, iExp });

            return null;
        }

        public IIdentifyable Read<T>(SQLiteDataReader reader) where T : class, IIdentifyable {
            var type = typeof(T);
            PrepareReaders(type);
            if (reader.Read()) 
                return _readers[type](reader) as T;
            return null;
        }

        private void PrepareReaders(Type type) {
            if (_readers.ContainsKey(type)) return;

            var properties = _properties[type].Select(r => r.Info).ToArray();

            var memberBindings = new List<MemberBinding>();
            for (var i = 0; i < properties.Length; i++) {
                var property = properties[i];
                var methodCall = GetCall(property.PropertyType, i);

                memberBindings.Add(Expression.Bind(property, methodCall));
            }

            var parser = Expression.Lambda(Expression.MemberInit(Expression.New(type), memberBindings)).Compile() as Func<SQLiteDataReader, object>;
            if (parser != null) _readers.Add(type, parser);
        }
    }
}