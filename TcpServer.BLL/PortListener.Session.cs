using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 端口监听服务（客户端会话回调与资源清理部分）
    /// 2026-09-14 拆分：PortListener 主体加入看门狗唤醒机制后再次逼近规约"单类不超 800 行"的红线，
    /// 按项目既有惯例（参见 frmMain.cs / frmMain.Console.cs / PortListener.Watchdog.cs）拆到本 partial 文件。
    /// 本文件职责：会话事件回调（收/发/关闭）、客户端集合增删与快照、监听器句柄清理。
    /// 端口启停见 PortListener.cs，看门狗与线程收尾见 PortListener.Watchdog.cs
    /// </summary>
    public partial class PortListener
    {
        #region 私有方法 —— 会话回调与资源清理

        /// <summary>
        /// 会话收到数据
        /// </summary>
        private void OnSessionDataReceived(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            // 2026-09-14 修改：改用原子累加与原子时间更新，多客户端并发时不丢统计
            Interlocked.Add(ref _totalBytesReceived, e.Length);
            UpdateLastActiveTime(e.EventTime);

            LogHelper.Instance.WritePortLog(Port,
                string.Format("RX [{0}] {1}", e.RemoteEndPoint, HexHelper.ToLogText(e.Data, e.Length)));

            EventHandler<PortDataEventArgs> handler = DataReceived;
            if (handler != null)
            {
                try { handler(this, e); }
                catch (Exception ex) { LogHelper.Instance.Error("数据接收事件处理异常：" + ex.Message, ex); }
            }
        }

        /// <summary>
        /// 会话发出数据
        /// </summary>
        private void OnSessionDataSent(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            // 2026-09-14 修改：同接收侧，改为原子累加与原子时间更新
            Interlocked.Add(ref _totalBytesSent, e.Length);
            UpdateLastActiveTime(e.EventTime);

            LogHelper.Instance.WritePortLog(Port,
                string.Format("TX [{0}] {1}", e.RemoteEndPoint, HexHelper.ToLogText(e.Data, e.Length)));

            EventHandler<PortDataEventArgs> handler = DataSent;
            if (handler != null)
            {
                try { handler(this, e); }
                catch (Exception ex) { LogHelper.Instance.Error("数据发送事件处理异常：" + ex.Message, ex); }
            }
        }

        /// <summary>
        /// 会话关闭
        /// </summary>
        private void OnSessionClosed(object sender, ClientSession session)
        {
            if (session == null) { return; }

            ClientInfo info = session.ToClientInfo();
            RemoveClient(session.SessionId);

            // 释放会话资源
            try { session.Dispose(); }
            catch (Exception) { }

            RaiseClientChanged(info, false);
        }

        /// <summary>
        /// 从字典移除会话
        /// </summary>
        /// <param name="sessionId">会话标识</param>
        private void RemoveClient(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) { return; }

            lock (_clientsLock)
            {
                if (_clients.ContainsKey(sessionId))
                {
                    _clients.Remove(sessionId);
                }
            }
        }

        /// <summary>
        /// 获取当前客户端快照（避免持锁期间调用外部方法）
        /// </summary>
        /// <returns>会话集合，永不为 null</returns>
        private List<ClientSession> GetClientSnapshot()
        {
            List<ClientSession> snapshot = new List<ClientSession>();

            lock (_clientsLock)
            {
                foreach (KeyValuePair<string, ClientSession> pair in _clients)
                {
                    snapshot.Add(pair.Value);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 关闭所有客户端连接
        /// </summary>
        private void CloseAllClients()
        {
            List<ClientSession> snapshot = GetClientSnapshot();

            foreach (ClientSession session in snapshot)
            {
                if (session == null) { continue; }
                try { session.Close(); }
                catch (Exception) { }
            }

            lock (_clientsLock)
            {
                _clients.Clear();
            }
        }

        /// <summary>
        /// 清理监听器资源
        /// </summary>
        private void CleanupListener()
        {
            if (_listener == null) { return; }

            try { _listener.Stop(); }
            catch (Exception) { }
            finally { _listener = null; }
        }

        #endregion
    }
}
