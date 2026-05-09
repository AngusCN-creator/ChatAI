﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChatAI.Data;
using ChatAI.Data.Entities;

namespace ChatAI.UI.Forms
{
    public partial class FrmSessionSettings : Form
    {
        private readonly string _loginUsername;
        private readonly UserRepository _userRepo = new UserRepository();

        public FrmSessionSettings(string loginUsername)
        {
            InitializeComponent();
            _loginUsername = loginUsername;
            LoadSessionSettings();
            SetupEventHandlers();
            // 订阅Load事件
            this.Load += FrmSessionSettings_Load;
        }

        private void LoadSessionSettings()
        {
            var settings = _userRepo.LoadSessionSettings(_loginUsername);
            if (settings != null)
            {
                // 加载设置到控件
                chkNoRole.Checked = settings.NoRole;
                txtContextLength.Text = settings.HistoryCount.ToString();
                txtMaxTokens.Text = settings.MaxPromptTokens.ToString();
                lblTemperatureValue.Text = settings.Temperature.ToString("0.0");
                // trkTemperature的值范围是0-20，温度值范围是0-2，所以需要乘以10
                trkTemperature.Value = (int)(settings.Temperature * 10);
                txtAutoMemoryCount.Text = settings.MemoryTrigger.ToString();
                if (!string.IsNullOrEmpty(settings.MemoryPrompt))
                {
                    txtMemoryPrompt.Text = settings.MemoryPrompt;
                }
                
                // 当数据库中有数据时，设置正常的用户输入样式（黑色+正体）
                SetUserInputStyle();
            }
            else
            {
                // 当settings为null时，根据lblTemperatureValue.Text计算滑块位置
                // 设计器中lblTemperatureValue.Text = "0.3"，trkTemperature的值范围是0-20，所以滑块位置应该是3 (0.3 * 10)
                if (double.TryParse(lblTemperatureValue.Text, out double tempValue))
                {
                    trkTemperature.Value = (int)(tempValue * 10);
                }
                else
                {
                    // 如果解析失败，设置默认值
                    trkTemperature.Value = 3; // 0.3 * 10
                }
            }
        }

        private void SetupEventHandlers()
        {
            // 温度滑块事件
            trkTemperature.ValueChanged += TrkTemperature_ValueChanged;
            // 保存按钮事件
            btnSaveSessionSettings.Click += BtnSave_Click;

            // TextBox获得焦点事件
            txtContextLength.GotFocus += Txt_GotFocus;
            txtMaxTokens.GotFocus += Txt_GotFocus;
            txtAutoMemoryCount.GotFocus += Txt_GotFocus;
            txtMemoryPrompt.GotFocus += TxtMemory_GotFocus;

            // TextBox失去焦点事件
            txtContextLength.LostFocus += Txt_LostFocus;
            txtMaxTokens.LostFocus += Txt_LostFocus;
            txtAutoMemoryCount.LostFocus += Txt_LostFocus;
            txtMemoryPrompt.LostFocus += TxtMemory_LostFocus;
        }

        private void FrmSessionSettings_Load(object sender, EventArgs e)
        {
            // 检查是否有数据库设置
            var settings = _userRepo.LoadSessionSettings(_loginUsername);
            
            if (settings == null)
            {
                // 当数据库中无设置数据时，设置placeholder样式
                SetPlaceholderStyle();
            }
            
            // 移除自动焦点，将焦点设置到非文本框控件
            this.ActiveControl = chkNoRole;
        }

        private void SetUserInputStyle()
        {
            // 设置正常的用户输入样式（黑色+正体）
            txtContextLength.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
            txtContextLength.ForeColor = Color.Black;

            txtMaxTokens.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
            txtMaxTokens.ForeColor = Color.Black;

            txtAutoMemoryCount.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
            txtAutoMemoryCount.ForeColor = Color.Black;

            txtMemoryPrompt.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
            txtMemoryPrompt.ForeColor = Color.Black;
        }

        private void SetPlaceholderStyle()
        {
            // 只有当文本框为空或等于默认值时才设置placeholder样式
            if (string.IsNullOrWhiteSpace(txtContextLength.Text) || txtContextLength.Text == "20")
            {
                txtContextLength.Text = "20";
                txtContextLength.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
                txtContextLength.ForeColor = Color.LightGray;
            }

            if (string.IsNullOrWhiteSpace(txtMaxTokens.Text) || txtMaxTokens.Text == "4096")
            {
                txtMaxTokens.Text = "4096";
                txtMaxTokens.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
                txtMaxTokens.ForeColor = Color.LightGray;
            }

            if (string.IsNullOrWhiteSpace(txtAutoMemoryCount.Text) || txtAutoMemoryCount.Text == "20")
            {
                txtAutoMemoryCount.Text = "20";
                txtAutoMemoryCount.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
                txtAutoMemoryCount.ForeColor = Color.LightGray;
            }

            // 设计器中使用的是中文引号
            string memoryPromptPlaceholder = "根据人设、与用户对话历史，以AI第一人称\"我\"的角度总结与用户的重要交谈内容，语言精炼无废话（3-5句话），不添加虚构内容，仅保留可长期复用的核心记忆点，用于后续对话个性化参考。";
            
            // 当数据库中无设置数据时，强制设置txtMemoryPrompt的样式
            // 这样可以确保它与其他文本框的样式一致
            txtMemoryPrompt.Text = memoryPromptPlaceholder;
            txtMemoryPrompt.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
            txtMemoryPrompt.ForeColor = Color.LightGray;
        }

        private void Txt_GotFocus(object? sender, EventArgs e)
        {
            if (sender is not TextBox txt) return;

            string[] placeholders = { "20", "4096", "20" };

            if (Array.Exists(placeholders, p => p == txt.Text))
            {
                txt.Text = string.Empty;
                txt.Font = new Font("微软雅黑", 10f, FontStyle.Regular);
                txt.ForeColor = Color.Black;
            }
        }

        private void Txt_LostFocus(object? sender, EventArgs e)
        {
            if (sender is not TextBox txt) return;

            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                string placeholder = txt.Name switch
                {
                    nameof(txtContextLength) => "20",
                    nameof(txtMaxTokens) => "4096",
                    nameof(txtAutoMemoryCount) => "20",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(placeholder))
                {
                    txt.Text = placeholder;
                    txt.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
                    txt.ForeColor = Color.LightGray;
                }
            }
        }

        private void TxtMemory_GotFocus(object? sender, EventArgs e)
        {
            if (sender is not TextBox txt) return;

            string placeholder = "根据人设、与用户对话历史，以AI第一人称\"我\"的角度总结与用户的重要交谈内容，语言精炼无废话（3-5句话），不添加虚构内容，仅保留可长期复用的核心记忆点，用于后续对话个性化参考。";

            if (txt.Text == placeholder)
            {
                txt.Text = string.Empty;
                txt.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
                txt.ForeColor = Color.Black;
            }
        }

        private void TxtMemory_LostFocus(object? sender, EventArgs e)
        {
            if (sender is not TextBox txt) return;

            string placeholder = "根据人设、与用户对话历史，以AI第一人称\"我\"的角度总结与用户的重要交谈内容，语言精炼无废话（3-5句话），不添加虚构内容，仅保留可长期复用的核心记忆点，用于后续对话个性化参考。";

            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                txt.Text = placeholder;
                txt.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
                txt.ForeColor = Color.LightGray;
            }
        }

        private void TrkTemperature_ValueChanged(object sender, EventArgs e)
        {
            // trkTemperature的值范围是0-20，温度值范围是0-2，所以需要除以10
            double temperature = trkTemperature.Value / 10.0;
            lblTemperatureValue.Text = temperature.ToString("0.0");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 验证输入
                if (!int.TryParse(txtContextLength.Text, out int historyCount) || historyCount <= 0)
                {
                    MessageBox.Show("请输入有效的历史消息数量", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!int.TryParse(txtMaxTokens.Text, out int maxPromptTokens) || maxPromptTokens <= 0)
                {
                    MessageBox.Show("请输入有效的最大提示词数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!int.TryParse(txtAutoMemoryCount.Text, out int memoryTrigger) || memoryTrigger <= 0)
                {
                    MessageBox.Show("请输入有效的记忆触发阈值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建会话设置实体
                var settings = new SessionSettings
                {
                    UserAccount = _loginUsername,
                    NoRole = chkNoRole.Checked,
                    HistoryCount = historyCount,
                    MaxPromptTokens = maxPromptTokens,
                    Temperature = double.Parse(lblTemperatureValue.Text),
                    MemoryTrigger = memoryTrigger,
                    MemoryPrompt = txtMemoryPrompt.Text
                };

                // 保存设置
                bool success = _userRepo.SaveSessionSettings(settings, out string errorMessage);
                if (success)
                {
                    MessageBox.Show("设置保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"保存失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置异常：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}