using System;
using System.Net;
using System.Text.RegularExpressions;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 数据校验帮助类 —— 提供常用输入校验方法（规约：永远不要相信外部输入）
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 校验字符串是否为空或空白
        /// </summary>
        /// <param name="value">待校验字符串</param>
        /// <returns>为空或空白返回 true</returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 校验整数是否在指定范围内（含边界）
        /// </summary>
        /// <param name="value">待校验数值</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>在范围内返回 true</returns>
        public static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// 校验端口号是否合法（1 ~ 65535）
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>合法返回 true</returns>
        public static bool IsValidPort(int port)
        {
            return IsInRange(port, AppConstants.MIN_PORT, AppConstants.MAX_PORT);
        }

        /// <summary>
        /// 校验端口数量是否合法（1 ~ 200）
        /// </summary>
        /// <param name="count">端口数量</param>
        /// <returns>合法返回 true</returns>
        public static bool IsValidPortCount(int count)
        {
            return IsInRange(count, AppConstants.MIN_PORT_COUNT, AppConstants.MAX_PORT_COUNT);
        }

        /// <summary>
        /// 校验是否为合法的 IP 地址（IPv4）
        /// </summary>
        /// <param name="ip">待校验的 IP 字符串</param>
        /// <returns>合法返回 true</returns>
        public static bool IsValidIpAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            IPAddress address;
            return IPAddress.TryParse(ip.Trim(), out address);
        }

        /// <summary>
        /// 校验字符串长度
        /// </summary>
        /// <param name="value">待校验字符串</param>
        /// <param name="minLength">最小长度</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>长度合规返回 true</returns>
        public static bool IsLengthValid(string value, int minLength, int maxLength)
        {
            return value != null && value.Length >= minLength && value.Length <= maxLength;
        }

        /// <summary>
        /// 校验是否为有效的手机号
        /// </summary>
        /// <param name="phone">手机号</param>
        /// <returns>合法返回 true</returns>
        public static bool IsValidPhone(string phone)
        {
            return !string.IsNullOrWhiteSpace(phone) && Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
        }

        /// <summary>
        /// 校验是否为有效的邮箱
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <returns>合法返回 true</returns>
        public static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// 校验两个端口区间是否存在重叠 —— 用于防止配置里出现重复端口
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="startPort">区间起始端口</param>
        /// <param name="count">区间端口数量</param>
        /// <returns>落在区间内返回 true</returns>
        public static bool IsPortInRange(int port, int startPort, int count)
        {
            if (count < 1)
            {
                return false;
            }

            return port >= startPort && port <= startPort + count - 1;
        }
    }
}
