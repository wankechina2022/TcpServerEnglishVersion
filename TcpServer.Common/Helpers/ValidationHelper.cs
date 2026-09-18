using System;
using System.Net;
using System.Text.RegularExpressions;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Data validation helper - provides common input validation methods
    /// (convention: never trust external input).
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates whether a string is null or whitespace.
        /// </summary>
        /// <param name="value">String to validate.</param>
        /// <returns>true when null or whitespace.</returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Validates whether an integer falls within the specified range (bounds inclusive).
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>true when within range.</returns>
        public static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Validates whether a port number is legal (1 ~ 65535).
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>true when legal.</returns>
        public static bool IsValidPort(int port)
        {
            return IsInRange(port, AppConstants.MIN_PORT, AppConstants.MAX_PORT);
        }

        /// <summary>
        /// Validates whether a port count is legal (1 ~ 200).
        /// </summary>
        /// <param name="count">Port count.</param>
        /// <returns>true when legal.</returns>
        public static bool IsValidPortCount(int count)
        {
            return IsInRange(count, AppConstants.MIN_PORT_COUNT, AppConstants.MAX_PORT_COUNT);
        }

        /// <summary>
        /// Validates whether a string is a legal IP address (IPv4).
        /// </summary>
        /// <param name="ip">IP string to validate.</param>
        /// <returns>true when legal.</returns>
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
        /// Validates the length of a string.
        /// </summary>
        /// <param name="value">String to validate.</param>
        /// <param name="minLength">Minimum length.</param>
        /// <param name="maxLength">Maximum length.</param>
        /// <returns>true when the length is acceptable.</returns>
        public static bool IsLengthValid(string value, int minLength, int maxLength)
        {
            return value != null && value.Length >= minLength && value.Length <= maxLength;
        }

        /// <summary>
        /// Validates whether a string is a valid mobile phone number.
        /// </summary>
        /// <param name="phone">Phone number.</param>
        /// <returns>true when legal.</returns>
        public static bool IsValidPhone(string phone)
        {
            return !string.IsNullOrWhiteSpace(phone) && Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
        }

        /// <summary>
        /// Validates whether a string is a valid e-mail address.
        /// </summary>
        /// <param name="email">E-mail address.</param>
        /// <returns>true when legal.</returns>
        public static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Validates whether a port falls inside a port range - used to prevent duplicate ports in the configuration.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="startPort">Range start port.</param>
        /// <param name="count">Number of ports in the range.</param>
        /// <returns>true when inside the range.</returns>
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
