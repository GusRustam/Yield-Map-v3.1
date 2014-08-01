using System;
using System.Data.SQLite;

namespace YieldMap.Transitive.Native.Reader {
    public static class SqliteReaderHelper {
        public static DateTime? GetNullableDateTime(this SQLiteDataReader reader, int position) {
            try {
                var res = reader.GetDateTime(position);
                return res != default (DateTime) ? res : new DateTime?();
            } catch (Exception) {
                return new DateTime?();
            }
        }

        public static long? GetNullableInt32(this SQLiteDataReader reader, int position) {
            try {
                var res = reader.GetInt32(position);
                return res != default (long) ? res : new long?();
            } catch (Exception) {
                return null;
            }
        }

        public static double? GetNullableDouble(this SQLiteDataReader reader, int position) {
            try {
                var res = reader.GetDouble(position);
                return Math.Abs(res - default(double)) > 10e-4 ? res : new double?();
            } catch (Exception) {
                return null;
            }
        }    
        
    }
}