using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YieldMap.Database.Access;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures {
    internal class BackupRestore : IBackupRestore {

        private readonly IDbConn _conn;
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Database.BackupRestore");

        public BackupRestore(IDbConn conn) {
            _conn = conn;
        }

        private class FieldDefition {
            public int Index { get; set; }
            public string Name { private get; set; }
            public string Type { get; set; }
            public bool NotNull { get; set; }

            public static string ToNames(IEnumerable<FieldDefition> fields) {
                const string delimiter = ", ";
                var res = fields.Aggregate("", (state, i) => state + "\"" + i.Name + "\"" + delimiter);
                return res.Substring(0, res.Length - delimiter.Length);
            }
        }

        private static IEnumerable<string> ListTables(DbConnection dbConnection) {
            var res = new List<string>();
            var query = dbConnection.CreateCommand();
            query.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND NOT INSTR(name, 'sqlite')";
            using (var r = query.ExecuteReader()) {
                while (r.Read()) 
                    res.Add(r.GetString(0));
            }
            return res;
        }

        public string Backup(bool useUnion = false) {
            Logger.Info("Backup()");
            var commands = new List<string>();
            using (var ctx = _conn.CreateContext()) {
                var dbConnection = ctx.Database.Connection;
                dbConnection.Open();
                var tables = ListTables(dbConnection);
                try {
                    foreach (var currentTable in tables) {
                        Logger.Debug(string.Format("Saving table {0}", currentTable));
                        try {
                            var table = GetFieldDefitions(dbConnection, currentTable);
                            var fieldNames = FieldDefition.ToNames(table);

                            const string delim = ", ";
                            var internalDelimiter = useUnion ? " UNION SELECT " : ", ";
                            var query = dbConnection.CreateCommand();
                            query.CommandText = "SELECT * FROM \"" + currentTable + "\"";

                            var values = "";
                            var any = false;
                            using (var reader = query.ExecuteReader()) {
                                var counter = 0;
                                while (reader.Read()) {
                                    any = true;
                                    if (++counter == 500) {
                                        commands.Add(RenderInsert(useUnion, values, internalDelimiter, currentTable,
                                            fieldNames));
                                        values = "";
                                        counter = 0;
                                    }

                                    var local = reader;
                                    var portion = table
                                        .Select(col => GetAsString(local, col))
                                        .Aggregate("", (current, fieldVal) => current + fieldVal + delim);

                                    portion = portion.Substring(0, portion.Length - delim.Length);
                                    values = useUnion
                                        ? values + portion + internalDelimiter
                                        : values + "(" + portion + ")" + internalDelimiter;
                                }
                            }
                            if (any) 
                                commands.Add(RenderInsert(useUnion, values, internalDelimiter, currentTable, fieldNames));
                            else
                                Logger.Debug(string.Format("Empty table {0}", currentTable));
                        } catch (Exception e) {
                            Logger.ErrorEx("Failure", e);
                            throw;
                        }
                    }
                } finally {
                    dbConnection.Close();
                }
            }

            var tempFile = Path.GetTempFileName();
            if (File.Exists(tempFile)) 
                File.AppendAllLines(tempFile, commands);
            else throw new Exception("Couldn't create temporary file");

            return tempFile;
        }

        private string RenderInsert(bool useUnion, string values, string delimiter, string currentTable, string fieldNames) {
            values = values.Substring(0, values.Length - delimiter.Length);
            var sql = string.Format("INSERT INTO \"{0}\"({1}) {2} {3}", currentTable, fieldNames, useUnion ? "" : "VALUES", values);
            Logger.Trace(sql);
            return sql;
        }

        private List<FieldDefition> GetFieldDefitions(DbConnection dbConnection, string tableName) {
            var table = new List<FieldDefition>();
            var query = dbConnection.CreateCommand();
            query.CommandText = "PRAGMA table_info(\"" + tableName + "\")";
            using (var r = query.ExecuteReader()) {
                var index = 0;
                while (r.Read()) {
                    // data: [0]: index, [1]: field name, [2]: field type, [3]: can be NULL, [4]: default
                    var name = r.GetString(1);
                    var type = r.GetString(2);
                    var canBeNull = r.GetInt16(3);
                    table.Add(new FieldDefition {
                        Index = index,
                        Name = name,
                        Type = type,
                        NotNull = canBeNull == 1
                    });
                    index++;
                }
            }
            return table;
        }

        private  string GetAsString(IDataRecord reader, FieldDefition def ) {
            var type = def.Type.ToUpper();
            var index = def.Index;

            var v = reader.GetValue(index);
            if (String.IsNullOrEmpty(v.ToString()))
                return def.NotNull ? "''" : "NULL";

            string res;
            if (type.StartsWith("INTEGER"))
                res = reader.GetInt32(index).ToString(CultureInfo.InvariantCulture);
            else if (type.Contains("BIGINT"))
                res = reader.GetInt64(index).ToString(CultureInfo.InvariantCulture);
            else if (type.Contains("FLOAT"))
                res = reader.GetDouble(index).ToString(CultureInfo.InvariantCulture);
            else if (type.Contains("CHAR") || type.Contains("TEXT"))
                res = reader.GetString(index).Replace("'", "''");
            else if (type.Contains("DATE") || type.Contains("TIME"))
                res = reader.GetDateTime(index).ToLocalTime().ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
            else if (type.Contains("BOOL") || type.Contains("BIT"))
                res = reader.GetInt16(index).ToString(CultureInfo.InvariantCulture);
            else throw new Exception(string.Format("Unknown type {0}", type));
            
            return string.Format("'{0}'", res);
        }

        public void Cleanup() {
            Logger.Info("Cleanup()");
            using (var ctx = _conn.CreateContext()) {
                var dbConnection = ctx.Database.Connection;
                try {
                    dbConnection.Open();
                    var tables = ListTables(dbConnection);
                    foreach (var table in tables) {
                        Logger.Debug(string.Format("Cleaning table {0}", table));
                        try {
                            var cmd = dbConnection.CreateCommand();
                            cmd.CommandText = "DELETE FROM \"" + table + "\"";
                            Logger.Trace(cmd.CommandText);
                            cmd.ExecuteNonQuery();
                        } catch (Exception e) {
                            Logger.ErrorEx("Failure", e);
                            throw;
                        }
                    }
                } finally {
                    dbConnection.Close();
                }
            }
        }

        public void Restore(string fileName) {
            Logger.Info(string.Format("Restore({0})", fileName));
            if (!File.Exists(fileName)) throw new Exception(string.Format("Backup file {0} not found", fileName));
            var commands = File.ReadAllLines(fileName);
            Cleanup();

            using (var ctx = _conn.CreateContext()) {
                var dbConnection = ctx.Database.Connection;
                try {
                    dbConnection.Open();
                    var tables = ListTables(dbConnection);
                    foreach (var table in tables) {
                        Logger.Debug(string.Format("Restoring table {0}", table));
                        try {
                            var tableName = table;
                            var sqls = commands.Where(command => Regex.IsMatch(command, "^INSERT INTO \"" + tableName + "\""));
                            foreach (var sql in sqls) {
                                if (string.IsNullOrEmpty(sql)) {
                                    Logger.Warn(string.Format("No insertions for table {0}", table));
                                    continue;
                                }
                                var cmd = dbConnection.CreateCommand();
                                cmd.CommandText = sql;
                                Logger.Trace(sql);
                                cmd.ExecuteNonQuery();
                            }
                        } catch (Exception e) {
                            Logger.ErrorEx("Failure", e);
                            throw;
                        }
                    }
                } finally {
                    dbConnection.Close();
                }
            }
        }
    }
}