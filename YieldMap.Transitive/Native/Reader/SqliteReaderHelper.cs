using System;
using System.Data.SQLite;

namespace YieldMap.Transitive.Native.Reader {
    public static class SqliteReaderHelper {
        public static DateTime? GetNullableDateTime(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                var res = reader.GetDateTime(position);
                return res != default (DateTime) ? res : new DateTime?();
            } catch (Exception) {
                return new DateTime?();
            }
        }

        public static string GetNullableString(this SQLiteDataReader reader, int position) {
            return reader.IsDBNull(position) ? string.Empty : reader.GetString(position);
        }

        public static long? GetNullableInt32(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position)) return null;
                var res = reader.GetInt32(position);
                return res != default (long) ? res : new long?();
            } catch (Exception) {
                return null;
            }
        }

        public static double? GetNullableDouble(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetDouble(position);
            } catch (Exception) {
                return null;
            }
        }    
        
    }
}