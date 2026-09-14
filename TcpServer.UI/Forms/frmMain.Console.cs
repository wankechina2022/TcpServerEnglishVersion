using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TcpServer.BLL;
using TcpServer.Common.Helpers;
using TcpServer.Model;
using TcpServer.UI.Helpers;

namespace TcpServer.UI.Forms
{
    /// <summary>
    /// 主窗体（数据控制台部分）—— 客户端选择、收发数据展示与手动发送
    /// 端口管理与启停控制相关成员见 frmMain.cs
    /// </summary>
    public partial class frmMain
    {
        // ============================================================
        // 1. 私有字段
        // ============================================================

        /// <summary>当前界面选中的端口号</summary>
        private int _currentPort;

        /// <summary>当前界面选中的会话标识</summary>
        private string _currentSessionId;

        /// <summary>数据区最大保留行数</summary>
        private const int MAX_DATA_LINES = 2000;

        // ============================================================
        // 2. 事件处理
        // ============================================================

        /// <summary>
        /// 客户端下拉框变化
        /// </summary>
        private void cboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClientInfo selected = cboClient.SelectedItem as ClientInfo;
            _currentSessionId = selected == null ? string.Empty : selected.SessionId;
        }

        /// <summary>
        /// 发送数据给选中客户端
        /// </summary>
        private void btnSend_Click(object sender, EventArgs e)
        {
            SendData(false);
        }

        /// <summary>
        /// 广播数据给该端口全部客户端
        /// </summary>
        private void btnSendAll_Click(object sender, EventArgs e)
        {
            SendData(true);
        }

        /// <summary>
        /// 清空数据区
        /// </summary>
        private void btnClearData_Click(object sender, EventArgs e)
        {
            txtData.Clear();
        }

        // ============================================================
        // 3. 私有业务方法
        // ============================================================

        /// <summary>
        /// 发送数据 —— 说明：本工具为模拟服务端，发送仅回给调试对端，
        /// 属高频调试交互，故按规约"敏感操作确认"精神做了可操作性取舍，不逐次弹窗。
        /// </summary>
        /// <param name="broadcast">true 为广播给全部客户端</param>
        private void SendData(bool broadcast)
        {
            if (_currentPort <= 0)
            {
                MessageHelper.ShowWarning("请先在端口列表中选择一个端口！");
                return;
            }

            string text = txtSend.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageHelper.ShowWarning("请输入要发送的内容！");
                txtSend.Focus();
                return;
            }

            byte[] data;

            if (chkSendHex.Checked)
            {
                if (!HexHelper.IsHexString(text))
                {
                    MessageHelper.ShowWarning("HEX 发送内容格式不正确，请输入如 4F 4B 0D 0A 形式的十六进制！");
                    txtSend.Focus();
                    return;
                }

                data = HexHelper.HexToBytes(text);
            }
            else
            {
                data = _serverManager.ReplyEngine.TextToBytes(text);
            }

            if (data == null || data.Length == 0)
            {
                MessageHelper.ShowWarning("发送内容为空，请检查输入！");
                return;
            }

            // 2026-09-14 新增：勾选"追加 0x0D 0x0A"时，在最终字节流末尾补上回车换行。
            // 关键点：追加发生在"解析之后"，所以与输入解析方式无关 ——
            //   文本模式（未勾选 HEX 发送）与 HEX 模式（勾选 HEX 发送）都会生效。
            // 取舍：若 HEX 模式下已手写 0D 0A，这里仍会再补一次，不做判重，
            //       目的是让"勾了就一定加"的行为保持可预期，不做隐式猜测。
            if (chkSendCrlf.Checked)
            {
                data = AppendCrlf(data);
            }

            // 规约要求：提交类操作进行中禁用按钮，防止重复点击
            btnSend.Enabled = false;
            btnSendAll.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                string error;

                if (broadcast)
                {
                    int count = _serverManager.SendToAllClients(_currentPort, data, out error);

                    if (count > 0)
                    {
                        AppendSystemText(_currentPort,
                            string.Format("已广播给 {0} 个客户端，共 {1} 字节。", count, data.Length));
                    }
                    else
                    {
                        MessageHelper.ShowWarning("广播失败：" + error);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_currentSessionId))
                    {
                        MessageHelper.ShowWarning("请先选择要发送的客户端！");
                        return;
                    }

                    if (_serverManager.SendToClient(_currentPort, _currentSessionId, data, out error))
                    {
                        AppendSystemText(_currentPort, string.Format("已发送 {0} 字节。", data.Length));
                    }
                    else
                    {
                        MessageHelper.ShowWarning("发送失败：" + error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("发送数据异常：" + ex.Message, ex);
                MessageHelper.ShowError("发送失败，详情请查看日志！");
            }
            finally
            {
                btnSend.Enabled = true;
                btnSendAll.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 在字节流末尾追加 0x0D 0x0A（回车换行）
        /// 2026-09-14 新增：配合"追加 0x0D 0x0A"勾选框，供文本 / HEX 两种发送模式共用。
        /// </summary>
        /// <param name="data">原始字节流</param>
        /// <returns>追加 CRLF 后的新数组（不修改原数组）</returns>
        private static byte[] AppendCrlf(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return new byte[] { 0x0D, 0x0A };
            }

            byte[] result = new byte[data.Length + 2];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            result[data.Length] = 0x0D;
            result[data.Length + 1] = 0x0A;

            return result;
        }

        /// <summary>
        /// 重置数据控制台的选择状态
        /// </summary>
        private void ResetConsoleSelection()
        {
            _currentPort = 0;
            _currentSessionId = string.Empty;

            cboClient.Items.Clear();
            txtData.Clear();
        }

        /// <summary>
        /// 从表格选中行更新当前端口，并刷新客户端下拉
        /// </summary>
        private void UpdateCurrentPortFromGrid()
        {
            try
            {
                int port = PortGridHelper.GetSelectedPort(dgvPorts);
                if (port == _currentPort)
                {
                    return;
                }

                _currentPort = port;
                _currentSessionId = string.Empty;
                txtData.Clear();

                RefreshClientComboBox();
            }
            catch (Exception ex)
            {
                _logger.Warn("切换当前端口异常：" + ex.Message);
            }
        }

        /// <summary>
        /// 刷新客户端下拉框
        /// 2026-09-14 修改：原实现依赖 SelectedIndexChanged 事件把选中会话回写到 _currentSessionId，
        /// 但该方法在"客户端列表为空"或"端口未选中"时会提前 return，既不触发事件也不清空
        /// _currentSessionId，导致下拉框已空、内部仍指向已下线的会话，
        /// 此时手动发送会报"客户端已断开"，看起来像程序坏了。
        /// 现改为：每次重建后按实际选中项强制回写，不再依赖事件是否触发。
        /// </summary>
        private void RefreshClientComboBox()
        {
            try
            {
                // 记住重建前的选中会话，若其仍在线则继续保持选中
                string previousSessionId = _currentSessionId;

                cboClient.Items.Clear();

                // 先清空内部选中状态，重建结束后一律以实际选中项为准
                _currentSessionId = string.Empty;

                if (_currentPort <= 0 || _serverManager == null) { return; }

                List<ClientInfo> clients = _serverManager.GetClientList(_currentPort);
                if (clients == null || clients.Count == 0) { return; }

                foreach (ClientInfo client in clients)
                {
                    if (client == null) { continue; }
                    cboClient.Items.Add(client);
                }

                if (cboClient.Items.Count == 0) { return; }

                int index = -1;

                if (!string.IsNullOrWhiteSpace(previousSessionId))
                {
                    for (int i = 0; i < cboClient.Items.Count; i++)
                    {
                        ClientInfo item = cboClient.Items[i] as ClientInfo;
                        if (item != null && item.SessionId == previousSessionId)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                // 原选中会话已下线（或本来就没选）时，默认选中第一个
                if (index < 0) { index = 0; }

                cboClient.SelectedIndex = index;

                // 关键：不依赖 SelectedIndexChanged 是否被触发，直接按实际选中项回写内部状态
                ClientInfo selected = cboClient.SelectedItem as ClientInfo;
                _currentSessionId = selected == null ? string.Empty : selected.SessionId;
            }
            catch (Exception ex)
            {
                _logger.Warn("刷新客户端下拉框异常：" + ex.Message);
            }
        }

        // ============================================================
        // 4. 监听事件回调（跨线程，需切回 UI 线程）
        // ============================================================

        /// <summary>
        /// 客户端上下线
        /// </summary>
        private void OnClientChanged(object sender, ClientChangedEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate
            {
                RefreshClientComboBox();
                RefreshPortGrid();

                string tip = e.IsConnected ? "上线" : "下线";
                AppendSystemText(e.Port, string.Format("客户端 {0} 已{1}（当前在线 {2} 个）。",
                    e.RemoteEndPoint, tip, e.ClientCount));
            });
        }

        /// <summary>
        /// 收到数据
        /// </summary>
        private void OnDataReceived(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate { AppendPortData(e); });
        }

        /// <summary>
        /// 发出数据
        /// </summary>
        private void OnDataSent(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate { AppendPortData(e); });
        }

        // ============================================================
        // 5. 数据区输出
        // ============================================================

        /// <summary>
        /// 将收发数据追加到数据区
        /// </summary>
        /// <param name="e">数据事件参数</param>
        private void AppendPortData(PortDataEventArgs e)
        {
            if (e == null || e.Port != _currentPort) { return; }

            // 已选定客户端时，只显示该客户端的数据
            if (!string.IsNullOrWhiteSpace(_currentSessionId) && e.SessionId != _currentSessionId)
            {
                return;
            }

            string content;

            if (chkHexView.Checked)
            {
                content = HexHelper.BytesToHex(e.Data, e.Length);
            }
            else
            {
                content = _serverManager.ReplyEngine.BytesToText(e.Data, e.Length);
            }

            string direction = e.Direction == DataDirection.Received ? "<< 收到" : ">> 发送";
            string line = string.Format("{0:HH:mm:ss.fff} {1} [{2}] {3}",
                e.EventTime, direction, e.RemoteEndPoint, content);

            AppendText(line);
        }

        /// <summary>
        /// 追加系统提示文本
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="message">提示内容</param>
        private void AppendSystemText(int port, string message)
        {
            if (port != _currentPort || string.IsNullOrWhiteSpace(message)) { return; }

            AppendText(string.Format("{0:HH:mm:ss.fff} -- {1}", DateTime.Now, message));
        }

        /// <summary>
        /// 向数据区追加一行文本
        /// </summary>
        /// <param name="line">文本行</param>
        private void AppendText(string line)
        {
            UiHelper.AppendLine(txtData, line, MAX_DATA_LINES);
        }
    }
}
