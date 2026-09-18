using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.UI.Helpers
{
    /// <summary>
    /// Port grid helper - consolidates the DataGridView build and refresh logic.
    /// </summary>
    public static class PortGridHelper
    {
        /// <summary>Port column name.</summary>
        public const string COL_PORT = "colPort";

        /// <summary>State column name.</summary>
        public const string COL_STATE = "colState";

        /// <summary>Client count column name.</summary>
        public const string COL_CLIENTS = "colClients";

        /// <summary>Bytes received column name.</summary>
        public const string COL_RECEIVED = "colReceived";

        /// <summary>Bytes sent column name.</summary>
        public const string COL_SENT = "colSent";

        /// <summary>Last activity column name.</summary>
        public const string COL_LAST_ACTIVE = "colLastActive";

        /// <summary>Remark column name.</summary>
        public const string COL_REMARK = "colRemark";

        /// <summary>
        /// Rebuilds the grid from the port list.
        /// </summary>
        /// <param name="dgv">Target grid; returns immediately when null.</param>
        /// <param name="ports">Port list; may be null.</param>
        public static void Rebuild(DataGridView dgv, List<PortConfig> ports)
        {
            if (dgv == null) { return; }

            dgv.Rows.Clear();

            if (ports == null || ports.Count == 0)
            {
                return;
            }

            foreach (PortConfig cfg in ports)
            {
                if (cfg == null) { continue; }

                int index = dgv.Rows.Add();
                DataGridViewRow row = dgv.Rows[index];

                SetCell(row, COL_PORT, cfg.Port);
                SetCell(row, COL_STATE, cfg.Enabled ? "Stopped" : "Disabled");
                SetCell(row, COL_CLIENTS, 0);
                SetCell(row, COL_RECEIVED, "0 B");
                SetCell(row, COL_SENT, "0 B");
                SetCell(row, COL_LAST_ACTIVE, string.Empty);
                SetCell(row, COL_REMARK, cfg.Remark ?? string.Empty);

                row.DefaultCellStyle.ForeColor = cfg.Enabled ? Color.Black : Color.Gray;
            }
        }

        /// <summary>
        /// Refreshes the runtime data in the grid.
        /// </summary>
        /// <param name="dgv">Target grid; returns immediately when null.</param>
        /// <param name="infos">Runtime state collection; may be null.</param>
        public static void Refresh(DataGridView dgv, List<PortRuntimeInfo> infos)
        {
            if (dgv == null || dgv.Rows.Count == 0) { return; }
            if (infos == null || infos.Count == 0) { return; }

            Dictionary<int, PortRuntimeInfo> map = new Dictionary<int, PortRuntimeInfo>();
            foreach (PortRuntimeInfo info in infos)
            {
                if (info == null || map.ContainsKey(info.Port)) { continue; }
                map[info.Port] = info;
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {
                int port = GetCellInt(row, COL_PORT);
                if (port <= 0) { continue; }

                PortRuntimeInfo info;
                if (!map.TryGetValue(port, out info) || info == null) { continue; }

                SetCell(row, COL_STATE, info.StateText);
                SetCell(row, COL_CLIENTS, info.ClientCount);
                SetCell(row, COL_RECEIVED, info.BytesReceived.ToSizeText());
                SetCell(row, COL_SENT, info.BytesSent.ToSizeText());
                SetCell(row, COL_LAST_ACTIVE, info.LastActiveTime == DateTime.MinValue
                    ? string.Empty
                    : info.LastActiveTime.ToString("MM-dd HH:mm:ss"));

                row.DefaultCellStyle.ForeColor = info.Enabled ? Color.Black : Color.Gray;
            }
        }

        /// <summary>
        /// Gets the port number of the specified row.
        /// </summary>
        /// <param name="dgv">Target grid.</param>
        /// <param name="rowIndex">Row index.</param>
        /// <returns>Port number; 0 when invalid.</returns>
        public static int GetRowPort(DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count)
            {
                return 0;
            }

            return GetCellInt(dgv.Rows[rowIndex], COL_PORT);
        }

        /// <summary>
        /// Gets the port number of the currently selected row.
        /// </summary>
        /// <param name="dgv">Target grid.</param>
        /// <returns>Port number; 0 when nothing is selected.</returns>
        public static int GetSelectedPort(DataGridView dgv)
        {
            if (dgv == null || dgv.SelectedRows.Count == 0)
            {
                return 0;
            }

            int port = GetCellInt(dgv.SelectedRows[0], COL_PORT);
            return ValidationHelper.IsValidPort(port) ? port : 0;
        }

        /// <summary>
        /// Sets the cell value of the specified column (silently skipped when the column does not exist).
        /// </summary>
        /// <param name="row">Target row.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="value">Cell value.</param>
        private static void SetCell(DataGridViewRow row, string columnName, object value)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName)) { return; }
            if (!row.DataGridView.Columns.Contains(columnName)) { return; }

            row.Cells[columnName].Value = value;
        }

        /// <summary>
        /// Reads the integer cell value of the specified column.
        /// </summary>
        /// <param name="row">Target row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Integer cell value; 0 when the column does not exist.</returns>
        private static int GetCellInt(DataGridViewRow row, string columnName)
        {
            if (row == null || row.DataGridView == null) { return 0; }
            if (!row.DataGridView.Columns.Contains(columnName)) { return 0; }

            return row.Cells[columnName].Value.ToInt(0);
        }
    }
}
