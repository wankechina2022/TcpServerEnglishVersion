using System;
using Newtonsoft.Json;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// JSON serialization helper - wraps Newtonsoft.Json consistently; every method has exception fallback.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Serialization settings - ignore null values and ignore missing members,
        /// so old configuration files can still be read by newer versions.
        /// </summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DateFormatString = "yyyy-MM-dd HH:mm:ss"
        };

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">Object to serialize; returns an empty string when null.</param>
        /// <param name="indented">Whether to format with indentation (easier for humans to read the config file).</param>
        /// <returns>JSON string; an empty string on failure.</returns>
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
                LogHelper.Instance.Error("Failed to serialize object to JSON:" + ex.Message, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Deserializes a JSON string into the specified type.
        /// </summary>
        /// <typeparam name="T">Target type (must be a reference type).</typeparam>
        /// <param name="json">JSON string.</param>
        /// <param name="defaultValue">Default value returned when parsing fails; may be null.</param>
        /// <returns>Deserialization result; returns defaultValue on failure.</returns>
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
        /// Attempts to deserialize a JSON string into the specified type.
        /// </summary>
        /// <typeparam name="T">Target type (must be a reference type).</typeparam>
        /// <param name="json">JSON string.</param>
        /// <param name="result">Deserialization result.</param>
        /// <returns>true on success; false on failure with result set to null.</returns>
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
                LogHelper.Instance.Error("Failed to deserialize JSON:" + ex.Message, ex);
                result = null;
                return false;
            }
        }
    }
}
