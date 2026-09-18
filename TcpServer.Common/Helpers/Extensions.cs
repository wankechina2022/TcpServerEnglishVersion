using System;
using System.Collections.Generic;
using System.Data;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Common extension methods - data conversion and null protection
    /// (convention: return non-null collections so callers do not need null checks).
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts a DataTable to a List (provided by convention for future database integration).
        /// </summary>
        /// <typeparam name="T">Target entity type.</typeparam>
        /// <param name="dt">Data source; returns an empty collection when null.</param>
        /// <returns>Entity collection, never null.</returns>
        public static List<T> ToList<T>(this DataTable dt) where T : new()
        {
            List<T> list = new List<T>();

            if (dt == null || dt.Rows.Count == 0)
            {
                return list;
            }

            var properties = typeof(T).GetProperties();

            foreach (DataRow row in dt.Rows)
            {
                T obj = new T();
                foreach (var prop in properties)
                {
                    if (dt.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                    {
                        try
                        {
                            object value = Convert.ChangeType(row[prop.Name], prop.PropertyType);
                            prop.SetValue(obj, value, null);
                        }
                        catch (Exception)
                        {
                            // When a single field fails to convert, keep its default value
                            // instead of aborting the whole conversion.
                        }
                    }
                }
                list.Add(obj);
            }

            return list;
        }

        /// <summary>
        /// Returns an empty table when the DataTable is null.
        /// </summary>
        /// <param name="dt">Data source.</param>
        /// <returns>A non-null DataTable.</returns>
        public static DataTable SafeReturn(this DataTable dt)
        {
            return dt ?? new DataTable();
        }

        /// <summary>
        /// Truncates a string.
        /// </summary>
        /// <param name="value">Source string.</param>
        /// <param name="maxLength">Maximum length.</param>
        /// <returns>The truncated string.</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength < 1)
            {
                return value;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Converts an object to an integer (safe conversion).
        /// </summary>
        /// <param name="value">Source object.</param>
        /// <param name="defaultValue">Default value used when conversion fails.</param>
        /// <returns>Conversion result.</returns>
        public static int ToInt(this object value, int defaultValue = 0)
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            int result;
            return int.TryParse(value.ToString(), out result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts an object to a long integer (safe conversion).
        /// </summary>
        /// <param name="value">Source object.</param>
        /// <param name="defaultValue">Default value used when conversion fails.</param>
        /// <returns>Conversion result.</returns>
        public static long ToLong(this object value, long defaultValue = 0L)
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            long result;
            return long.TryParse(value.ToString(), out result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts an object to a decimal (safe conversion).
        /// </summary>
        /// <param name="value">Source object.</param>
        /// <param name="defaultValue">Default value used when conversion fails.</param>
        /// <returns>Conversion result.</returns>
        public static decimal ToDecimal(this object value, decimal defaultValue = 0m)
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            decimal result;
            return decimal.TryParse(value.ToString(), out result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts an object to a string (safe conversion).
        /// </summary>
        /// <param name="value">Source object.</param>
        /// <param name="defaultValue">Default value used when the source is null.</param>
        /// <returns>Conversion result.</returns>
        public static string ToSafeString(this object value, string defaultValue = "")
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue ?? string.Empty;
            }

            return value.ToString();
        }

        /// <summary>
        /// Null protection for collections - never null.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="list">Source collection.</param>
        /// <returns>A non-null collection.</returns>
        public static List<T> SafeReturn<T>(this List<T> list)
        {
            return list ?? new List<T>();
        }

        /// <summary>
        /// Formats a byte count as human-readable text (B / KB / MB).
        /// </summary>
        /// <param name="bytes">Byte count.</param>
        /// <returns>Human-readable text.</returns>
        public static string ToSizeText(this long bytes)
        {
            if (bytes < 1024L)
            {
                return bytes + " B";
            }

            if (bytes < 1024L * 1024L)
            {
                return (bytes / 1024.0).ToString("F1") + " KB";
            }

            return (bytes / 1024.0 / 1024.0).ToString("F2") + " MB";
        }
    }
}
