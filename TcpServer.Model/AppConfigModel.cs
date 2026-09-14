using System;
using System.Collections.Generic;

namespace TcpServer.Model
{
    /// <summary>
    /// 应用配置根对象 —— 对应 Config\portconfig.json 的顶层结构
    /// </summary>
    public class AppConfigModel
    {
        /// <summary>
        /// 配置结构版本号 —— 后续升级时用于兼容旧文件
        /// </summary>
        public string ConfigVersion { get; set; }

        /// <summary>
        /// 监听地址（如 127.0.0.1、本机网卡地址或 0.0.0.0）
        /// </summary>
        public string ListenIp { get; set; }

        /// <summary>
        /// 起始端口号（默认 60000）
        /// </summary>
        public int StartPort { get; set; }

        /// <summary>
        /// 端口数量 —— 从起始端口连开的个数
        /// </summary>
        public int PortCount { get; set; }

        /// <summary>
        /// 是否在程序启动后自动开始监听（默认 false，防止误占端口）
        /// </summary>
        public bool AutoStartOnLaunch { get; set; }

        /// <summary>
        /// 数据区是否以十六进制显示（false 为 ASCII）
        /// </summary>
        public bool DisplayAsHex { get; set; }

        /// <summary>
        /// 端口清单 —— 业务配置主体
        /// </summary>
        public List<PortConfig> Ports { get; set; }

        /// <summary>
        /// 自动应答规则清单
        /// </summary>
        public List<AutoReplyRule> Rules { get; set; }

        /// <summary>
        /// 配置最后保存时间
        /// </summary>
        public DateTime LastSavedTime { get; set; }

        /// <summary>备用字段 1（规约要求：预留 5 个扩展字段，后续升级不改结构）</summary>
        public string Exp1 { get; set; }

        /// <summary>备用字段 2</summary>
        public string Exp2 { get; set; }

        /// <summary>备用字段 3</summary>
        public string Exp3 { get; set; }

        /// <summary>备用字段 4</summary>
        public string Exp4 { get; set; }

        /// <summary>备用字段 5</summary>
        public string Exp5 { get; set; }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值，并初始化非空集合
        /// </summary>
        public AppConfigModel()
        {
            ConfigVersion = "1.0";
            ListenIp = "127.0.0.1";
            StartPort = 60000;
            PortCount = 0;
            AutoStartOnLaunch = false;
            DisplayAsHex = false;
            Ports = new List<PortConfig>();
            Rules = new List<AutoReplyRule>();
            LastSavedTime = DateTime.MinValue;
            Exp1 = string.Empty;
            Exp2 = string.Empty;
            Exp3 = string.Empty;
            Exp4 = string.Empty;
            Exp5 = string.Empty;
        }
    }
}
