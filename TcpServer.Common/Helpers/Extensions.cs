using System;
using System.Collections.Generic;
using System.Data;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 常用扩展方法类 —— 数据转换与空值保护（规约：返回非空集合，避免调用方判空）
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 将 DataTable 转换为 List（规约提供，便于后续接入数据库）
        /// </summary>
        /// <typeparam name="T">目标实体类型</typeparam>
        /// <param name="dt">数据源，为 null 时返回空集合</param>
        /// <returns>实体集合，永不为 null</returns>
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
                            // 单个字段转换失败时保留默认值，不中断整体转换
                        }
                    }
                }
                list.Add(obj);
            }

            return list;
        }

        /// <summary>
        /// DataTable 为空时返回空表
        /// </summary>
        /// <param name="dt">数据源</param>
        /// <returns>非空的 DataTable</returns>
        public static DataTable SafeReturn(this DataTable dt)
        {
            return dt ?? new DataTable();
        }

        /// <summary>
        /// 字符串截断
        /// </summary>
        /// <param name="value">源字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>截断后的字符串</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength < 1)
            {
                return value;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 对象转整数（安全转换）
        /// </summary>
        /// <param name="value">源对象</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换结果</returns>
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
        /// 对象转长整数（安全转换）
        /// </summary>
        /// <param name="value">源对象</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换结果</returns>
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
        /// 对象转小数（安全转换）
        /// </summary>
        /// <param name="value">源对象</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换结果</returns>
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
        /// 对象转字符串（安全转换）
        /// </summary>
        /// <param name="value">源对象</param>
        /// <param name="defaultValue">空值时的默认值</param>
        /// <returns>转换结果</returns>
        public static string ToSafeString(this object value, string defaultValue = "")
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue ?? string.Empty;
            }

            return value.ToString();
        }

        /// <summary>
        /// 集合空值保护 —— 永不为 null
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">源集合</param>
        /// <returns>非空集合</returns>
        public static List<T> SafeReturn<T>(this List<T> list)
        {
            return list ?? new List<T>();
        }

        /// <summary>
        /// 字节数格式化为易读文本（B / KB / MB）
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>易读文本</returns>
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
