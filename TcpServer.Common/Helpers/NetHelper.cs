using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 网络帮助类 —— 本机地址枚举、端口占用探测（规约：写入前先做网络连通性检查）
    /// </summary>
    public static class NetHelper
    {
        /// <summary>
        /// 获取界面下拉框可选的监听地址列表
        /// 顺序：127.0.0.1 → 0.0.0.0 → 本机各网卡 IPv4 地址
        /// </summary>
        /// <returns>地址清单，永不为 null</returns>
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

            // 兜底：即使网络枚举异常，也保证至少有回环地址可选
            if (list.Count == 0)
            {
                list.Add(AppConstants.DEFAULT_LISTEN_IP);
            }

            return list;
        }

        /// <summary>
        /// 获取本机所有网卡的 IPv4 地址（不含回环地址）
        /// </summary>
        /// <returns>IPv4 地址清单，永不为 null</returns>
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

                    // 仅统计以太网与无线网卡，跳过虚拟隧道
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
        /// 探测本机某个 TCP 端口是否可被监听（是否已被占用）
        /// </summary>
        /// <param name="port">待探测端口号</param>
        /// <returns>可监听返回 true；被占用或参数非法返回 false</returns>
        public static bool IsPortAvailable(int port)
        {
            return IsPortAvailable(null, port);
        }

        /// <summary>
        /// 探测指定监听地址上的某个 TCP 端口是否可被监听（2026-09-14 新增）
        /// 说明：原实现固定用 0.0.0.0 探测，当别的程序只绑定了某一块网卡的地址时，
        ///       会误报"端口已被占用"。改为按实际要监听的地址探测，避免误判。
        /// </summary>
        /// <param name="listenIp">监听地址；为空、0.0.0.0 或格式非法时按 IPAddress.Any 处理</param>
        /// <param name="port">待探测端口号</param>
        /// <returns>可监听返回 true；被占用或参数非法返回 false</returns>
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
                    // 端口确实已被占用
                    return false;
                }

                // 指定地址在本机不存在（AddressNotAvailable）等情形：
                // 退回通配地址复核一次，避免把"地址不可用"误报成"端口被占用"
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
                // 规约要求：非托管资源必须释放
                if (listener != null)
                {
                    try { listener.Stop(); }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// 在指定区间内探测出所有已被占用的端口
        /// </summary>
        /// <param name="startPort">起始端口</param>
        /// <param name="count">端口数量</param>
        /// <returns>已被占用的端口清单，永不为 null</returns>
        public static List<int> FindOccupiedPorts(int startPort, int count)
        {
            return FindOccupiedPorts(null, startPort, count);
        }

        /// <summary>
        /// 在指定区间内探测出所有已被占用的端口（按指定监听地址探测，2026-09-14 新增）
        /// </summary>
        /// <param name="listenIp">监听地址；为空时按 IPAddress.Any 处理</param>
        /// <param name="startPort">起始端口</param>
        /// <param name="count">端口数量</param>
        /// <returns>已被占用的端口清单，永不为 null</returns>
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
