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
    /// Main window (data console part) - client selection, send / receive data display and manual sending.
    /// Port management and start / stop control members live in frmMain.cs.
    /// </summary>
    public partial class frmMain
    {
        // ============================================================
        // 1. Private fields
        // ============================================================

        /// <summary>Port number currently selected in the UI.</summary>
        private int _currentPort;

        /// <summary>Session identifier currently selected in the UI.</summary>
        private string _currentSessionId;

        /// <summary>Maximum number of lines retained in the data area.</summary>
        private const int MAX_DATA_LINES = 2000;

        // ============================================================
        // 2. Event handlers
        // ============================================================

        /// <summary>
        /// Client drop-down selection changed.
        /// </summary>
        private void cboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClientInfo selected = cboClient.SelectedItem as ClientInfo;
            _currentSessionId = selected == null ? string.Empty : selected.SessionId;
        }

        /// <summary>
        /// Sends data to the selected client.
        /// </summary>
        private void btnSend_Click(object sender, EventArgs e)
        {
            SendData(false);
        }

        /// <summary>
        /// Broadcasts data to all clients on this port.
        /// </summary>
        private void btnSendAll_Click(object sender, EventArgs e)
        {
            SendData(true);
        }

        /// <summary>
        /// Clears the data area.
        /// </summary>
        private void btnClearData_Click(object sender, EventArgs e)
        {
            txtData.Clear();
        }

        // ============================================================
        // 3. Private business methods
        // ============================================================

        /// <summary>
        /// Sends data - note: this tool emulates a server, so sending only replies to the debugging peer.
        /// That is high-frequency debugging interaction, so following the spirit of the convention's
        /// "confirm sensitive operations" a usability trade-off is made and no dialog is shown each time.
        /// </summary>
        /// <param name="broadcast">true to broadcast to all clients.</param>
        private void SendData(bool broadcast)
        {
            if (_currentPort <= 0)
            {
                MessageHelper.ShowWarning("Please select a port in the port list first!");
                return;
            }

            string text = txtSend.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageHelper.ShowWarning("Please enter the content to send!");
                txtSend.Focus();
                return;
            }

            byte[] data;

            if (chkSendHex.Checked)
            {
                if (!HexHelper.IsHexString(text))
                {
                    MessageHelper.ShowWarning("Invalid HEX format. Please enter hexadecimal such as 4F 4B 0D 0A!");
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
                MessageHelper.ShowWarning("Nothing to send. Please check your input!");
                return;
            }

            // Added 2026-09-14: when "Append 0x0D 0x0A" is checked, append CR / LF at the end of the final byte stream.
            // Key point: the append happens *after* parsing, so it is independent of how the input was parsed -
            //   both text mode (Send as HEX unchecked) and HEX mode (Send as HEX checked) are affected.
            // Trade-off: if 0D 0A was already written by hand in HEX mode, it is appended once more here with no
            //            duplicate check, so that "checking it always appends" stays predictable rather than
            //            relying on implicit guessing.
            if (chkSendCrlf.Checked)
            {
                data = AppendCrlf(data);
            }

            // Convention: disable buttons during a submitting operation to prevent double clicks.
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
                            string.Format("Broadcast to {0} client(s), {1} byte(s) in total.", count, data.Length));
                    }
                    else
                    {
                        MessageHelper.ShowWarning("Broadcast failed:" + error);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_currentSessionId))
                    {
                        MessageHelper.ShowWarning("Please select a client to send to first!");
                        return;
                    }

                    if (_serverManager.SendToClient(_currentPort, _currentSessionId, data, out error))
                    {
                        AppendSystemText(_currentPort, string.Format("Sent {0} byte(s).", data.Length));
                    }
                    else
                    {
                        MessageHelper.ShowWarning("Send failed:" + error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while sending data:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to send. See the log for details!");
            }
            finally
            {
                btnSend.Enabled = true;
                btnSendAll.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Appends 0x0D 0x0A (carriage return / line feed) to the end of the byte stream.
        /// Added 2026-09-14: works together with the "Append 0x0D 0x0A" check box and is shared by both the
        /// text and the HEX send modes.
        /// </summary>
        /// <param name="data">Original byte stream.</param>
        /// <returns>A new array with CRLF appended (the original array is not modified).</returns>
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
        /// Resets the selection state of the data console.
        /// </summary>
        private void ResetConsoleSelection()
        {
            _currentPort = 0;
            _currentSessionId = string.Empty;

            cboClient.Items.Clear();
            txtData.Clear();
        }

        /// <summary>
        /// Updates the current port from the selected grid row and refreshes the client drop-down.
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
                _logger.Warn("Exception while switching the current port:" + ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the client drop-down.
        /// 2026-09-14 change: the original implementation relied on the SelectedIndexChanged event to write the
        /// selected session back into _currentSessionId, but that method returns early when "the client list is
        /// empty" or "no port is selected", so neither the event fired nor _currentSessionId was cleared. The
        /// drop-down could be empty while the internals still pointed at an offline session, and a manual send
        /// then reported "client disconnected", looking like the program was broken.
        /// It now writes back forcibly according to the actual selection after every rebuild, no longer relying
        /// on whether the event fires.
        /// </summary>
        private void RefreshClientComboBox()
        {
            try
            {
                // Remember the session selected before the rebuild; keep it selected if still online.
                string previousSessionId = _currentSessionId;

                cboClient.Items.Clear();

                // Clear the internal selection first; after the rebuild the actual selection is authoritative.
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

                // When the previously selected session went offline (or nothing was selected), select the first.
                if (index < 0) { index = 0; }

                cboClient.SelectedIndex = index;

                // Key point: do not rely on whether SelectedIndexChanged fired; write the internal state back
                // directly from the actual selection.
                ClientInfo selected = cboClient.SelectedItem as ClientInfo;
                _currentSessionId = selected == null ? string.Empty : selected.SessionId;
            }
            catch (Exception ex)
            {
                _logger.Warn("Exception while refreshing the client list:" + ex.Message);
            }
        }

        // ============================================================
        // 4. Listener event callbacks (cross-thread; must switch back to the UI thread)
        // ============================================================

        /// <summary>
        /// Client online / offline.
        /// </summary>
        private void OnClientChanged(object sender, ClientChangedEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate
            {
                RefreshClientComboBox();
                RefreshPortGrid();

                string tip = e.IsConnected ? "online" : "offline";
                AppendSystemText(e.Port, string.Format("Client {0} is now {1} ({2} online).",
                    e.RemoteEndPoint, tip, e.ClientCount));
            });
        }

        /// <summary>
        /// Data received.
        /// </summary>
        private void OnDataReceived(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate { AppendPortData(e); });
        }

        /// <summary>
        /// Data sent.
        /// </summary>
        private void OnDataSent(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate { AppendPortData(e); });
        }

        // ============================================================
        // 5. Data area output
        // ============================================================

        /// <summary>
        /// Appends sent / received data to the data area.
        /// </summary>
        /// <param name="e">Data event arguments.</param>
        private void AppendPortData(PortDataEventArgs e)
        {
            if (e == null || e.Port != _currentPort) { return; }

            // When a client has been selected, show only that client's data.
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

            string direction = e.Direction == DataDirection.Received ? "<< Received" : ">> Sent";
            string line = string.Format("{0:HH:mm:ss.fff} {1} [{2}] {3}",
                e.EventTime, direction, e.RemoteEndPoint, content);

            AppendText(line);
        }

        /// <summary>
        /// Appends a system notice line.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="message">Notice content.</param>
        private void AppendSystemText(int port, string message)
        {
            if (port != _currentPort || string.IsNullOrWhiteSpace(message)) { return; }

            AppendText(string.Format("{0:HH:mm:ss.fff} -- {1}", DateTime.Now, message));
        }

        /// <summary>
        /// Appends one line of text to the data area.
        /// </summary>
        /// <param name="line">Text line.</param>
        private void AppendText(string line)
        {
            UiHelper.AppendLine(txtData, line, MAX_DATA_LINES);
        }
    }
}
