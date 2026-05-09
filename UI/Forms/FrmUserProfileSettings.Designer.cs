﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿namespace ChatAI.UI.Forms
{
    partial class FrmUserProfileSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtSystemPrompt = new TextBox();
            lblSystemPrompt = new Label();
            txtPersona = new TextBox();
            lblPersona = new Label();
            rdoFemale = new RadioButton();
            rdoMale = new RadioButton();
            lblGender = new Label();
            txtNickName = new TextBox();
            lblNickName = new Label();
            btnCancel = new Button();
            btnSaveUserProfile = new Button();
            SuspendLayout();
            // 
            // txtSystemPrompt
            // 
            txtSystemPrompt.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtSystemPrompt.ForeColor = Color.LightGray;
            txtSystemPrompt.Location = new Point(99, 361);
            txtSystemPrompt.Multiline = true;
            txtSystemPrompt.Name = "txtSystemPrompt";
            txtSystemPrompt.Size = new Size(209, 148);
            txtSystemPrompt.TabIndex = 26;
            txtSystemPrompt.Text = "你是一个专业的演员，严格遵守以下人设全程扮演，绝不跳出角色：";
            // 
            // lblSystemPrompt
            // 
            lblSystemPrompt.AutoSize = true;
            lblSystemPrompt.Font = new Font("微软雅黑", 11F);
            lblSystemPrompt.ForeColor = Color.FromArgb(51, 51, 51);
            lblSystemPrompt.Location = new Point(6, 361);
            lblSystemPrompt.Name = "lblSystemPrompt";
            lblSystemPrompt.Size = new Size(99, 20);
            lblSystemPrompt.TabIndex = 25;
            lblSystemPrompt.Text = "系统提示词：";
            lblSystemPrompt.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtPersona
            // 
            txtPersona.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtPersona.ForeColor = Color.LightGray;
            txtPersona.Location = new Point(99, 147);
            txtPersona.Multiline = true;
            txtPersona.Name = "txtPersona";
            txtPersona.Size = new Size(209, 180);
            txtPersona.TabIndex = 20;
            txtPersona.Text = "请输入人设设定（性格、背景等）";
            // 
            // lblPersona
            // 
            lblPersona.AutoSize = true;
            lblPersona.Font = new Font("微软雅黑", 11F);
            lblPersona.ForeColor = Color.FromArgb(51, 51, 51);
            lblPersona.Location = new Point(21, 147);
            lblPersona.Name = "lblPersona";
            lblPersona.Size = new Size(84, 20);
            lblPersona.TabIndex = 19;
            lblPersona.Text = "人设设定：";
            lblPersona.TextAlign = ContentAlignment.MiddleRight;
            // 
            // rdoFemale
            // 
            rdoFemale.AutoSize = true;
            rdoFemale.Font = new Font("微软雅黑", 11F);
            rdoFemale.Location = new Point(197, 95);
            rdoFemale.Name = "rdoFemale";
            rdoFemale.Size = new Size(53, 24);
            rdoFemale.TabIndex = 18;
            rdoFemale.Text = "女♀";
            // 
            // rdoMale
            // 
            rdoMale.AutoSize = true;
            rdoMale.Checked = true;
            rdoMale.Font = new Font("微软雅黑", 11F);
            rdoMale.Location = new Point(119, 95);
            rdoMale.Name = "rdoMale";
            rdoMale.Size = new Size(53, 24);
            rdoMale.TabIndex = 17;
            rdoMale.TabStop = true;
            rdoMale.Text = "男♂";
            // 
            // lblGender
            // 
            lblGender.AutoSize = true;
            lblGender.Font = new Font("微软雅黑", 11F);
            lblGender.ForeColor = Color.FromArgb(51, 51, 51);
            lblGender.Location = new Point(27, 97);
            lblGender.Name = "lblGender";
            lblGender.Size = new Size(70, 20);
            lblGender.TabIndex = 16;
            lblGender.Text = "性    别：";
            lblGender.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtNickName
            // 
            txtNickName.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtNickName.ForeColor = Color.LightGray;
            txtNickName.Location = new Point(99, 38);
            txtNickName.Name = "txtNickName";
            txtNickName.Size = new Size(209, 29);
            txtNickName.TabIndex = 15;
            txtNickName.Text = "请输入用户昵称";
            // 
            // lblNickName
            // 
            lblNickName.AutoSize = true;
            lblNickName.Font = new Font("微软雅黑", 11F);
            lblNickName.ForeColor = Color.FromArgb(51, 51, 51);
            lblNickName.Location = new Point(27, 37);
            lblNickName.Name = "lblNickName";
            lblNickName.Size = new Size(70, 20);
            lblNickName.TabIndex = 14;
            lblNickName.Text = "昵    称：";
            lblNickName.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(220, 220, 220);
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("微软雅黑", 11F);
            btnCancel.Location = new Point(182, 535);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(140, 45);
            btnCancel.TabIndex = 28;
            btnCancel.Text = "取消";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSaveUserProfile
            // 
            btnSaveUserProfile.BackColor = Color.FromArgb(0, 122, 204);
            btnSaveUserProfile.FlatAppearance.BorderSize = 0;
            btnSaveUserProfile.FlatStyle = FlatStyle.Flat;
            btnSaveUserProfile.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            btnSaveUserProfile.ForeColor = Color.White;
            btnSaveUserProfile.Location = new Point(22, 535);
            btnSaveUserProfile.Name = "btnSaveUserProfile";
            btnSaveUserProfile.Size = new Size(140, 45);
            btnSaveUserProfile.TabIndex = 27;
            btnSaveUserProfile.Text = "保存";
            btnSaveUserProfile.UseVisualStyleBackColor = false;
            // 
            // FrmUserProfileSettings
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(342, 592);
            Controls.Add(btnCancel);
            Controls.Add(btnSaveUserProfile);
            Controls.Add(txtSystemPrompt);
            Controls.Add(lblSystemPrompt);
            Controls.Add(txtPersona);
            Controls.Add(lblPersona);
            Controls.Add(rdoFemale);
            Controls.Add(rdoMale);
            Controls.Add(lblGender);
            Controls.Add(txtNickName);
            Controls.Add(lblNickName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FrmUserProfileSettings";
            StartPosition = FormStartPosition.CenterParent;
            Text = "用户配置窗口";
            Load += FrmUserProfileSettings_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtSystemPrompt;
        private Label lblSystemPrompt;
        private TextBox txtPersona;
        private Label lblPersona;
        private RadioButton rdoFemale;
        private RadioButton rdoMale;
        private Label lblGender;
        private TextBox txtNickName;
        private Label lblNickName;
        private Button btnCancel;
        private Button btnSaveUserProfile;
    }
}