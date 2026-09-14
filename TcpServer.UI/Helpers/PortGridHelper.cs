using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.UI.Helpers
{
    /// <summary>
    /// 端口表格帮助类 —— 收敛 DataGridView 的构建与刷新逻辑
    /// </summary>
    public static class PortGridHelper
    {
        /// <summary>端口列名</summary>
        public const string COL_PORT = "colPort";

        /// <summary>状态列名</summary>
        public const string COL_STATE = "colState";

        /// <summary>客户端数列名</summary>
        public const string COL_CLIENTS = "colClients";

        /// <summary>接收字节列名</summary>
        public const string COL_RECEIVED = "colReceived";

        /// <summary>发送字节列名</summary>
        public const string COL_SENT = "colSent";

        /// <summary>最后活动列名</summary>
        public const string COL_LAST_ACTIVE = "colLastActive";

        /// <summary>备注列名</summary>
        public const string COL_REMARK = "colRemark";

        /// <summary>
        /// 按端口清单重建表格
        /// </summary>
        /// <param name="dgv">目标表格，为 null 时直接返回</param>
        /// <param name="ports">端口清单，可为 null</param>
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
                SetCell(row, COL_STATE, cfg.Enabled ? "已停止" : "已停用");
                SetCell(row, COL_CLIENTS, 0);
                SetCell(row, COL_RECEIVED, "0 B");
                SetCell(row, COL_SENT, "0 B");
                SetCell(row, COL_LAST_ACTIVE, string.Empty);
                SetCell(row, COL_REMARK, cfg.Remark ?? string.Empty);

                row.DefaultCellStyle.ForeColor = cfg.Enabled ? Color.Black : Color.Gray;
            }
        }

        /// <summary>
        /// 刷新表格中的运行时数据
        /// </summary>
        /// <param name="dgv">目标表格，为 null 时直接返回</param>
        /// <param name="infos">运行时状态集合，可为 null</param>
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
        /// 获取指定行的端口号
        /// </summary>
        /// <param name="dgv">目标表格</param>
        /// <param name="rowIndex">行索引</param>
        /// <returns>端口号；无效时返回 0</returns>
        public static int GetRowPort(DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count)
            {
                return 0;
            }

            return GetCellInt(dgv.Rows[rowIndex], COL_PORT);
        }

        /// <summary>
        /// 获取当前选中行的端口号
        /// </summary>
        /// <param name="dgv">目标表格</param>
        /// <returns>端口号；未选中时返回 0</returns>
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
        /// 设置指定列的单元格值（列不存在时静默跳过）
        /// </summary>
        /// <param name="row">目标行</param>
        /// <param name="columnName">列名</param>
        /// <param name="value">单元格值</param>
        private static void SetCell(DataGridViewRow row, string columnName, object value)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName)) { return; }
            if (!row.DataGridView.Columns.Contains(columnName)) { return; }

            row.Cells[columnName].Value = value;
        }

        /// <summary>
        /// 读取指定列的单元格整数值
        /// </summary>
        /// <param name="row">目标行</param>
        /// <param name="columnName">列名</param>
        /// <returns>单元格整数值；列不存在时返回 0</returns>
        private static int GetCellInt(DataGridViewRow row, string columnName)
        {
            if (row == null || row.DataGridView == null) { return 0; }
            if (!row.DataGridView.Columns.Contains(columnName)) { return 0; }

            return row.Cells[columnName].Value.ToInt(0);
        }
    }
}
