﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using ChatAI.Data;
using ChatControl.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChatAI.UI.Forms
{
    public partial class FrmMemoryManager : Form
    {
        /// <summary>
        /// 登录用户名
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? LoginUsername { get; set; }

        /// <summary>
        /// 要选中的会话ID
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int? SelectedSessionId { get; set; }

        /// <summary>
        /// 当前选中的会话信息
        /// </summary>
        private SessionInfo? _currentSession;

        /// <summary>
        /// 记忆数据集合，存储ID和内容的映射
        /// </summary>
        private Dictionary<int, string> _memoryMap = new Dictionary<int, string>();

        public FrmMemoryManager()
        {
            InitializeComponent();
            InitializeDataGridView();
        }

        /// <summary>
        /// 初始化DataGridView控件
        /// </summary>
        private void InitializeDataGridView()
        {
            dgvMemories.AutoGenerateColumns = false;
            dgvMemories.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMemories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMemories.MultiSelect = false;
            dgvMemories.ReadOnly = true;
            dgvMemories.RowHeadersVisible = false;

            // 添加列
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "Id";
            idColumn.HeaderText = "ID";
            idColumn.DataPropertyName = "Id";
            idColumn.Visible = false; // 隐藏ID列
            dgvMemories.Columns.Add(idColumn);

            DataGridViewTextBoxColumn contentColumn = new DataGridViewTextBoxColumn();
            contentColumn.Name = "Content";
            contentColumn.HeaderText = "记忆内容";
            contentColumn.DataPropertyName = "Content";
            contentColumn.Width = 400;
            contentColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvMemories.Columns.Add(contentColumn);

            DataGridViewTextBoxColumn timeColumn = new DataGridViewTextBoxColumn();
            timeColumn.Name = "CreateTime";
            timeColumn.HeaderText = "创建时间";
            timeColumn.DataPropertyName = "CreateTime";
            timeColumn.Width = 120;
            timeColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvMemories.Columns.Add(timeColumn);

            // 设置行高以支持多行显示
            dgvMemories.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvMemories.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void FrmMemoryManager_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(LoginUsername))
            {
                LoadSessionList();
            }
        }

        /// <summary>
        /// 加载会话列表
        /// </summary>
        private void LoadSessionList()
        {
            if (string.IsNullOrEmpty(LoginUsername)) return;

            var userRepo = new UserRepository();
            var sessions = userRepo.LoadAiCharacter(LoginUsername);

            lbxRoleList.Items.Clear();
            foreach (var session in sessions)
            {
                lbxRoleList.Items.Add(session);
            }

            // 设置显示文本
            lbxRoleList.DisplayMember = "AiName";

            // 自动选中指定的会话
            if (SelectedSessionId.HasValue)
            {
                for (int i = 0; i < lbxRoleList.Items.Count; i++)
                {
                    if (lbxRoleList.Items[i] is SessionInfo session && session.Id == SelectedSessionId.Value)
                    {
                        lbxRoleList.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void lbxRoleList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbxRoleList.SelectedItem is SessionInfo session)
            {
                _currentSession = session;
                LoadMemories(session);
            }
        }

        /// <summary>
        /// 加载指定会话的记忆列表
        /// </summary>
        /// <param name="session">会话信息</param>
        private void LoadMemories(SessionInfo session)
        {
            if (string.IsNullOrEmpty(LoginUsername)) return;

            var userRepo = new UserRepository();
            var memories = userRepo.GetMemoriesWithIds(LoginUsername, session.Id);

            dgvMemories.DataSource = memories;
            _memoryMap.Clear();

            foreach (var memory in memories)
            {
                // 存储ID和内容的映射
                _memoryMap[memory.Id] = memory.Content;
            }
        }

        private void dgvMemories_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvMemories.Rows[e.RowIndex];
                int memoryId = (int)row.Cells["Id"].Value;
                string currentContent = _memoryMap[memoryId];

                // 打开编辑窗口，设置父窗口并居中显示
                using var editForm = new FrmMemoryEdit();
                editForm.MemoryContent = currentContent;
                editForm.StartPosition = FormStartPosition.CenterParent;
                editForm.Owner = this;
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    // 更新内存中的数据
                    _memoryMap[memoryId] = editForm.MemoryContent;
                    // 更新DataGridView中的显示
                    row.Cells["Content"].Value = editForm.MemoryContent;
                }
            }
        }

        private void btnDeleteMemory_Click(object sender, EventArgs e)
        {
            if (dgvMemories.SelectedRows.Count > 0)
            {
                var selectedRow = dgvMemories.SelectedRows[0];
                int memoryId = (int)selectedRow.Cells["Id"].Value;

                // 确认对话框
                var result = MessageBox.Show(
                    "删除操作不可恢复，确定要删除这条记忆吗？",
                    "删除确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // 从数据库删除
                    var userRepo = new UserRepository();
                    bool success = userRepo.DeleteMemory(memoryId, out string errorMessage);

                    if (success)
                    {
                        // 从内存中移除
                        _memoryMap.Remove(memoryId);
                        // 重新加载记忆列表
                        LoadMemories(_currentSession);
                        MessageBox.Show("记忆删除成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"删除失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的记忆！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnSaveMemory_Click(object sender, EventArgs e)
        {
            if (_currentSession == null)
            {
                MessageBox.Show("请先选择一个会话！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 保存所有记忆
            var userRepo = new UserRepository();
            bool allSuccess = true;
            StringBuilder errorMessages = new StringBuilder();

            foreach (DataGridViewRow row in dgvMemories.Rows)
            {
                if (!row.IsNewRow)
                {
                    int memoryId = (int)row.Cells["Id"].Value;
                    string newContent = row.Cells["Content"].Value.ToString();

                    bool success = userRepo.UpdateMemory(memoryId, newContent, out string errorMessage);
                    if (!success)
                    {
                        allSuccess = false;
                        errorMessages.AppendLine($"ID {memoryId}：{errorMessage}");
                    }
                }
            }

            if (allSuccess)
            {
                MessageBox.Show("所有记忆保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show($"部分记忆保存失败：\n{errorMessages}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// 记忆编辑窗口
    /// </summary>
    public class FrmMemoryEdit : Form
    {
        private TextBox txtMemoryContent;
        private Button btnOK;
        private Button btnCancel;

        /// <summary>
        /// 记忆内容
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? MemoryContent
        {
            get => txtMemoryContent?.Text;
            set 
            {
                if (txtMemoryContent != null)
                {
                    // 处理文本，在序号后添加换行符
                    string processedText = value ?? string.Empty;
                    // 匹配数字序号模式，如 "1. "、"2. " 等
                    processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"(?<!^)(\d+\.\s)", "\n$1");
                    // 同时处理中文句号后的空格，添加换行符
                    processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"([。！？])\s*", "$1\n");
                    // 统一换行格式，Windows 控件只认 \r\n
                    processedText = processedText.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    txtMemoryContent.Text = processedText;
                }
            }
        }

        public FrmMemoryEdit()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.txtMemoryContent = new TextBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.SuspendLayout();
            // 
            // txtMemoryContent
            // 
            this.txtMemoryContent.Dock = DockStyle.Top;
            this.txtMemoryContent.Multiline = true;
            this.txtMemoryContent.WordWrap = true;
            this.txtMemoryContent.ScrollBars = ScrollBars.Both;
            this.txtMemoryContent.Font = new System.Drawing.Font(this.txtMemoryContent.Font.FontFamily, this.txtMemoryContent.Font.Size + 2);
            this.txtMemoryContent.Location = new Point(0, 0);
            this.txtMemoryContent.Name = "txtMemoryContent";
            this.txtMemoryContent.Size = new Size(400, 300);
            this.txtMemoryContent.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnOK.Location = new Point(234, 316);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(315, 316);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // FrmMemoryEdit
            // 
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(400, 350);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtMemoryContent);
            this.Name = "FrmMemoryEdit";
            this.Text = "编辑记忆";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
