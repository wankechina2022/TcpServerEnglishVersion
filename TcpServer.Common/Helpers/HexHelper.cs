using System;
using System.Text;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 十六进制转换帮助类 —— 收发数据的 HEX 显示与解析统一走此类
    /// </summary>
    public static class HexHelper
    {
        /// <summary>
        /// 字节数组转十六进制文本（形如 "4F 4B 0D 0A"）
        /// </summary>
        /// <param name="data">字节数组，为 null 或空时返回空串</param>
        /// <returns>十六进制文本</returns>
        public static string BytesToHex(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            return BytesToHex(data, data.Length);
        }

        /// <summary>
        /// 字节数组转十六进制文本（指定有效长度）
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="length">有效字节长度</param>
        /// <returns>十六进制文本</returns>
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
        /// 十六进制文本转字节数组 —— 支持 "4F4B"、"4F 4B"、"0x4F-0x4B" 等常见写法
        /// </summary>
        /// <param name="hex">十六进制文本</param>
        /// <returns>字节数组；无法解析时返回空数组</returns>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return new byte[0];
            }

            try
            {
                // 去掉 0x 前缀、空格、连字符、逗号等分隔符
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

                // 奇数长度时前面补 0，避免异常
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
        /// 判断文本是否为合法的十六进制串（允许空格与分隔符）
        /// </summary>
        /// <param name="hex">待校验文本</param>
        /// <returns>合法返回 true</returns>
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
        /// 字节数组转可读文本（HEX 优先，用于日志）
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="length">有效长度</param>
        /// <returns>形如 "HEX(4) 4F 4B 0D 0A" 的文本</returns>
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
