using System;
using System.Collections.Generic;
using System.Text;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Auto-reply engine - replies automatically to received data according to preset rules,
    /// simulating device behaviour.
    /// Matching strategy: the first hit in rule-list order wins; both the enabled flag and the port
    /// restriction take part in filtering.
    /// </summary>
    public class AutoReplyEngine
    {
        #region Fields

        private readonly object _lockObj = new object();
        private List<AutoReplyRule> _rules;
        private Encoding _encoding;

        #endregion

        #region Properties

        /// <summary>
        /// Text encoding used for matching and replying (GBK by default, compatible with the ASCII and
        /// Chinese scenarios common in on-site devices).
        /// </summary>
        public Encoding TextEncoding
        {
            get { return _encoding; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor - uses the default GBK encoding.
        /// </summary>
        public AutoReplyEngine()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor taking an explicit text encoding.
        /// </summary>
        /// <param name="encoding">Text encoding; uses GBK when null.</param>
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

        #region Public Methods

        /// <summary>
        /// Updates the rule list (passing null is treated as clearing it).
        /// </summary>
        /// <param name="rules">Rule list.</param>
        public void UpdateRules(List<AutoReplyRule> rules)
        {
            lock (_lockObj)
            {
                _rules = rules ?? new List<AutoReplyRule>();
                LogHelper.Instance.Info(string.Format("Auto reply rules updated, {0} rule(s) in total.", _rules.Count));
            }
        }

        /// <summary>
        /// Gets a copy of the current rule list.
        /// </summary>
        /// <returns>Rule collection, never null.</returns>
        public List<AutoReplyRule> GetRules()
        {
            lock (_lockObj)
            {
                return new List<AutoReplyRule>(_rules);
            }
        }

        /// <summary>
        /// Attempts to match a reply rule.
        /// </summary>
        /// <param name="port">Port the data came from.</param>
        /// <param name="data">Raw received data.</param>
        /// <param name="length">Effective data length.</param>
        /// <param name="matchedRule">The matched rule; null when nothing matched.</param>
        /// <param name="replyData">Reply data; an empty array when nothing matched.</param>
        /// <returns>true when a rule was matched.</returns>
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

                // Port restriction: 0 means the rule applies to all ports.
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
        /// Converts text to a byte array using the current encoding.
        /// </summary>
        /// <param name="text">Text content.</param>
        /// <returns>Byte array, never null.</returns>
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
        /// Converts a byte array to text using the current encoding.
        /// </summary>
        /// <param name="data">Byte array.</param>
        /// <param name="length">Effective length.</param>
        /// <returns>Text content, never null.</returns>
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

        #region Private Methods

        /// <summary>
        /// Determines whether a single rule matches.
        /// </summary>
        /// <param name="rule">Rule.</param>
        /// <param name="data">Received data.</param>
        /// <param name="length">Effective length.</param>
        /// <returns>true when it matches.</returns>
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
        /// Builds the reply data for a rule.
        /// </summary>
        /// <param name="rule">Matched rule.</param>
        /// <returns>Reply byte array, never null.</returns>
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
        /// Determines whether two byte sequences are exactly equal.
        /// </summary>
        /// <param name="source">Source data.</param>
        /// <param name="offset">Source start offset.</param>
        /// <param name="length">Source effective length.</param>
        /// <param name="pattern">Pattern to compare.</param>
        /// <returns>true when exactly equal.</returns>
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
        /// Determines whether a byte sequence contains the specified pattern.
        /// </summary>
        /// <param name="source">Source data.</param>
        /// <param name="offset">Source start offset.</param>
        /// <param name="length">Source effective length.</param>
        /// <param name="pattern">Pattern to compare.</param>
        /// <returns>true when contained.</returns>
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
