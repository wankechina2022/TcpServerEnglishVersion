using System;
using System.Text;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Hexadecimal conversion helper - HEX display and parsing of sent / received data all go through this class.
    /// </summary>
    public static class HexHelper
    {
        /// <summary>
        /// Converts a byte array to hexadecimal text (formatted like "4F 4B 0D 0A").
        /// </summary>
        /// <param name="data">Byte array; returns an empty string when null or empty.</param>
        /// <returns>Hexadecimal text.</returns>
        public static string BytesToHex(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            return BytesToHex(data, data.Length);
        }

        /// <summary>
        /// Converts a byte array to hexadecimal text using an explicit effective length.
        /// </summary>
        /// <param name="data">Byte array.</param>
        /// <param name="length">Effective byte length.</param>
        /// <returns>Hexadecimal text.</returns>
        public static string BytesToHex(byte[] data, int length)
        {
            if (data == null || data.Length == 0 || length < 1)
            {
                return string.Empty;
            }

            int count = length > data.Length ? data.Length : length;
            StringBuilder sb = new StringBuilder(count * 3);

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(data[i].ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts hexadecimal text to a byte array - supports common notations such as
        /// "4F4B", "4F 4B" and "0x4F-0x4B".
        /// </summary>
        /// <param name="hex">Hexadecimal text.</param>
        /// <returns>Byte array; an empty array when it cannot be parsed.</returns>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return new byte[0];
            }

            try
            {
                // Strip the 0x prefix and separators such as spaces, hyphens and commas.
                string clean = hex.Replace("0x", string.Empty)
                                  .Replace("0X", string.Empty)
                                  .Replace(" ", string.Empty)
                                  .Replace("-", string.Empty)
                                  .Replace(",", string.Empty)
                                  .Replace("\r", string.Empty)
                                  .Replace("\n", string.Empty)
                                  .Replace("\t", string.Empty);

                if (clean.Length == 0)
                {
                    return new byte[0];
                }

                // Pad with a leading 0 on odd length to avoid an exception.
                if (clean.Length % 2 != 0)
                {
                    clean = "0" + clean;
                }

                byte[] result = new byte[clean.Length / 2];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
                }

                return result;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn("Failed to parse hexadecimal text:" + ex.Message);
                return new byte[0];
            }
        }

        /// <summary>
        /// Determines whether the text is a valid hexadecimal string (spaces and separators are allowed).
        /// </summary>
        /// <param name="hex">Text to validate.</param>
        /// <returns>true when valid.</returns>
        public static bool IsHexString(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            string clean = hex.Replace("0x", string.Empty)
                              .Replace("0X", string.Empty)
                              .Replace(" ", string.Empty)
                              .Replace("-", string.Empty)
                              .Replace(",", string.Empty)
                              .Replace("\r", string.Empty)
                              .Replace("\n", string.Empty)
                              .Replace("\t", string.Empty);

            if (clean.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < clean.Length; i++)
            {
                char c = clean[i];
                bool isHexChar = (c >= '0' && c <= '9')
                                 || (c >= 'A' && c <= 'F')
                                 || (c >= 'a' && c <= 'f');
                if (!isHexChar)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a byte array to readable text (HEX preferred, used for logging).
        /// </summary>
        /// <param name="data">Byte array.</param>
        /// <param name="length">Effective length.</param>
        /// <returns>Text formatted like "HEX(4) 4F 4B 0D 0A".</returns>
        public static string ToLogText(byte[] data, int length)
        {
            if (data == null || data.Length == 0 || length < 1)
            {
                return string.Empty;
            }

            int count = length > data.Length ? data.Length : length;
            return string.Format("HEX({0}) {1}", count, BytesToHex(data, count));
        }
    }
}
