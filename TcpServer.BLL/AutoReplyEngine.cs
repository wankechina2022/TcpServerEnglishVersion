using System;
using System.Collections.Generic;
using System.Text;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 自动应答引擎 —— 按预置规则对收到的数据自动回复，用于模拟设备行为
    /// 匹配策略：按规则列表顺序取第一条命中项；启用/端口限定均参与过滤
    /// </summary>
    public class AutoReplyEngine
    {
        #region 字段

        private readonly object _lockObj = new object();
        private List<AutoReplyRule> _rules;
        private Encoding _encoding;

        #endregion

        #region 属性

        /// <summary>
        /// 匹配与应答时使用的文本编码（默认 GBK，兼容现场设备常见的 ASCII 与中文场景）
        /// </summary>
        public Encoding TextEncoding
        {
            get { return _encoding; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 —— 使用默认编码 GBK
        /// </summary>
        public AutoReplyEngine()
            : this(null)
        {
        }

        /// <summary>
        /// 指定文本编码的构造函数
        /// </summary>
        /// <param name="encoding">文本编码，为 null 时使用 GBK</param>
        public AutoReplyEngine(Encoding encoding)
        {
            _rules = new List<AutoReplyRule>();

            if (encoding != null)
            {
                _encoding = encoding;
            }
            else
            {
                try
                {
                    _encoding = Encoding.GetEncoding("GBK");
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Warn("GBK encoding unavailable, falling back to UTF-8:" + ex.Message);
                    _encoding = Encoding.UTF8;
                }
            }
        }

        #endregion

        #region 对外方法

        /// <summary>
        /// 更新规则清单（传入 null 时视为清空）
        /// </summary>
        /// <param name="rules">规则列表</param>
        public void UpdateRules(List<AutoReplyRule> rules)
        {
            lock (_lockObj)
            {
                _rules = rules ?? new List<AutoReplyRule>();
                LogHelper.Instance.Info(string.Format("Auto reply rules updated, {0} rule(s) in total.", _rules.Count));
            }
        }

        /// <summary>
        /// 获取当前规则清单的副本
        /// </summary>
        /// <returns>规则集合，永不为 null</returns>
        public List<AutoReplyRule> GetRules()
        {
            lock (_lockObj)
            {
                return new List<AutoReplyRule>(_rules);
            }
        }

        /// <summary>
        /// 尝试匹配应答规则
        /// </summary>
        /// <param name="port">数据来源端口</param>
        /// <param name="data">收到的原始数据</param>
        /// <param name="length">有效数据长度</param>
        /// <param name="matchedRule">命中的规则，未命中时为 null</param>
        /// <param name="replyData">应答数据，未命中时为空数组</param>
        /// <returns>命中返回 true</returns>
        public bool TryMatch(int port, byte[] data, int length,
            out AutoReplyRule matchedRule, out byte[] replyData)
        {
            matchedRule = null;
            replyData = new byte[0];

            if (data == null || length < 1)
            {
                return false;
            }

            List<AutoReplyRule> snapshot;
            lock (_lockObj)
            {
                snapshot = new List<AutoReplyRule>(_rules);
            }

            foreach (AutoReplyRule rule in snapshot)
            {
                if (rule == null || !rule.Enabled)
                {
                    continue;
                }

                // 端口限定：0 表示对所有端口生效
                if (rule.OnlyForPort > 0 && rule.OnlyForPort != port)
                {
                    continue;
                }

                if (!IsMatched(rule, data, length))
                {
                    continue;
                }

                byte[] reply = BuildReplyData(rule);
                if (reply == null || reply.Length == 0)
                {
                    LogHelper.Instance.Warn(string.Format("Rule [{0}] matched but the reply content is empty. Ignored.",
                        rule.RuleName));
                    continue;
                }

                matchedRule = rule;
                replyData = reply;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 将文本按当前编码转换为字节数组
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>字节数组，永不为 null</returns>
        public byte[] TextToBytes(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new byte[0];
            }

            try
            {
                return _encoding.GetBytes(text);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to convert text to bytes:" + ex.Message, ex);
                return new byte[0];
            }
        }

        /// <summary>
        /// 将字节数组按当前编码转换为文本
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="length">有效长度</param>
        /// <returns>文本内容，永不为 null</returns>
        public string BytesToText(byte[] data, int length)
        {
            if (data == null || data.Length == 0 || length < 1)
            {
                return string.Empty;
            }

            try
            {
                int count = length > data.Length ? data.Length : length;
                return _encoding.GetString(data, 0, count);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn("Failed to convert bytes to text:" + ex.Message);
                return string.Empty;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 判断单条规则是否命中
        /// </summary>
        /// <param name="rule">规则</param>
        /// <param name="data">收到的数据</param>
        /// <param name="length">有效长度</param>
        /// <returns>命中返回 true</returns>
        private bool IsMatched(AutoReplyRule rule, byte[] data, int length)
        {
            if (rule == null || string.IsNullOrEmpty(rule.MatchText))
            {
                return false;
            }

            try
            {
                if (rule.MatchAsHex)
                {
                    byte[] pattern = HexHelper.HexToBytes(rule.MatchText);
                    if (pattern.Length == 0)
                    {
                        LogHelper.Instance.Warn(string.Format("Rule [{0}] has invalid hexadecimal match content. Skipped.",
                            rule.RuleName));
                        return false;
                    }

                    return rule.MatchExactly
                        ? ByteEquals(data, 0, length, pattern)
                        : ByteContains(data, 0, length, pattern);
                }

                string received = BytesToText(data, length);
                if (string.IsNullOrEmpty(received))
                {
                    return false;
                }

                return rule.MatchExactly
                    ? string.Equals(received, rule.MatchText, StringComparison.Ordinal)
                    : received.IndexOf(rule.MatchText, StringComparison.Ordinal) >= 0;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Exception while matching rule [{0}]: {1}", rule.RuleName, ex.Message), ex);
                return false;
            }
        }

        /// <summary>
        /// 根据规则构造应答数据
        /// </summary>
        /// <param name="rule">命中的规则</param>
        /// <returns>应答字节数组，永不为 null</returns>
        private byte[] BuildReplyData(AutoReplyRule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.ReplyText))
            {
                return new byte[0];
            }

            if (rule.ReplyAsHex)
            {
                return HexHelper.HexToBytes(rule.ReplyText);
            }

            return TextToBytes(rule.ReplyText);
        }

        /// <summary>
        /// 判断字节序列是否完全相等
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="offset">源起始偏移</param>
        /// <param name="length">源有效长度</param>
        /// <param name="pattern">比对模式</param>
        /// <returns>完全相等返回 true</returns>
        private bool ByteEquals(byte[] source, int offset, int length, byte[] pattern)
        {
            if (source == null || pattern == null) { return false; }
            if (length != pattern.Length) { return false; }

            for (int i = 0; i < pattern.Length; i++)
            {
                if (source[offset + i] != pattern[i]) { return false; }
            }

            return true;
        }

        /// <summary>
        /// 判断字节序列是否包含指定模式
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="offset">源起始偏移</param>
        /// <param name="length">源有效长度</param>
        /// <param name="pattern">比对模式</param>
        /// <returns>包含返回 true</returns>
        private bool ByteContains(byte[] source, int offset, int length, byte[] pattern)
        {
            if (source == null || pattern == null) { return false; }
            if (pattern.Length == 0 || pattern.Length > length) { return false; }

            int maxStart = offset + length - pattern.Length;
            for (int i = offset; i <= maxStart; i++)
            {
                bool matched = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) { return true; }
            }

            return false;
        }

        #endregion
    }
}
