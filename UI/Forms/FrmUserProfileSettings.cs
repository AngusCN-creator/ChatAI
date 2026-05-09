﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using ChatAI.Data;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChatAI.UI.Forms
{
    public partial class FrmUserProfileSettings : Form
    {
        private string? _loginUsername;
        private UserProfile? _currentUserProfile;
        private UserRepository _userRepo = new UserRepository();

        public FrmUserProfileSettings()
        {
            InitializeComponent();
        }

        public FrmUserProfileSettings(string loginUsername) : this()
        {
            _loginUsername = loginUsername;
        }

        private void FrmUserProfileSettings_Load(object sender, EventArgs e)
        {
            SetButtonRound(btnSaveUserProfile, 8);
            SetButtonRound(btnCancel, 8);

            txtNickName.GotFocus += Txt_GotFocus;
            txtNickName.LostFocus += Txt_LostFocus;
            txtPersona.GotFocus += Txt_GotFocus;
            txtPersona.LostFocus += Txt_LostFocus;
            txtSystemPrompt.GotFocus += Txt_GotFocus;
            txtSystemPrompt.LostFocus += Txt_LostFocus;

            btnSaveUserProfile.Click += BtnSave_Click;
            btnCancel.Click += (s, ev) => Close();

            SetTextBoxStyle(txtNickName, false);
            SetTextBoxStyle(txtPersona, true);
            SetTextBoxStyle(txtSystemPrompt, true);

            LoadUserProfile();
            
            // 移除自动焦点，将焦点设置到非文本框控件
            this.ActiveControl = rdoMale;
        }

        private void LoadUserProfile()
        {
            if (string.IsNullOrEmpty(_loginUsername))
                return;

            try
            {
                _currentUserProfile = _userRepo.GetUserProfile(_loginUsername);

                if (_currentUserProfile != null)
                {
                    txtNickName.Text = _currentUserProfile.Nickname ?? string.Empty;
                    txtNickName.ForeColor = Color.Black;
                    txtNickName.Font = new Font("微软雅黑", 12f, FontStyle.Regular);

                    if (_currentUserProfile.Gender == "女")
                    {
                        rdoFemale.Checked = true;
                    }
                    else
                    {
                        rdoMale.Checked = true;
                    }

                    txtPersona.Text = _currentUserProfile.Persona ?? string.Empty;
                    txtPersona.ForeColor = Color.Black;
                    txtPersona.Font = new Font("微软雅黑", 12f, FontStyle.Regular);

                    if (!string.IsNullOrEmpty(_currentUserProfile.SystemPrompt))
                    {
                        txtSystemPrompt.Text = _currentUserProfile.SystemPrompt;
                        txtSystemPrompt.ForeColor = Color.Black;
                        txtSystemPrompt.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
                    }
                }
                else
                {
                    // 只设置其他字段的默认值，不设置系统提示词，使用设计器中的默认值
                    txtNickName.Text = string.Empty;
                    txtNickName.ForeColor = Color.Black;
                    txtNickName.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
                    rdoMale.Checked = true;
                    txtPersona.Text = string.Empty;
                    txtPersona.ForeColor = Color.Black;
                    txtPersona.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
                }
                
                // 重新设置文本框样式，确保placeholder正确显示
                SetTextBoxStyle(txtNickName, false);
                SetTextBoxStyle(txtPersona, true);
                SetTextBoxStyle(txtSystemPrompt, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载用户信息失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetButtonRound(Button btn, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddLine(radius, 0, btn.Width - radius, 0);
            path.AddArc(btn.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddLine(btn.Width, radius, btn.Width, btn.Height - radius);
            path.AddArc(btn.Width - radius * 2, btn.Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddLine(btn.Width - radius, btn.Height, radius, btn.Height);
            path.AddArc(0, btn.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();

            btn.Region = new Region(path);
            btn.FlatAppearance.BorderSize = 0;
        }

        private void Txt_GotFocus(object? sender, EventArgs e)
        {
            if (sender is not TextBox txt) return;

            string[] placeholders = {
                "请输入用户昵称",
                "请输入人设设定（性格、背景等）",
                "请输入系统提示词"
            };

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
                    nameof(txtNickName) => "请输入用户昵称",
                    nameof(txtPersona) => "请输入人设设定（性格、背景等）",
                    nameof(txtSystemPrompt) => "请输入系统提示词",
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

        private void SetTextBoxStyle(TextBox txt, bool isMultiline)
        {
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                string placeholder = txt.Name switch
                {
                    nameof(txtNickName) => "请输入用户昵称",
                    nameof(txtPersona) => "请输入人设设定（性格、背景等）",
                    nameof(txtSystemPrompt) => "请输入系统提示词",
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

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_loginUsername))
            {
                MessageBox.Show("用户信息不完整，请重新登录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string nickname = txtNickName.Text == "请输入用户昵称" ? string.Empty : txtNickName.Text.Trim();
            string gender = rdoFemale.Checked ? "女" : "男";
            string persona = txtPersona.Text == "请输入人设设定（性格、背景等）" ? string.Empty : txtPersona.Text.Trim();
            string systemPrompt = txtSystemPrompt.Text == "请输入系统提示词" ? string.Empty : txtSystemPrompt.Text.Trim();

            if (string.IsNullOrEmpty(nickname))
            {
                MessageBox.Show("请输入用户昵称！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNickName.Focus();
                return;
            }

            try
            {
                var userProfile = new UserProfile
                {
                    UserAccount = _loginUsername,
                    Nickname = nickname,
                    Gender = gender,
                    Persona = persona,
                    SystemPrompt = systemPrompt
                };

                bool success = _userRepo.SaveUserProfile(userProfile, out string errorMessage);

                if (success)
                {
                    MessageBox.Show("用户配置保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show($"保存失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存异常：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}