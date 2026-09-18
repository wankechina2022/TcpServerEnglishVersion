using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Network helper - local address enumeration and port occupancy probing
    /// (convention: always run a network connectivity check before binding).
    /// </summary>
    public static class NetHelper
    {
        /// <summary>
        /// Gets the list of listening addresses available to the UI drop-down.
        /// Order: 127.0.0.1 -> 0.0.0.0 -> IPv4 addresses of each local NIC.
        /// </summary>
        /// <returns>Address list, never null.</returns>
        public static List<string> GetListenAddressList()
        {
            List<string> list = new List<string>();

            try
            {
                list.Add(AppConstants.DEFAULT_LISTEN_IP);
                list.Add(AppConstants.LISTEN_ALL_IP);

                List<string> localIps = GetLocalIPv4List();
                foreach (string ip in localIps)
                {
                    if (!list.Contains(ip))
                    {
                        list.Add(ip);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to enumerate local listen addresses:" + ex.Message, ex);
            }

            // Fallback: even if network enumeration throws, keep at least the loopback address selectable.
            if (list.Count == 0)
            {
                list.Add(AppConstants.DEFAULT_LISTEN_IP);
            }

            return list;
        }

        /// <summary>
        /// Gets the IPv4 addresses of all local NICs (loopback excluded).
        /// </summary>
        /// <returns>IPv4 address list, never null.</returns>
        public static List<string> GetLocalIPv4List()
        {
            List<string> list = new List<string>();

            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                if (interfaces == null || interfaces.Length == 0)
                {
                    return list;
                }

                foreach (NetworkInterface ni in interfaces)
                {
                    if (ni == null || ni.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    // Only consider Ethernet and wireless adapters; skip virtual tunnels.
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback
                        || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    IPInterfaceProperties props = ni.GetIPProperties();
                    if (props == null) { continue; }

                    foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
                    {
                        if (addr == null || addr.Address == null) { continue; }
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) { continue; }

                        string ip = addr.Address.ToString();
                        if (!list.Contains(ip))
                        {
                            list.Add(ip);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to enumerate local network adapter addresses:" + ex.Message, ex);
            }

            return list;
        }

        /// <summary>
        /// Probes whether a local TCP port can be listened on (i.e. whether it is already occupied).
        /// </summary>
        /// <param name="port">Port number to probe.</param>
        /// <returns>true when the port is available; false when occupied or the argument is invalid.</returns>
        public static bool IsPortAvailable(int port)
        {
            return IsPortAvailable(null, port);
        }

        /// <summary>
        /// Probes whether a TCP port on the specified listening address can be listened on (added 2026-09-14).
        /// Note: the original implementation always probed with 0.0.0.0, so when another program had bound
        ///       only one specific NIC address, it wrongly reported "port already in use". Probing with the
        ///       address actually intended for listening avoids that false positive.
        /// </summary>
        /// <param name="listenIp">Listening address; empty, 0.0.0.0 or malformed values are treated as IPAddress.Any.</param>
        /// <param name="port">Port number to probe.</param>
        /// <returns>true when the port is available; false when occupied or the argument is invalid.</returns>
        public static bool IsPortAvailable(string listenIp, int port)
        {
            if (!ValidationHelper.IsValidPort(port))
            {
                return false;
            }

            IPAddress address = IPAddress.Any;

            if (!string.IsNullOrWhiteSpace(listenIp))
            {
                IPAddress parsed;
                if (IPAddress.TryParse(listenIp.Trim(), out parsed))
                {
                    address = parsed;
                }
            }

            TcpListener listener = null;
            try
            {
                listener = new TcpListener(address, port);
                listener.Start();
                return true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    // The port genuinely is already occupied.
                    return false;
                }

                // Cases such as the specified address not existing on this machine (AddressNotAvailable):
                // re-check once against the wildcard address, so that "address unavailable" is not
                // misreported as "port occupied".
                if (!address.Equals(IPAddress.Any) && IsPortAvailable(null, port))
                {
                    return true;
                }

                LogHelper.Instance.Warn(string.Format("Socket exception while probing whether port {0} ({1}) is available: {2}",
                    port, address, ex.SocketErrorCode));
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Exception while probing whether port {0} is available: {1}", port, ex.Message));
                return false;
            }
            finally
            {
                // Convention: unmanaged resources must be released.
                if (listener != null)
                {
                    try { listener.Stop(); }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Probes all occupied ports within the specified range.
        /// </summary>
        /// <param name="startPort">Start port.</param>
        /// <param name="count">Number of ports.</param>
        /// <returns>List of occupied ports, never null.</returns>
        public static List<int> FindOccupiedPorts(int startPort, int count)
        {
            return FindOccupiedPorts(null, startPort, count);
        }

        /// <summary>
        /// Probes all occupied ports within the specified range, probing against a given listening address
        /// (added 2026-09-14).
        /// </summary>
        /// <param name="listenIp">Listening address; treated as IPAddress.Any when empty.</param>
        /// <param name="startPort">Start port.</param>
        /// <param name="count">Number of ports.</param>
        /// <returns>List of occupied ports, never null.</returns>
        public static List<int> FindOccupiedPorts(string listenIp, int startPort, int count)
        {
            List<int> occupied = new List<int>();

            if (count < 1 || startPort < AppConstants.MIN_PORT)
            {
                return occupied;
            }

            for (int i = 0; i < count; i++)
            {
                int port = startPort + i;
                if (port > AppConstants.MAX_PORT)
                {
                    break;
                }

                if (!IsPortAvailable(listenIp, port))
                {
                    occupied.Add(port);
                }
            }

            return occupied;
        }
    }
}
