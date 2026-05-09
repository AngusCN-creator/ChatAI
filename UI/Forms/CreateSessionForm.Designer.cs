namespace ChatAI.UI.Forms
{
    partial class CreateSessionForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码
        private void InitializeComponent()
        {
            lblNickName = new Label();
            txtNickName = new TextBox();
            lblGender = new Label();
            rdoMale = new RadioButton();
            rdoFemale = new RadioButton();
            lblRoleDesc = new Label();
            txtRoleDesc = new TextBox();
            lblTalkStyle = new Label();
            txtTalkStyle = new TextBox();
            lblHabit = new Label();
            txtHabit = new TextBox();
            lblOpening = new Label();
            txtOpening = new TextBox();
            btnCreate = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblNickName
            // 
            lblNickName.AutoSize = true;
            lblNickName.Font = new Font("微软雅黑", 11F);
            lblNickName.ForeColor = Color.FromArgb(51, 51, 51);
            lblNickName.Location = new Point(27, 51);
            lblNickName.Name = "lblNickName";
            lblNickName.Size = new Size(70, 20);
            lblNickName.TabIndex = 1;
            lblNickName.Text = "昵    称：";
            lblNickName.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtNickName
            // 
            txtNickName.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtNickName.ForeColor = Color.LightGray;
            txtNickName.Location = new Point(99, 52);
            txtNickName.Name = "txtNickName";
            txtNickName.Size = new Size(300, 29);
            txtNickName.TabIndex = 2;
            txtNickName.Text = "请输入角色昵称";
            txtNickName.Enter += Txt_GotFocus;
            txtNickName.Leave += Txt_LostFocus;
            // 
            // lblGender
            // 
            lblGender.AutoSize = true;
            lblGender.Font = new Font("微软雅黑", 11F);
            lblGender.ForeColor = Color.FromArgb(51, 51, 51);
            lblGender.Location = new Point(27, 111);
            lblGender.Name = "lblGender";
            lblGender.Size = new Size(70, 20);
            lblGender.TabIndex = 3;
            lblGender.Text = "性    别：";
            lblGender.TextAlign = ContentAlignment.MiddleRight;
            // 
            // rdoMale
            // 
            rdoMale.AutoSize = true;
            rdoMale.Checked = true;
            rdoMale.Font = new Font("微软雅黑", 11F);
            rdoMale.Location = new Point(119, 109);
            rdoMale.Name = "rdoMale";
            rdoMale.Size = new Size(53, 24);
            rdoMale.TabIndex = 4;
            rdoMale.TabStop = true;
            rdoMale.Text = "男♂";
            // 
            // rdoFemale
            // 
            rdoFemale.AutoSize = true;
            rdoFemale.Font = new Font("微软雅黑", 11F);
            rdoFemale.Location = new Point(197, 109);
            rdoFemale.Name = "rdoFemale";
            rdoFemale.Size = new Size(53, 24);
            rdoFemale.TabIndex = 5;
            rdoFemale.Text = "女♀";
            // 
            // lblRoleDesc
            // 
            lblRoleDesc.AutoSize = true;
            lblRoleDesc.Font = new Font("微软雅黑", 11F);
            lblRoleDesc.ForeColor = Color.FromArgb(51, 51, 51);
            lblRoleDesc.Location = new Point(14, 161);
            lblRoleDesc.Name = "lblRoleDesc";
            lblRoleDesc.Size = new Size(84, 20);
            lblRoleDesc.TabIndex = 6;
            lblRoleDesc.Text = "人设设定：";
            lblRoleDesc.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtRoleDesc
            // 
            txtRoleDesc.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtRoleDesc.ForeColor = Color.LightGray;
            txtRoleDesc.Location = new Point(99, 161);
            txtRoleDesc.Multiline = true;
            txtRoleDesc.Name = "txtRoleDesc";
            txtRoleDesc.Size = new Size(300, 94);
            txtRoleDesc.TabIndex = 7;
            txtRoleDesc.Text = "请输入人设设定（性格、背景等）";
            txtRoleDesc.Enter += Txt_GotFocus;
            txtRoleDesc.Leave += Txt_LostFocus;
            // 
            // lblTalkStyle
            // 
            lblTalkStyle.AutoSize = true;
            lblTalkStyle.Font = new Font("微软雅黑", 11F);
            lblTalkStyle.ForeColor = Color.FromArgb(51, 51, 51);
            lblTalkStyle.Location = new Point(14, 261);
            lblTalkStyle.Name = "lblTalkStyle";
            lblTalkStyle.Size = new Size(84, 20);
            lblTalkStyle.TabIndex = 8;
            lblTalkStyle.Text = "表达风格：";
            lblTalkStyle.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtTalkStyle
            // 
            txtTalkStyle.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtTalkStyle.ForeColor = Color.LightGray;
            txtTalkStyle.Location = new Point(99, 261);
            txtTalkStyle.Multiline = true;
            txtTalkStyle.Name = "txtTalkStyle";
            txtTalkStyle.Size = new Size(300, 74);
            txtTalkStyle.TabIndex = 9;
            txtTalkStyle.Text = "请输入表达风格（语言特点等）";
            txtTalkStyle.Enter += Txt_GotFocus;
            txtTalkStyle.Leave += Txt_LostFocus;
            // 
            // lblHabit
            // 
            lblHabit.AutoSize = true;
            lblHabit.Font = new Font("微软雅黑", 11F);
            lblHabit.ForeColor = Color.FromArgb(51, 51, 51);
            lblHabit.Location = new Point(14, 341);
            lblHabit.Name = "lblHabit";
            lblHabit.Size = new Size(84, 20);
            lblHabit.TabIndex = 10;
            lblHabit.Text = "习惯用语：";
            lblHabit.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtHabit
            // 
            txtHabit.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtHabit.ForeColor = Color.LightGray;
            txtHabit.Location = new Point(99, 341);
            txtHabit.Multiline = true;
            txtHabit.Name = "txtHabit";
            txtHabit.Size = new Size(300, 74);
            txtHabit.TabIndex = 11;
            txtHabit.Text = "请输入习惯用语（口头禅等）";
            txtHabit.Enter += Txt_GotFocus;
            txtHabit.Leave += Txt_LostFocus;
            // 
            // lblOpening
            // 
            lblOpening.AutoSize = true;
            lblOpening.Font = new Font("微软雅黑", 11F);
            lblOpening.ForeColor = Color.FromArgb(51, 51, 51);
            lblOpening.Location = new Point(29, 421);
            lblOpening.Name = "lblOpening";
            lblOpening.Size = new Size(69, 20);
            lblOpening.TabIndex = 12;
            lblOpening.Text = "开场白：";
            lblOpening.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtOpening
            // 
            txtOpening.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            txtOpening.ForeColor = Color.LightGray;
            txtOpening.Location = new Point(99, 421);
            txtOpening.Multiline = true;
            txtOpening.Name = "txtOpening";
            txtOpening.Size = new Size(300, 80);
            txtOpening.TabIndex = 13;
            txtOpening.Text = "请输入开场白（例如：好久不见！）";
            txtOpening.Enter += Txt_GotFocus;
            txtOpening.Leave += Txt_LostFocus;
            // 
            // btnCreate
            // 
            btnCreate.BackColor = Color.FromArgb(0, 122, 204);
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.FlatStyle = FlatStyle.Flat;
            btnCreate.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            btnCreate.ForeColor = Color.White;
            btnCreate.Location = new Point(57, 568);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new Size(140, 45);
            btnCreate.TabIndex = 14;
            btnCreate.Text = "创建";
            btnCreate.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(220, 220, 220);
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("微软雅黑", 11F);
            btnCancel.Location = new Point(217, 568);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(140, 45);
            btnCancel.TabIndex = 15;
            btnCancel.Text = "取消";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // CreateSessionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 245, 245);
            ClientSize = new Size(423, 680);
            Controls.Add(btnCancel);
            Controls.Add(btnCreate);
            Controls.Add(txtOpening);
            Controls.Add(lblOpening);
            Controls.Add(txtHabit);
            Controls.Add(lblHabit);
            Controls.Add(txtTalkStyle);
            Controls.Add(lblTalkStyle);
            Controls.Add(txtRoleDesc);
            Controls.Add(lblRoleDesc);
            Controls.Add(rdoFemale);
            Controls.Add(rdoMale);
            Controls.Add(lblGender);
            Controls.Add(txtNickName);
            Controls.Add(lblNickName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateSessionForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "创建新会话💬";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label lblNickName;
        private TextBox txtNickName;
        private Label lblGender;
        private RadioButton rdoMale;
        private RadioButton rdoFemale;
        private Label lblRoleDesc;
        private TextBox txtRoleDesc;
        private Label lblTalkStyle;
        private TextBox txtTalkStyle;
        private Label lblHabit;
        private TextBox txtHabit;
        private Label lblOpening;
        private TextBox txtOpening;
        private Button btnCreate;
        private Button btnCancel;
    }
}