using System;

namespace TcpServer.Model
{
    /// <summary>
    /// 端口监听配置实体 —— 描述一个被本工具监听的 TCP 端口
    /// </summary>
    public class PortConfig
    {
        /// <summary>
        /// 监听端口号（取值范围 1 ~ 65535）
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 是否启用 —— 为 false 时"启动全部"会跳过该端口
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 备注名称 —— 便于识别该端口模拟的设备，可为空
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值（规约：所有定义的变量必须有默认值）
        /// </summary>
        public PortConfig()
        {
            Port = 0;
            Enabled = true;
            Remark = string.Empty;
        }

        /// <summary>
        /// 构造函数 —— 按端口号创建
        /// </summary>
        /// <param name="port">端口号</param>
        public PortConfig(int port)
        {
            Port = port;
            Enabled = true;
            Remark = string.Empty;
        }

        /// <summary>
        /// 返回便于日志输出的文本
        /// </summary>
        /// <returns>端口配置描述文本</returns>
        public override string ToString()
        {
            return string.Format("Port={0}, Enabled={1}, Remark={2}",
                Port, Enabled, Remark ?? string.Empty);
        }
    }
}
