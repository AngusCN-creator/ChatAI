namespace ChatAI.UI.Forms
{
    partial class FrmChatMain
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
            components = new System.ComponentModel.Container();
            ms_MainMenuBar = new MenuStrip();
            tsmi_MainMenu_APIConfig = new ToolStripMenuItem();
            tsmi_MainMenu_SessionSetting = new ToolStripMenuItem();
            tsmi_UserIdentitySetting = new ToolStripMenuItem();
            tsmi_AiMemorySettings = new ToolStripMenuItem();
            tsmi_MainMenu_Logout = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            sessionListuc1 = new ChatControl.Controls.SessionListUC();
            chatMainControl1 = new ChatControl.Controls.ChatMainControl();
            toolMenuInfo = new ToolTip(components);
            ms_MainMenuBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // ms_MainMenuBar
            // 
            ms_MainMenuBar.AutoSize = false;
            ms_MainMenuBar.Dock = DockStyle.Left;
            ms_MainMenuBar.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            ms_MainMenuBar.ImageScalingSize = new Size(0, 0);
            ms_MainMenuBar.Items.AddRange(new ToolStripItem[] { tsmi_MainMenu_APIConfig, tsmi_MainMenu_SessionSetting, tsmi_UserIdentitySetting, tsmi_AiMemorySettings, tsmi_MainMenu_Logout });
            ms_MainMenuBar.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
            ms_MainMenuBar.Location = new Point(0, 0);
            ms_MainMenuBar.Name = "ms_MainMenuBar";
            ms_MainMenuBar.ShowItemToolTips = true;
            ms_MainMenuBar.Size = new Size(35, 604);
            ms_MainMenuBar.TabIndex = 0;
            ms_MainMenuBar.Text = "menuStrip1";
            // 
            // tsmi_MainMenu_APIConfig
            // 
            tsmi_MainMenu_APIConfig.AutoSize = false;
            tsmi_MainMenu_APIConfig.Name = "tsmi_MainMenu_APIConfig";
            tsmi_MainMenu_APIConfig.Size = new Size(28, 40);
            tsmi_MainMenu_APIConfig.Text = "🔧";
            tsmi_MainMenu_APIConfig.TextImageRelation = TextImageRelation.TextBeforeImage;
            tsmi_MainMenu_APIConfig.ToolTipText = "API设置";
            tsmi_MainMenu_APIConfig.Click += tsmi_MainMenu_APIConfig_Click;
            // 
            // tsmi_MainMenu_SessionSetting
            // 
            tsmi_MainMenu_SessionSetting.AutoSize = false;
            tsmi_MainMenu_SessionSetting.Name = "tsmi_MainMenu_SessionSetting";
            tsmi_MainMenu_SessionSetting.Size = new Size(28, 40);
            tsmi_MainMenu_SessionSetting.Text = "🗨️";
            tsmi_MainMenu_SessionSetting.TextImageRelation = TextImageRelation.TextBeforeImage;
            tsmi_MainMenu_SessionSetting.ToolTipText = "会话设置";
            // 
            // tsmi_UserIdentitySetting
            // 
            tsmi_UserIdentitySetting.AutoSize = false;
            tsmi_UserIdentitySetting.Name = "tsmi_UserIdentitySetting";
            tsmi_UserIdentitySetting.Size = new Size(28, 40);
            tsmi_UserIdentitySetting.Text = "👤";
            tsmi_UserIdentitySetting.ToolTipText = "用户信息设置";
            tsmi_UserIdentitySetting.Click += tsmi_UserIdentitySetting_Click;
            // 
            // tsmi_AiMemorySettings
            // 
            tsmi_AiMemorySettings.AutoSize = false;
            tsmi_AiMemorySettings.Name = "tsmi_AiMemorySettings";
            tsmi_AiMemorySettings.Size = new Size(28, 40);
            tsmi_AiMemorySettings.Text = "💭";
            tsmi_AiMemorySettings.ToolTipText = "记忆设置";
            // 
            // tsmi_MainMenu_Logout
            // 
            tsmi_MainMenu_Logout.AutoSize = false;
            tsmi_MainMenu_Logout.Name = "tsmi_MainMenu_Logout";
            tsmi_MainMenu_Logout.Size = new Size(28, 40);
            tsmi_MainMenu_Logout.Text = "❌";
            tsmi_MainMenu_Logout.ToolTipText = "注销";
            tsmi_MainMenu_Logout.Click += tsmi_MainMenu_Logout_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.BackColor = SystemColors.Control;
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(35, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(sessionListuc1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(chatMainControl1);
            splitContainer1.Size = new Size(835, 604);
            splitContainer1.SplitterDistance = 170;
            splitContainer1.TabIndex = 1;
            // 
            // sessionListuc1
            // 
            sessionListuc1.BackColor = Color.Gainsboro;
            sessionListuc1.Dock = DockStyle.Fill;
            sessionListuc1.Location = new Point(0, 0);
            sessionListuc1.Name = "sessionListuc1";
            sessionListuc1.Size = new Size(170, 604);
            sessionListuc1.TabIndex = 0;
            // 
            // chatMainControl1
            // 
            chatMainControl1.BackColor = Color.Gainsboro;
            chatMainControl1.Dock = DockStyle.Fill;
            chatMainControl1.Location = new Point(0, 0);
            chatMainControl1.Name = "chatMainControl1";
            chatMainControl1.Size = new Size(661, 604);
            chatMainControl1.TabIndex = 0;
            // 
            // FrmChatMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Gainsboro;
            ClientSize = new Size(870, 604);
            Controls.Add(splitContainer1);
            Controls.Add(ms_MainMenuBar);
            Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MainMenuStrip = ms_MainMenuBar;
            Name = "FrmChatMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FrmChatMain";
            FormClosing += FrmChatMain_FormClosing;
            ms_MainMenuBar.ResumeLayout(false);
            ms_MainMenuBar.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private MenuStrip ms_MainMenuBar;
        private ToolStripMenuItem tsmi_AiMemorySettings;
        private ToolStripMenuItem tsmi_UserIdentitySetting;
        private SplitContainer splitContainer1;
        private ChatControl.Controls.SessionListUC sessionListuc1;
        private ChatControl.Controls.ChatMainControl chatMainControl1;
        private ToolTip toolMenuInfo;
        private ToolStripMenuItem tsmi_MainMenu_APIConfig;
        private ToolStripMenuItem tsmi_MainMenu_SessionSetting;
        private ToolStripMenuItem tsmi_MainMenu_Logout;
    }
}