using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using Autofac;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Native {
    public class NEntityHelper : INEntityHelper {
        private readonly INEntityCache _cache;

        public IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T> {
            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var addition = instrumentChunk.Aggregate(" SELECT ", (unitedSql, instrument) => {
                        var valuesList =
                            _cache.Properties[type]
                                .Where(p => p.DbName != "id")
                                .Aggregate("", (allFields, p) => {
                                    var formattedField = _cache.Rules[type][p.Info.Name](p.Info.GetValue(instrument));
                                    return allFields + formattedField + ", ";
                                });
                        return unitedSql + valuesList.Substring(0, valuesList.Length - ", ".Length) + " UNION SELECT ";
                    });
                    return _cache.Queries[type][Operations.Create] + addition.Substring(0, addition.Length - " UNION SELECT ".Length);
                }, 500);

            return res;
        }

        public IEnumerable<string> BulkUpdateSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T> {
            var type = typeof(T);
            var res =
                instruments.ChunkedSelect(instrumentChunk => {
                    var updateCommands = instrumentChunk.Aggregate(" ", (unitedSql, instrument) => {
                        var valuesList =
                            _cache.Properties[type]
                                .Where(p => p.DbName != "id")
                                .Aggregate(_cache.Queries[type][Operations.Update], (allFields, p) => {
                                    var formattedField = _cache.Rules[type][p.Info.Name](p.Info.GetValue(instrument));
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
                            _cache.Queries[type][Operations.Delete] + " WHERE id IN (",
                            (deleteSql, instrument) => deleteSql + instrument.id + ", ");
                    return deleteCommand.Substring(0, deleteCommand.Length - ", ".Length) + ")";
                }, 500));

            if (enumerable.Any(x => x.id == default(long))) // all
                res.AddRange(enumerable
                    .Where(instrument => instrument.id == default(long))
                    .ChunkedSelect(instrumentChunk => { // by 500
                        var deleteCommands = "";
                        foreach (var instrument in instrumentChunk) {
                            deleteCommands += _cache.Queries[type][Operations.Delete] + " WHERE ";
                            var valuesList = _cache.Properties[type]
                                .Where(p => p.DbName != "id")
                                .Aggregate("", (condition, p) => {
                                    var formattedField = _cache.Rules[type][p.Info.Name](p.Info.GetValue(instrument));
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
            return _cache.Queries[typeof (T)][Operations.Delete];
        }

        public string SelectSql<T>() where T : IIdentifyable {
            return _cache.Queries[typeof(T)][Operations.Read];
        }

        public string FindIdSql<T>(T instrument) where T : IIdentifyable {
            var type = typeof (T);
            var typeName = type.Name;
            var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

            var valuesList = _cache.Properties[type].Where(p => p.DbName != "id").Aggregate("", (condition, p) => {
                var formattedField = _cache.Rules[type][p.Info.Name](p.Info.GetValue(instrument));
                return condition + p.DbName + (formattedField != "NULL" ? " = " : " IS ") + formattedField + " AND ";
            });

            return string.Format("SELECT id FROM {0} WHERE {1}", name, valuesList.Substring(0, valuesList.Length - " AND ".Length));
        }

        public string FindIdSql<T>(IEnumerable<T> instruments) where T : IIdentifyable {
            var i = instruments as List<T> ?? instruments.ToList();
            if (i.Any()) return string.Join(" UNION ALL ", i.Select(FindIdSql));
            throw new ArgumentException("No instruments");
        }

        public NEntityHelper(Func<IContainer> containerFunc) {
            var container = containerFunc();

            _cache = container.Resolve<INEntityCache>();
            var types =
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(IIdentifyable)))
                    .ToList();

            foreach (var type in types) {
                _cache.PrepareProperties(type);
                _cache.PrepareRules(type);

                var typeName = type.Name;
                var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

                var allFields = string.Join(", ", _cache.Properties[type].Select(p => p.DbName));
                var valueFields = string.Join(", ", _cache.Properties[type].Where(p => p.DbName != "id").Select(p => p.DbName));

                var queries = new Dictionary<Operations, string> {
                    {Operations.Create, string.Format("INSERT INTO {0}({1})", name, valueFields)},
                    {Operations.Read, string.Format("SELECT {1} FROM {0}", name, allFields)},
                    {Operations.Update, string.Format("UPDATE {0} SET ", name)},
                    {Operations.Delete, string.Format("DELETE FROM {0} ", name)}
                };

                _cache.Queries.Add(type, queries);
            }
        }

        public long ReadId(SQLiteDataReader reader) {
            return reader.Read() ? reader.GetInt32(0) : default(long);
        }

        public string AllFields<T>(bool qualified = false) where T : class, IIdentifyable {
            var type = typeof (T);
            return string.Join(", ", _cache.Properties[type].Select(p => qualified ? p.TableName + "." + p.DbName : p.DbName));
        }

        public T Read<T>(SQLiteDataReader reader) where T : class, IIdentifyable {
            var type = typeof(T);
            _cache.PrepareReaders(type);
            if (reader.Read()) 
                return _cache.Readers[type](reader) as T;
            return null;
        }
    }
}