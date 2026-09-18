namespace TcpServer.Model
{
    /// <summary>
    /// Auto-reply rule entity - simulates a device that answers "reply with whatever was received".
    /// </summary>
    public class AutoReplyRule
    {
        /// <summary>
        /// Rule name - makes the rule easy to identify in the UI.
        /// </summary>
        public string RuleName { get; set; }

        /// <summary>
        /// Whether enabled - when false, this rule does not take part in matching.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Match content.
        /// </summary>
        public string MatchText { get; set; }

        /// <summary>
        /// Whether the match content is interpreted as hexadecimal (when true, MatchText looks like "4F 4B").
        /// </summary>
        public bool MatchAsHex { get; set; }

        /// <summary>
        /// Whether an exact match is required (false means a containment match is enough).
        /// </summary>
        public bool MatchExactly { get; set; }

        /// <summary>
        /// Reply content.
        /// </summary>
        public string ReplyText { get; set; }

        /// <summary>
        /// Whether the reply content is interpreted as hexadecimal.
        /// </summary>
        public bool ReplyAsHex { get; set; }

        /// <summary>
        /// Reply delay in milliseconds (0 means reply immediately).
        /// </summary>
        public int DelayMs { get; set; }

        /// <summary>
        /// Whether the rule applies to one specific port only (0 means all ports).
        /// </summary>
        public int OnlyForPort { get; set; }

        /// <summary>
        /// Constructor - every field is initialized to a default value.
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
        /// Returns a text representation suitable for log output.
        /// </summary>
        /// <returns>Rule description text.</returns>
        public override string ToString()
        {
            return string.Format("{0} [{1}] {2} -> {3}",
                RuleName ?? string.Empty,
                Enabled ? "Enabled" : "Disabled",
                MatchText ?? string.Empty,
                ReplyText ?? string.Empty);
        }
    }
}
