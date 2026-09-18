using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.UI.Forms
{
    /// <summary>
    /// Auto-reply rule manager form - edits the "reply with whatever was received" rule list
    /// as a dialog.
    /// </summary>
    public partial class frmRuleManager : Form
    {
        // ============================================================
        // 1. Private fields
        // ============================================================

        /// <summary>Logging utility.</summary>
        private readonly LogHelper _logger = LogHelper.Instance;

        /// <summary>Rule list bound to the grid.</summary>
        private BindingList<AutoReplyRule> _ruleList;

        /// <summary>Edit result (taken by the caller after OK).</summary>
        private List<AutoReplyRule> _rules;

        // ============================================================
        // 2. Properties
        // ============================================================

        /// <summary>
        /// The edited rule list - valid only when the dialog returned OK.
        /// </summary>
        public List<AutoReplyRule> Rules
        {
            get { return _rules ?? new List<AutoReplyRule>(); }
        }

        // ============================================================
        // 3. Constructors
        // ============================================================

        /// <summary>
        /// Parameterless constructor - for the form designer.
        /// </summary>
        public frmRuleManager()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rules">Rule list to edit; may be null.</param>
        public frmRuleManager(List<AutoReplyRule> rules)
        {
            InitializeComponent();
            InitializeCustomSettings(rules);
        }

        // ============================================================
        // 4. Custom initialization
        // ============================================================

        /// <summary>
        /// Custom initialization - deep-copies the argument and binds the grid.
        /// </summary>
        /// <param name="rules">Rule list to edit.</param>
        private void InitializeCustomSettings(List<AutoReplyRule> rules)
        {
            try
            {
                // Deep copy, so that "Cancel" does not affect the original configuration.
                _rules = DeepCopy(rules);
                _ruleList = new BindingList<AutoReplyRule>(_rules);
                dgvRules.DataSource = _ruleList;

                BindEvents();
            }
            catch (Exception ex)
            {
                _logger.Error("Reply rules form initialization failed:" + ex.Message, ex);
                MessageHelper.ShowError("Reply rules form initialization failed. See the log for details!");
            }
        }

        /// <summary>
        /// Binds the control events.
        /// </summary>
        private void BindEvents()
        {
            btnAdd.Click += btnAdd_Click;
            btnDelete.Click += btnDelete_Click;
            btnMoveUp.Click += btnMoveUp_Click;
            btnMoveDown.Click += btnMoveDown_Click;
            btnOk.Click += btnOk_Click;
            btnCancel.Click += btnCancel_Click;

            dgvRules.DataError += dgvRules_DataError;
        }

        // ============================================================
        // 5. Event handlers
        // ============================================================

        /// <summary>
        /// Adds a rule.
        /// </summary>
        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                AutoReplyRule rule = new AutoReplyRule();
                rule.RuleName = string.Format("New Rule {0}", _ruleList.Count + 1);

                _ruleList.Add(rule);

                if (dgvRules.Rows.Count > 0)
                {
                    int lastIndex = dgvRules.Rows.Count - 1;
                    dgvRules.CurrentCell = dgvRules.Rows[lastIndex].Cells[colRuleName.Index];
                    dgvRules.Rows[lastIndex].Selected = true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while adding the reply rule:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to add the rule. See the log for details!");
            }
        }

        /// <summary>
        /// Deletes a rule.
        /// </summary>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvRules.CurrentRow == null || dgvRules.CurrentRow.Index < 0)
                {
                    MessageHelper.ShowWarning("Please select a rule to delete first!");
                    return;
                }

                int index = dgvRules.CurrentRow.Index;

                if (!MessageHelper.ShowConfirm("Delete the selected reply rule?"))
                {
                    return;
                }

                dgvRules.EndEdit();

                if (index >= 0 && index < _ruleList.Count)
                {
                    _ruleList.RemoveAt(index);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while deleting the reply rule:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to delete the rule. See the log for details!");
            }
        }

        /// <summary>
        /// Moves a rule up.
        /// </summary>
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveRule(-1);
        }

        /// <summary>
        /// Moves a rule down.
        /// </summary>
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveRule(1);
        }

        /// <summary>
        /// OK - validates and returns the result.
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                dgvRules.EndEdit();

                string errorMessage;
                if (!ValidateRules(out errorMessage))
                {
                    MessageHelper.ShowWarning(errorMessage);
                    return;
                }

                _rules = new List<AutoReplyRule>(_ruleList);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while saving the reply rules:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to save the reply rules. See the log for details!");
            }
        }

        /// <summary>
        /// Cancel.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Grid data error - prevents invalid input from raising an exception dialog.
        /// </summary>
        private void dgvRules_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;

            _logger.Warn(string.Format("Invalid input in the reply rules grid (row {0}, column {1}).",
                e.RowIndex + 1, e.ColumnIndex + 1));
        }

        // ============================================================
        // 6. Private methods
        // ============================================================

        /// <summary>
        /// Moves a rule position.
        /// </summary>
        /// <param name="offset">-1 moves up, 1 moves down.</param>
        private void MoveRule(int offset)
        {
            try
            {
                if (dgvRules.CurrentRow == null || dgvRules.CurrentRow.Index < 0)
                {
                    MessageHelper.ShowWarning("Please select a rule to move first!");
                    return;
                }

                dgvRules.EndEdit();

                int index = dgvRules.CurrentRow.Index;
                int target = index + offset;

                if (target < 0 || target >= _ruleList.Count)
                {
                    return;
                }

                AutoReplyRule item = _ruleList[index];
                _ruleList.RemoveAt(index);
                _ruleList.Insert(target, item);

                dgvRules.CurrentCell = dgvRules.Rows[target].Cells[colRuleName.Index];
                dgvRules.Rows[target].Selected = true;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while moving the reply rule:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to move the rule. See the log for details!");
            }
        }

        /// <summary>
        /// Validates the rule list.
        /// </summary>
        /// <param name="errorMessage">Failure reason.</param>
        /// <returns>true when everything is valid.</returns>
        private bool ValidateRules(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (_ruleList == null || _ruleList.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < _ruleList.Count; i++)
            {
                AutoReplyRule rule = _ruleList[i];
                if (rule == null) { continue; }

                int lineNo = i + 1;

                if (!rule.Enabled)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rule.MatchText))
                {
                    errorMessage = string.Format("Row {0} is enabled but Match is empty!", lineNo);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(rule.ReplyText))
                {
                    errorMessage = string.Format("Row {0} is enabled but Reply is empty!", lineNo);
                    return false;
                }

                if (rule.MatchAsHex && !HexHelper.IsHexString(rule.MatchText))
                {
                    errorMessage = string.Format("Row {0}: Match is not valid hexadecimal!", lineNo);
                    return false;
                }

                if (rule.ReplyAsHex && !HexHelper.IsHexString(rule.ReplyText))
                {
                    errorMessage = string.Format("Row {0}: Reply is not valid hexadecimal!", lineNo);
                    return false;
                }

                if (rule.DelayMs < 0 || rule.DelayMs > 60000)
                {
                    errorMessage = string.Format("Row {0}: Delay must be between 0 and 60000 ms!", lineNo);
                    return false;
                }

                if (rule.OnlyForPort < 0 || rule.OnlyForPort > 65535)
                {
                    errorMessage = string.Format("Row {0}: Port Only must be between 0 and 65535!", lineNo);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Deep-copies the rule list (so cancelling an edit does not pollute the original configuration).
        /// </summary>
        /// <param name="rules">Source list; may be null.</param>
        /// <returns>A new copied list, never null.</returns>
        private List<AutoReplyRule> DeepCopy(List<AutoReplyRule> rules)
        {
            List<AutoReplyRule> result = new List<AutoReplyRule>();

            if (rules == null)
            {
                return result;
            }

            foreach (AutoReplyRule item in rules)
            {
                if (item == null) { continue; }

                AutoReplyRule copy = new AutoReplyRule();
                copy.RuleName = item.RuleName ?? string.Empty;
                copy.Enabled = item.Enabled;
                copy.MatchText = item.MatchText ?? string.Empty;
                copy.MatchAsHex = item.MatchAsHex;
                copy.MatchExactly = item.MatchExactly;
                copy.ReplyText = item.ReplyText ?? string.Empty;
                copy.ReplyAsHex = item.ReplyAsHex;
                copy.DelayMs = item.DelayMs;
                copy.OnlyForPort = item.OnlyForPort;

                result.Add(copy);
            }

            return result;
        }
    }
}
