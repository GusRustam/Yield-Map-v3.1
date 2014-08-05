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

        public static bool? GetNullableBoolean(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetBoolean(position);
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

        public static float? GetNullableFloat(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetFloat(position);
            } catch (Exception) {
                return null;
            }
        }

        public static long? GetNullableInt32(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetInt32(position);
            } catch (Exception) {
                return null;
            }
        }

        public static int? GetNullableInt16(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetInt16(position);
            } catch (Exception) {
                return null;
            }
        }  
    }
}