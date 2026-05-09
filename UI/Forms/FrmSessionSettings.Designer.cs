namespace ChatAI.UI.Forms
{
    partial class FrmSessionSettings
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
            lblContextLength = new Label();
            txtContextLength = new TextBox();
            txtMaxTokens = new TextBox();
            lblMaxTokens = new Label();
            grpBasicSettings = new GroupBox();
            lblNoRole = new Label();
            chkNoRole = new CheckBox();
            lblTemperatureValue = new Label();
            trkTemperature = new TrackBar();
            lblTemperature = new Label();
            grpMemorySettings = new GroupBox();
            txtMemoryPrompt = new TextBox();
            lblMemoryPrompt = new Label();
            txtAutoMemoryCount = new TextBox();
            lblAutoMemoryCount = new Label();
            btnSaveSessionSettings = new Button();
            grpBasicSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkTemperature).BeginInit();
            grpMemorySettings.SuspendLayout();
            SuspendLayout();
            // 
            // lblContextLength
            // 
            lblContextLength.AutoSize = true;
            lblContextLength.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblContextLength.Location = new Point(50, 35);
            lblContextLength.Name = "lblContextLength";
            lblContextLength.Size = new Size(106, 21);
            lblContextLength.TabIndex = 0;
            lblContextLength.Text = "上下文长度：";
            // 
            // txtContextLength
            // 
            txtContextLength.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtContextLength.Location = new Point(159, 31);
            txtContextLength.Name = "txtContextLength";
            txtContextLength.Size = new Size(55, 29);
            txtContextLength.TabIndex = 1;
            txtContextLength.Text = "20";
            // 
            // txtMaxTokens
            // 
            txtMaxTokens.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtMaxTokens.Location = new Point(159, 76);
            txtMaxTokens.Name = "txtMaxTokens";
            txtMaxTokens.Size = new Size(55, 29);
            txtMaxTokens.TabIndex = 3;
            txtMaxTokens.Text = "4096";
            // 
            // lblMaxTokens
            // 
            lblMaxTokens.AutoSize = true;
            lblMaxTokens.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblMaxTokens.Location = new Point(13, 80);
            lblMaxTokens.Name = "lblMaxTokens";
            lblMaxTokens.Size = new Size(144, 21);
            lblMaxTokens.TabIndex = 2;
            lblMaxTokens.Text = "最大上行Tokens：";
            // 
            // grpBasicSettings
            // 
            grpBasicSettings.Controls.Add(lblNoRole);
            grpBasicSettings.Controls.Add(chkNoRole);
            grpBasicSettings.Controls.Add(lblTemperatureValue);
            grpBasicSettings.Controls.Add(trkTemperature);
            grpBasicSettings.Controls.Add(lblTemperature);
            grpBasicSettings.Controls.Add(lblContextLength);
            grpBasicSettings.Controls.Add(txtMaxTokens);
            grpBasicSettings.Controls.Add(txtContextLength);
            grpBasicSettings.Controls.Add(lblMaxTokens);
            grpBasicSettings.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            grpBasicSettings.Location = new Point(12, 12);
            grpBasicSettings.Name = "grpBasicSettings";
            grpBasicSettings.Size = new Size(303, 300);
            grpBasicSettings.TabIndex = 4;
            grpBasicSettings.TabStop = false;
            grpBasicSettings.Text = "基础设置";
            // 
            // lblNoRole
            // 
            lblNoRole.AutoSize = true;
            lblNoRole.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblNoRole.Location = new Point(19, 231);
            lblNoRole.Name = "lblNoRole";
            lblNoRole.Size = new Size(280, 17);
            lblNoRole.TabIndex = 8;
            lblNoRole.Text = "(勾选纯净模式后，将会禁用所有用户和AI角色设定)";
            // 
            // chkNoRole
            // 
            chkNoRole.AutoSize = true;
            chkNoRole.Location = new Point(26, 205);
            chkNoRole.Name = "chkNoRole";
            chkNoRole.Size = new Size(93, 25);
            chkNoRole.TabIndex = 7;
            chkNoRole.Text = "纯净模式";
            chkNoRole.UseVisualStyleBackColor = true;
            // 
            // lblTemperatureValue
            // 
            lblTemperatureValue.AutoSize = true;
            lblTemperatureValue.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblTemperatureValue.Location = new Point(217, 155);
            lblTemperatureValue.Name = "lblTemperatureValue";
            lblTemperatureValue.Size = new Size(32, 21);
            lblTemperatureValue.TabIndex = 6;
            lblTemperatureValue.Text = "0.3";
            // 
            // trkTemperature
            // 
            trkTemperature.Location = new Point(13, 154);
            trkTemperature.Maximum = 20;
            trkTemperature.Name = "trkTemperature";
            trkTemperature.Size = new Size(193, 45);
            trkTemperature.TabIndex = 5;
            // 
            // lblTemperature
            // 
            lblTemperature.AutoSize = true;
            lblTemperature.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblTemperature.Location = new Point(13, 119);
            lblTemperature.Name = "lblTemperature";
            lblTemperature.Size = new Size(74, 21);
            lblTemperature.TabIndex = 4;
            lblTemperature.Text = "温度值：";
            // 
            // grpMemorySettings
            // 
            grpMemorySettings.Controls.Add(txtMemoryPrompt);
            grpMemorySettings.Controls.Add(lblMemoryPrompt);
            grpMemorySettings.Controls.Add(txtAutoMemoryCount);
            grpMemorySettings.Controls.Add(lblAutoMemoryCount);
            grpMemorySettings.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            grpMemorySettings.Location = new Point(335, 12);
            grpMemorySettings.Name = "grpMemorySettings";
            grpMemorySettings.Size = new Size(352, 300);
            grpMemorySettings.TabIndex = 5;
            grpMemorySettings.TabStop = false;
            grpMemorySettings.Text = "记忆设置";
            // 
            // txtMemoryPrompt
            // 
            txtMemoryPrompt.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtMemoryPrompt.Location = new Point(6, 109);
            txtMemoryPrompt.Multiline = true;
            txtMemoryPrompt.Name = "txtMemoryPrompt";
            txtMemoryPrompt.Size = new Size(340, 185);
            txtMemoryPrompt.TabIndex = 8;
            txtMemoryPrompt.Text = "根据人设、与用户对话历史，以AI第一人称“我”的角度总结与用户的重要交谈内容，语言精炼无废话（3-5句话），不添加虚构内容，仅保留可长期复用的核心记忆点，用于后续对话个性化参考。";
            // 
            // lblMemoryPrompt
            // 
            lblMemoryPrompt.AutoSize = true;
            lblMemoryPrompt.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblMemoryPrompt.Location = new Point(6, 85);
            lblMemoryPrompt.Name = "lblMemoryPrompt";
            lblMemoryPrompt.Size = new Size(138, 21);
            lblMemoryPrompt.TabIndex = 9;
            lblMemoryPrompt.Text = "记忆生成提示词：";
            // 
            // txtAutoMemoryCount
            // 
            txtAutoMemoryCount.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtAutoMemoryCount.Location = new Point(153, 32);
            txtAutoMemoryCount.Name = "txtAutoMemoryCount";
            txtAutoMemoryCount.Size = new Size(55, 29);
            txtAutoMemoryCount.TabIndex = 7;
            txtAutoMemoryCount.Text = "20";
            // 
            // lblAutoMemoryCount
            // 
            lblAutoMemoryCount.AutoSize = true;
            lblAutoMemoryCount.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lblAutoMemoryCount.Location = new Point(6, 35);
            lblAutoMemoryCount.Name = "lblAutoMemoryCount";
            lblAutoMemoryCount.Size = new Size(154, 21);
            lblAutoMemoryCount.TabIndex = 7;
            lblAutoMemoryCount.Text = "自动记忆触发条数：";
            // 
            // btnSaveSessionSettings
            // 
            btnSaveSessionSettings.BackColor = Color.DeepSkyBlue;
            btnSaveSessionSettings.FlatStyle = FlatStyle.Popup;
            btnSaveSessionSettings.Location = new Point(242, 323);
            btnSaveSessionSettings.Name = "btnSaveSessionSettings";
            btnSaveSessionSettings.Size = new Size(181, 34);
            btnSaveSessionSettings.TabIndex = 6;
            btnSaveSessionSettings.Text = "保存";
            btnSaveSessionSettings.UseVisualStyleBackColor = false;
            // 
            // FrmSessionSettings
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(708, 365);
            Controls.Add(btnSaveSessionSettings);
            Controls.Add(grpMemorySettings);
            Controls.Add(grpBasicSettings);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FrmSessionSettings";
            StartPosition = FormStartPosition.CenterParent;
            Text = "会话设置窗口";
            grpBasicSettings.ResumeLayout(false);
            grpBasicSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkTemperature).EndInit();
            grpMemorySettings.ResumeLayout(false);
            grpMemorySettings.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lblContextLength;
        private TextBox txtContextLength;
        private TextBox txtMaxTokens;
        private Label lblMaxTokens;
        private GroupBox grpBasicSettings;
        private TrackBar trkTemperature;
        private Label lblTemperature;
        private Label lblTemperatureValue;
        private GroupBox grpMemorySettings;
        private TextBox txtMemoryPrompt;
        private Label lblMemoryPrompt;
        private TextBox txtAutoMemoryCount;
        private Label lblAutoMemoryCount;
        private Button btnSaveSessionSettings;
        private CheckBox chkNoRole;
        private Label lblNoRole;
    }
}