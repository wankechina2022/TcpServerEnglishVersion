using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.UI.Forms
{
    /// <summary>
    /// 自动应答规则管理窗体 —— 以对话框方式编辑"收到什么就回什么"的规则清单
    /// </summary>
    public partial class frmRuleManager : Form
    {
        // ============================================================
        // 1. 私有字段
        // ============================================================

        /// <summary>日志工具</summary>
        private readonly LogHelper _logger = LogHelper.Instance;

        /// <summary>供表格绑定的规则列表</summary>
        private BindingList<AutoReplyRule> _ruleList;

        /// <summary>编辑结果（确定后由调用方取用）</summary>
        private List<AutoReplyRule> _rules;

        // ============================================================
        // 2. 属性
        // ============================================================

        /// <summary>
        /// 编辑后的规则清单 —— 仅在对话框返回"确定"时有效
        /// </summary>
        public List<AutoReplyRule> Rules
        {
            get { return _rules ?? new List<AutoReplyRule>(); }
        }

        // ============================================================
        // 3. 构造函数
        // ============================================================

        /// <summary>
        /// 无参构造函数 —— 供窗体设计器使用
        /// </summary>
        public frmRuleManager()
            : this(null)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rules">待编辑的规则清单，可为 null</param>
        public frmRuleManager(List<AutoReplyRule> rules)
        {
            InitializeComponent();
            InitializeCustomSettings(rules);
        }

        // ============================================================
        // 4. 自定义初始化
        // ============================================================

        /// <summary>
        /// 自定义初始化 —— 深拷贝入参并绑定表格
        /// </summary>
        /// <param name="rules">待编辑的规则清单</param>
        private void InitializeCustomSettings(List<AutoReplyRule> rules)
        {
            try
            {
                // 深拷贝，保证"取消"时不影响原配置
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
        /// 绑定控件事件
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
        // 5. 事件处理
        // ============================================================

        /// <summary>
        /// 新增规则
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
        /// 删除规则
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
        /// 规则上移
        /// </summary>
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveRule(-1);
        }

        /// <summary>
        /// 规则下移
        /// </summary>
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveRule(1);
        }

        /// <summary>
        /// 确定 —— 校验并返回结果
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
        /// 取消
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// 表格数据错误 —— 防止输入非法值导致异常弹窗
        /// </summary>
        private void dgvRules_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;

            _logger.Warn(string.Format("Invalid input in the reply rules grid (row {0}, column {1}).",
                e.RowIndex + 1, e.ColumnIndex + 1));
        }

        // ============================================================
        // 6. 私有方法
        // ============================================================

        /// <summary>
        /// 移动规则位置
        /// </summary>
        /// <param name="offset">-1 上移，1 下移</param>
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
        /// 校验规则清单
        /// </summary>
        /// <param name="errorMessage">失败原因</param>
        /// <returns>全部合法返回 true</returns>
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
        /// 深拷贝规则清单（保证取消编辑时不污染原配置）
        /// </summary>
        /// <param name="rules">源清单，可为 null</param>
        /// <returns>拷贝出的新清单，永不为 null</returns>
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
