namespace TcpServer.Model
{
    /// <summary>
    /// 自动应答规则实体 —— "收到什么就回什么"的模拟设备应答配置
    /// </summary>
    public class AutoReplyRule
    {
        /// <summary>
        /// 规则名称 —— 便于在界面上识别
        /// </summary>
        public string RuleName { get; set; }

        /// <summary>
        /// 是否启用 —— 为 false 时该规则不参与匹配
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 匹配内容
        /// </summary>
        public string MatchText { get; set; }

        /// <summary>
        /// 匹配内容是否按十六进制解释（true 时 MatchText 形如 "4F 4B"）
        /// </summary>
        public bool MatchAsHex { get; set; }

        /// <summary>
        /// 是否要求完全匹配（false 表示包含即命中）
        /// </summary>
        public bool MatchExactly { get; set; }

        /// <summary>
        /// 应答内容
        /// </summary>
        public string ReplyText { get; set; }

        /// <summary>
        /// 应答内容是否按十六进制解释
        /// </summary>
        public bool ReplyAsHex { get; set; }

        /// <summary>
        /// 应答延迟毫秒数（0 表示立即应答）
        /// </summary>
        public int DelayMs { get; set; }

        /// <summary>
        /// 是否仅对指定端口生效（0 表示对所有端口生效）
        /// </summary>
        public int OnlyForPort { get; set; }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值
        /// </summary>
        public AutoReplyRule()
        {
            RuleName = string.Empty;
            Enabled = true;
            MatchText = string.Empty;
            MatchAsHex = false;
            MatchExactly = false;
            ReplyText = string.Empty;
            ReplyAsHex = false;
            DelayMs = 0;
            OnlyForPort = 0;
        }

        /// <summary>
        /// 返回便于日志输出的文本
        /// </summary>
        /// <returns>规则描述文本</returns>
        public override string ToString()
        {
            return string.Format("{0} [{1}] {2} -> {3}",
                RuleName ?? string.Empty,
                Enabled ? "启用" : "停用",
                MatchText ?? string.Empty,
                ReplyText ?? string.Empty);
        }
    }
}
