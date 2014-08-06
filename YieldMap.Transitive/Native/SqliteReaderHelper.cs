using System;
using System.Data.SQLite;
using System.Linq.Expressions;

namespace YieldMap.Transitive.Native {
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

        public static Expression GetCall(Type propertyType, ParameterExpression reader, int i) {
            var sqliteReaderType = typeof(SQLiteDataReader);
            var helperType = typeof(SqliteReaderHelper);

            var iExp = Expression.Constant(i);

            if (propertyType == typeof(bool))
                return Expression.Call(reader, sqliteReaderType.GetMethod("GetBoolean"), new Expression[] { iExp });

            if (propertyType == typeof(bool?))
                return Expression.Call(helperType.GetMethod("GetNullableBoolean"), new Expression[] { reader, iExp });

            if (propertyType == typeof(float))
                return Expression.Call(reader, sqliteReaderType.GetMethod("GetGloat"), new Expression[] { iExp });

            if (propertyType == typeof(float?))
                return Expression.Call(helperType.GetMethod("GetNullableFloat"), new Expression[] { reader, iExp });

            if (propertyType == typeof(double))
                return Expression.Call(reader, sqliteReaderType.GetMethod("GetDouble"), new Expression[] { iExp });

            if (propertyType == typeof(double?))
                return Expression.Call(helperType.GetMethod("GetNullableDouble"), new Expression[] { reader, iExp });

            if (propertyType == typeof(int))
                return Expression.Call(reader, sqliteReaderType.GetMethod("GetInt32"), new Expression[] { iExp });

            if (propertyType == typeof(int?))
                return Expression.Call(helperType.GetMethod("GetNullableInt32"), new Expression[] { reader, iExp });

            if (propertyType == typeof(long))
                return Expression.Call(reader, sqliteReaderType.GetMethod("GetInt64"), new Expression[] { iExp });

            if (propertyType == typeof(long?))
                return Expression.Call(helperType.GetMethod("GetNullableInt64"), new Expression[] { reader, iExp });

            if (propertyType == typeof(DateTime))
                return Expression.Call(reader, sqliteReaderType.GetMethod("GetDateTime"), new Expression[] { iExp });

            if (propertyType == typeof(DateTime?))
                return Expression.Call(helperType.GetMethod("GetNullableDateTime"), new Expression[] { reader, iExp });

            if (propertyType == typeof(string))
                return Expression.Call(helperType.GetMethod("GetNullableString"), new Expression[] { reader, iExp });

            return null;
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

        public static long? GetNullableInt64(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetInt64(position);
            } catch (Exception) {
                return null;
            }
        }

        public static int? GetNullableInt32(this SQLiteDataReader reader, int position) {
            try {
                if (reader.IsDBNull(position))
                    return null;
                return reader.GetInt32(position);
            } catch (Exception) {
                return null;
            }
        }


        public static short? GetNullableInt16(this SQLiteDataReader reader, int position) {
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