namespace ChatAI.UI.Forms
{
    partial class FrmApiConfig
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
            cboProvider = new ComboBox();
            txtBaseUrl = new TextBox();
            cboModel = new ComboBox();
            txtApiKey = new TextBox();
            lblProvider = new Label();
            lblBaseUrl = new Label();
            lblApiKey = new Label();
            lblModel = new Label();
            btnTestConnect = new Button();
            btnShowApiKey = new Button();
            btnSave = new Button();
            btnLoadModels = new Button();
            groupBox_CloudApiConfig = new GroupBox();
            groupBox_LocalModelConfig = new GroupBox();
            numericUpDown1 = new NumericUpDown();
            lbl_GpuMemLimit = new Label();
            cbo_GpuAccelMode = new ComboBox();
            lbl_GpuAccelMode = new Label();
            btn_BrowseModel = new Button();
            txt_LocalModelPath = new TextBox();
            lbl_LocalModelPath = new Label();
            chk_EnableLocalModel = new CheckBox();
            groupBox_CloudApiConfig.SuspendLayout();
            groupBox_LocalModelConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            SuspendLayout();
            // 
            // cboProvider
            // 
            cboProvider.FormattingEnabled = true;
            cboProvider.Location = new Point(21, 48);
            cboProvider.Name = "cboProvider";
            cboProvider.Size = new Size(230, 25);
            cboProvider.TabIndex = 0;
            cboProvider.SelectedIndexChanged += cboProvider_SelectedIndexChanged;
            // 
            // txtBaseUrl
            // 
            txtBaseUrl.Location = new Point(21, 113);
            txtBaseUrl.Name = "txtBaseUrl";
            txtBaseUrl.Size = new Size(522, 23);
            txtBaseUrl.TabIndex = 1;
            // 
            // cboModel
            // 
            cboModel.FormattingEnabled = true;
            cboModel.Location = new Point(257, 48);
            cboModel.Name = "cboModel";
            cboModel.Size = new Size(265, 25);
            cboModel.TabIndex = 2;
            // 
            // txtApiKey
            // 
            txtApiKey.Location = new Point(21, 178);
            txtApiKey.Name = "txtApiKey";
            txtApiKey.PasswordChar = '*';
            txtApiKey.Size = new Size(522, 23);
            txtApiKey.TabIndex = 3;
            // 
            // lblProvider
            // 
            lblProvider.AutoSize = true;
            lblProvider.Location = new Point(21, 26);
            lblProvider.Name = "lblProvider";
            lblProvider.Size = new Size(80, 17);
            lblProvider.TabIndex = 4;
            lblProvider.Text = "接口服务商：";
            // 
            // lblBaseUrl
            // 
            lblBaseUrl.AutoSize = true;
            lblBaseUrl.Location = new Point(20, 90);
            lblBaseUrl.Name = "lblBaseUrl";
            lblBaseUrl.Size = new Size(68, 17);
            lblBaseUrl.TabIndex = 5;
            lblBaseUrl.Text = "接口地址：";
            // 
            // lblApiKey
            // 
            lblApiKey.AutoSize = true;
            lblApiKey.Location = new Point(21, 158);
            lblApiKey.Name = "lblApiKey";
            lblApiKey.Size = new Size(64, 17);
            lblApiKey.TabIndex = 6;
            lblApiKey.Text = "API Key：";
            // 
            // lblModel
            // 
            lblModel.AutoSize = true;
            lblModel.Location = new Point(257, 28);
            lblModel.Name = "lblModel";
            lblModel.Size = new Size(68, 17);
            lblModel.TabIndex = 7;
            lblModel.Text = "模型选择：";
            // 
            // btnTestConnect
            // 
            btnTestConnect.FlatStyle = FlatStyle.Popup;
            btnTestConnect.Location = new Point(549, 113);
            btnTestConnect.Name = "btnTestConnect";
            btnTestConnect.Size = new Size(75, 23);
            btnTestConnect.TabIndex = 8;
            btnTestConnect.Text = "测试连接";
            btnTestConnect.UseVisualStyleBackColor = true;
            btnTestConnect.Click += btnTestConnect_Click;
            // 
            // btnShowApiKey
            // 
            btnShowApiKey.FlatStyle = FlatStyle.Popup;
            btnShowApiKey.Location = new Point(549, 178);
            btnShowApiKey.Name = "btnShowApiKey";
            btnShowApiKey.Size = new Size(75, 23);
            btnShowApiKey.TabIndex = 9;
            btnShowApiKey.Text = "显示";
            btnShowApiKey.UseVisualStyleBackColor = true;
            btnShowApiKey.Click += btnShowApiKey_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(444, 358);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(213, 37);
            btnSave.TabIndex = 10;
            btnSave.Text = "保存配置";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnLoadModels
            // 
            btnLoadModels.FlatStyle = FlatStyle.Popup;
            btnLoadModels.Location = new Point(528, 48);
            btnLoadModels.Name = "btnLoadModels";
            btnLoadModels.Size = new Size(96, 25);
            btnLoadModels.TabIndex = 11;
            btnLoadModels.Text = "获取模型列表";
            btnLoadModels.UseVisualStyleBackColor = true;
            btnLoadModels.Click += btnLoadModels_Click;
            // 
            // groupBox_CloudApiConfig
            // 
            groupBox_CloudApiConfig.Controls.Add(btnLoadModels);
            groupBox_CloudApiConfig.Controls.Add(btnShowApiKey);
            groupBox_CloudApiConfig.Controls.Add(btnTestConnect);
            groupBox_CloudApiConfig.Controls.Add(lblModel);
            groupBox_CloudApiConfig.Controls.Add(lblApiKey);
            groupBox_CloudApiConfig.Controls.Add(lblBaseUrl);
            groupBox_CloudApiConfig.Controls.Add(lblProvider);
            groupBox_CloudApiConfig.Controls.Add(txtApiKey);
            groupBox_CloudApiConfig.Controls.Add(cboModel);
            groupBox_CloudApiConfig.Controls.Add(txtBaseUrl);
            groupBox_CloudApiConfig.Controls.Add(cboProvider);
            groupBox_CloudApiConfig.Location = new Point(15, 8);
            groupBox_CloudApiConfig.Name = "groupBox_CloudApiConfig";
            groupBox_CloudApiConfig.Size = new Size(642, 225);
            groupBox_CloudApiConfig.TabIndex = 12;
            groupBox_CloudApiConfig.TabStop = false;
            groupBox_CloudApiConfig.Text = "云端在线大模型 API 配置";
            // 
            // groupBox_LocalModelConfig
            // 
            groupBox_LocalModelConfig.Controls.Add(numericUpDown1);
            groupBox_LocalModelConfig.Controls.Add(lbl_GpuMemLimit);
            groupBox_LocalModelConfig.Controls.Add(cbo_GpuAccelMode);
            groupBox_LocalModelConfig.Controls.Add(lbl_GpuAccelMode);
            groupBox_LocalModelConfig.Controls.Add(btn_BrowseModel);
            groupBox_LocalModelConfig.Controls.Add(txt_LocalModelPath);
            groupBox_LocalModelConfig.Controls.Add(lbl_LocalModelPath);
            groupBox_LocalModelConfig.Location = new Point(15, 261);
            groupBox_LocalModelConfig.Name = "groupBox_LocalModelConfig";
            groupBox_LocalModelConfig.Size = new Size(642, 82);
            groupBox_LocalModelConfig.TabIndex = 13;
            groupBox_LocalModelConfig.TabStop = false;
            groupBox_LocalModelConfig.Text = "本地离线私有大模型配置";
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(451, 48);
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(173, 23);
            numericUpDown1.TabIndex = 6;
            // 
            // lbl_GpuMemLimit
            // 
            lbl_GpuMemLimit.AutoSize = true;
            lbl_GpuMemLimit.Location = new Point(451, 26);
            lbl_GpuMemLimit.Name = "lbl_GpuMemLimit";
            lbl_GpuMemLimit.Size = new Size(137, 17);
            lbl_GpuMemLimit.TabIndex = 5;
            lbl_GpuMemLimit.Text = "显存 / 内存上限 (MB)：";
            // 
            // cbo_GpuAccelMode
            // 
            cbo_GpuAccelMode.FormattingEnabled = true;
            cbo_GpuAccelMode.Location = new Point(245, 46);
            cbo_GpuAccelMode.Name = "cbo_GpuAccelMode";
            cbo_GpuAccelMode.Size = new Size(193, 25);
            cbo_GpuAccelMode.TabIndex = 4;
            // 
            // lbl_GpuAccelMode
            // 
            lbl_GpuAccelMode.AutoSize = true;
            lbl_GpuAccelMode.Location = new Point(245, 26);
            lbl_GpuAccelMode.Name = "lbl_GpuAccelMode";
            lbl_GpuAccelMode.Size = new Size(92, 17);
            lbl_GpuAccelMode.TabIndex = 3;
            lbl_GpuAccelMode.Text = "硬件加速模式：";
            // 
            // btn_BrowseModel
            // 
            btn_BrowseModel.Location = new Point(206, 46);
            btn_BrowseModel.Name = "btn_BrowseModel";
            btn_BrowseModel.Size = new Size(32, 23);
            btn_BrowseModel.TabIndex = 2;
            btn_BrowseModel.Text = "⚙";
            btn_BrowseModel.UseVisualStyleBackColor = true;
            btn_BrowseModel.Click += btn_BrowseModel_Click;
            // 
            // txt_LocalModelPath
            // 
            txt_LocalModelPath.Location = new Point(20, 46);
            txt_LocalModelPath.Name = "txt_LocalModelPath";
            txt_LocalModelPath.ReadOnly = true;
            txt_LocalModelPath.Size = new Size(181, 23);
            txt_LocalModelPath.TabIndex = 1;
            // 
            // lbl_LocalModelPath
            // 
            lbl_LocalModelPath.AutoSize = true;
            lbl_LocalModelPath.Location = new Point(23, 26);
            lbl_LocalModelPath.Name = "lbl_LocalModelPath";
            lbl_LocalModelPath.Size = new Size(92, 17);
            lbl_LocalModelPath.TabIndex = 0;
            lbl_LocalModelPath.Text = "模型文件路径：";
            // 
            // chk_EnableLocalModel
            // 
            chk_EnableLocalModel.AutoSize = true;
            chk_EnableLocalModel.Location = new Point(518, 243);
            chk_EnableLocalModel.Name = "chk_EnableLocalModel";
            chk_EnableLocalModel.Size = new Size(135, 21);
            chk_EnableLocalModel.TabIndex = 14;
            chk_EnableLocalModel.Text = "启用本地离线大模型";
            chk_EnableLocalModel.UseVisualStyleBackColor = true;
            chk_EnableLocalModel.CheckedChanged += chk_EnableLocalModel_CheckedChanged;
            // 
            // FrmApiConfig
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(678, 415);
            Controls.Add(chk_EnableLocalModel);
            Controls.Add(groupBox_LocalModelConfig);
            Controls.Add(groupBox_CloudApiConfig);
            Controls.Add(btnSave);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FrmApiConfig";
            StartPosition = FormStartPosition.CenterParent;
            Text = "API配置窗口";
            Load += FrmApiConfig_Load;
            groupBox_CloudApiConfig.ResumeLayout(false);
            groupBox_CloudApiConfig.PerformLayout();
            groupBox_LocalModelConfig.ResumeLayout(false);
            groupBox_LocalModelConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cboProvider;
        private TextBox txtBaseUrl;
        private ComboBox cboModel;
        private TextBox txtApiKey;
        private Label lblProvider;
        private Label lblBaseUrl;
        private Label lblApiKey;
        private Label lblModel;
        private Button btnTestConnect;
        private Button btnShowApiKey;
        private Button btnSave;
        private Button btnLoadModels;
        private GroupBox groupBox_CloudApiConfig;
        private GroupBox groupBox_LocalModelConfig;
        private TextBox txt_LocalModelPath;
        private Label lbl_LocalModelPath;
        private ComboBox cbo_GpuAccelMode;
        private Label lbl_GpuAccelMode;
        private Button btn_BrowseModel;
        private NumericUpDown numericUpDown1;
        private Label lbl_GpuMemLimit;
        private CheckBox chk_EnableLocalModel;
    }
}