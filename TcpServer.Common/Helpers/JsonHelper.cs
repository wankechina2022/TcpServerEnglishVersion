using System;
using Newtonsoft.Json;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// JSON 序列化帮助类 —— 统一封装 Newtonsoft.Json，所有方法均做异常兜底
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 序列化设置 —— 忽略空值、忽略缺失成员，保证旧配置可被新版本读取
        /// </summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DateFormatString = "yyyy-MM-dd HH:mm:ss"
        };

        /// <summary>
        /// 将对象序列化为 JSON 字符串
        /// </summary>
        /// <param name="obj">待序列化对象，为 null 时返回空串</param>
        /// <param name="indented">是否格式化缩进（便于人工查看配置文件）</param>
        /// <returns>JSON 字符串；失败时返回空串</returns>
        public static string Serialize(object obj, bool indented = true)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            try
            {
                return JsonConvert.SerializeObject(
                    obj,
                    indented ? Formatting.Indented : Formatting.None,
                    SerializerSettings);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("对象序列化为 JSON 失败：" + ex.Message, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// 将 JSON 字符串反序列化为指定类型
        /// </summary>
        /// <typeparam name="T">目标类型（必须是引用类型）</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <param name="defaultValue">解析失败时返回的默认值，可为 null</param>
        /// <returns>反序列化结果；失败时返回 defaultValue</returns>
        public static T Deserialize<T>(string json, T defaultValue = null) where T : class
        {
            T result;
            if (TryDeserialize(json, out result))
            {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// 尝试将 JSON 字符串反序列化为指定类型
        /// </summary>
        /// <typeparam name="T">目标类型（必须是引用类型）</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <param name="result">反序列化结果</param>
        /// <returns>成功返回 true；失败返回 false 并输出 null</returns>
        public static bool TryDeserialize<T>(string json, out T result) where T : class
        {
            result = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(json, SerializerSettings);
                return result != null;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("JSON 反序列化失败：" + ex.Message, ex);
                result = null;
                return false;
            }
        }
    }
}
