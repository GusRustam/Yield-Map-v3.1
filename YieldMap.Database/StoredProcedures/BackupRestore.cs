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
    public static class BackupRestore {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("BackupRestore");

        private static readonly List<string> Tables = new List<string> {
            "LegType", 
            "InstrumentType",
            "Country",
            "Currency",
            "Industry",
            "Seniority",
            "Specimen",
            "Chain",
            "Isin",
            "RicToChain",
            "SubIndustry",
            "Ticker",
            
            "FieldGroup",
            "Field",
            "Feed",

            "RatingAgency",
            "RatingAgencyCode",
            "Rating",
            "RatingToInstrument",
            "RatingToLegalEntity",

            "LegalEntity",
            "Ric",
            "Leg",
            "Index",
            "Description",
            "Instrument",
        };

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

        public static string Backup() {
            Logger.Info("Backup()");
            var commands = new List<string>();
            using (var ctx = DbConn.CreateContext()) {
                for (var i = Tables.Count - 1; i >= 0; i--) {
                    var dbConnection = ctx.Database.Connection;
                    dbConnection.Open();
                    var currentTable = Tables[i];
                    try {
                        var table = GetFieldDefitions(dbConnection, currentTable);

                        const string delim = ", ";
                        const string union = " UNION SELECT ";
                        var query = dbConnection.CreateCommand();
                        query.CommandText = "SELECT * FROM \"" + currentTable + "\"";

                        var res = "";
                        var any = false;
                        using (var reader = query.ExecuteReader()) {
                            while (reader.Read()) {
                                any = true;
                                var local = reader;

                                var portion = table
                                    .Select(col => GetAsString(local, col))
                                    .Aggregate("", (current, fieldVal) => current + fieldVal + delim);

                                portion = portion.Substring(0, portion.Length - delim.Length);
                                res = res + portion + union;
                            }
                        }
                        if (any) {
                            res = res.Substring(0, res.Length - union.Length);
                            var names = FieldDefition.ToNames(table);
                            var sql = string.Format("INSERT INTO \"{0}\"({1}) SELECT {2}", currentTable, names, res);
                            commands.Add(sql);
                            Logger.Trace(sql);
                        } else
                            Logger.Debug(string.Format("Empty table {0}", currentTable));
                    } catch(Exception e) {
                        Logger.ErrorEx("Failure", e);
                        throw;
                    } finally {
                        dbConnection.Close();
                    }
                }
            }

            var tempFile = Path.GetTempFileName();
            if (File.Exists(tempFile)) 
                File.AppendAllLines(tempFile, commands);

            return tempFile;
        }

        private static List<FieldDefition> GetFieldDefitions(DbConnection dbConnection, string tableName) {
            var table = new List<FieldDefition>();
            var query = dbConnection.CreateCommand();
            query.CommandText = "PRAGMA table_info(\"" + tableName + "\")";
            using (var r = query.ExecuteReader()) {
                var index = 0;
                while (r.Read()) {
                    // data: [0]: index, 
                    //       [1]: field name
                    //       [2]: field type
                    //       [3]: can be NULL
                    //       [4]: default

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

        private static string GetAsString(IDataRecord reader, FieldDefition def ) {
            var type = def.Type.ToUpper();
            var index = def.Index;

            var v = reader.GetValue(index);
            if (String.IsNullOrEmpty(v.ToString()))
                return def.NotNull ? "''" : "NULL";
            

            string res;
            if (type.StartsWith("INTEGER"))
                res = reader.GetInt32(index).ToString(CultureInfo.InvariantCulture);
            else if (type.Contains("CHAR"))
                res = reader.GetString(index).Replace("'", "''");
            else if (type.Contains("DATE") || type.Contains("TIME"))
                res = reader.GetDateTime(index).ToLocalTime().ToString(CultureInfo.InvariantCulture);
            else if (type.Contains("BOOL") || type.Contains("BIT"))
                res = reader.GetInt16(index).ToString(CultureInfo.InvariantCulture);
            else throw new Exception(string.Format("Unknown type {0}", type));
            
            return string.Format("'{0}'", res);
        }

        public static void Cleanup() {
            Logger.Info("Cleanup()");
            using (var ctx = DbConn.CreateContext()) {
                var dbConnection = ctx.Database.Connection;
                try {
                    dbConnection.Open();
                    foreach (var table in Tables) {
                        try {
                            var cmd = dbConnection.CreateCommand();
                            cmd.CommandText = "DELETE FROM \"" + table + "\"";
                            Logger.Debug(cmd.CommandText);
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

        public static void Restore(string fileName) {
            Logger.Info(string.Format("Restore({0})", fileName));
            if (!File.Exists(fileName)) throw new Exception(string.Format("Backup file {0} not found", fileName));
            var commands = File.ReadAllLines(fileName);
            Cleanup();

            using (var ctx = DbConn.CreateContext()) {
                var dbConnection = ctx.Database.Connection;
                try {
                    dbConnection.Open();
                    foreach (var table in Tables) {
                        try {
                            var sql = commands.FirstOrDefault(command => Regex.IsMatch(command, "^INSERT INTO \"" + table + "\""));
                            if (string.IsNullOrEmpty(sql)) {
                                Logger.Warn(string.Format("No insertions for table {0}", table));
                                continue;
                            }
                            var cmd = dbConnection.CreateCommand();
                            cmd.CommandText = sql;
                            Logger.Trace(sql);
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
    }
}