namespace ChatAI.UI.Forms
{
    partial class FrmLogin
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControlMain = new TabControl();
            tabPageLogin = new TabPage();
            lblPassword = new Label();
            lblUserName = new Label();
            btnLogin = new Button();
            chkRememberPwd = new CheckBox();
            txtPassword = new TextBox();
            txtUsername = new TextBox();
            tabPageRegister = new TabPage();
            lblRegConfirmPwd = new Label();
            lblRegPassword = new Label();
            lblRegUsername = new Label();
            btnRegister = new Button();
            txtRegConfirmPwd = new TextBox();
            txtRegPassword = new TextBox();
            txtRegUsername = new TextBox();
            tabPageModifyPwd = new TabPage();
            lblConfirmNewPwd = new Label();
            txtConfirmNewPwd = new TextBox();
            lblNewPwd = new Label();
            lblOldPwd = new Label();
            lblModifyUsername = new Label();
            btnModifyPwd = new Button();
            txtNewPwd = new TextBox();
            txtOldPwd = new TextBox();
            txtModifyUsername = new TextBox();
            tabControlMain.SuspendLayout();
            tabPageLogin.SuspendLayout();
            tabPageRegister.SuspendLayout();
            tabPageModifyPwd.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabPageLogin);
            tabControlMain.Controls.Add(tabPageRegister);
            tabControlMain.Controls.Add(tabPageModifyPwd);
            tabControlMain.Location = new Point(0, -2);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(346, 311);
            tabControlMain.TabIndex = 0;
            // 
            // tabPageLogin
            // 
            tabPageLogin.Controls.Add(lblPassword);
            tabPageLogin.Controls.Add(lblUserName);
            tabPageLogin.Controls.Add(btnLogin);
            tabPageLogin.Controls.Add(chkRememberPwd);
            tabPageLogin.Controls.Add(txtPassword);
            tabPageLogin.Controls.Add(txtUsername);
            tabPageLogin.Location = new Point(4, 26);
            tabPageLogin.Name = "tabPageLogin";
            tabPageLogin.Padding = new Padding(3);
            tabPageLogin.Size = new Size(338, 281);
            tabPageLogin.TabIndex = 0;
            tabPageLogin.Text = " 登录";
            tabPageLogin.UseVisualStyleBackColor = true;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(50, 72);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(32, 17);
            lblPassword.TabIndex = 5;
            lblPassword.Text = "密码";
            // 
            // lblUserName
            // 
            lblUserName.AutoSize = true;
            lblUserName.Location = new Point(50, 22);
            lblUserName.Name = "lblUserName";
            lblUserName.Size = new Size(44, 17);
            lblUserName.TabIndex = 4;
            lblUserName.Text = "用户名";
            // 
            // btnLogin
            // 
            btnLogin.FlatAppearance.BorderColor = SystemColors.ActiveBorder;
            btnLogin.Location = new Point(50, 171);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(225, 35);
            btnLogin.TabIndex = 3;
            btnLogin.Text = "登录";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // chkRememberPwd
            // 
            chkRememberPwd.AutoSize = true;
            chkRememberPwd.Location = new Point(182, 120);
            chkRememberPwd.Name = "chkRememberPwd";
            chkRememberPwd.Size = new Size(75, 21);
            chkRememberPwd.TabIndex = 2;
            chkRememberPwd.Text = "记住密码";
            chkRememberPwd.UseVisualStyleBackColor = true;
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(50, 91);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(225, 23);
            txtPassword.TabIndex = 1;
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(50, 41);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(225, 23);
            txtUsername.TabIndex = 0;
            // 
            // tabPageRegister
            // 
            tabPageRegister.Controls.Add(lblRegConfirmPwd);
            tabPageRegister.Controls.Add(lblRegPassword);
            tabPageRegister.Controls.Add(lblRegUsername);
            tabPageRegister.Controls.Add(btnRegister);
            tabPageRegister.Controls.Add(txtRegConfirmPwd);
            tabPageRegister.Controls.Add(txtRegPassword);
            tabPageRegister.Controls.Add(txtRegUsername);
            tabPageRegister.Location = new Point(4, 26);
            tabPageRegister.Name = "tabPageRegister";
            tabPageRegister.Padding = new Padding(3);
            tabPageRegister.Size = new Size(338, 281);
            tabPageRegister.TabIndex = 1;
            tabPageRegister.Text = " 注册";
            tabPageRegister.UseVisualStyleBackColor = true;
            // 
            // lblRegConfirmPwd
            // 
            lblRegConfirmPwd.AutoSize = true;
            lblRegConfirmPwd.Location = new Point(53, 123);
            lblRegConfirmPwd.Name = "lblRegConfirmPwd";
            lblRegConfirmPwd.Size = new Size(56, 17);
            lblRegConfirmPwd.TabIndex = 6;
            lblRegConfirmPwd.Text = "重复密码";
            // 
            // lblRegPassword
            // 
            lblRegPassword.AutoSize = true;
            lblRegPassword.Location = new Point(53, 71);
            lblRegPassword.Name = "lblRegPassword";
            lblRegPassword.Size = new Size(32, 17);
            lblRegPassword.TabIndex = 5;
            lblRegPassword.Text = "密码";
            // 
            // lblRegUsername
            // 
            lblRegUsername.AutoSize = true;
            lblRegUsername.Location = new Point(53, 20);
            lblRegUsername.Name = "lblRegUsername";
            lblRegUsername.Size = new Size(44, 17);
            lblRegUsername.TabIndex = 4;
            lblRegUsername.Text = "用户名";
            // 
            // btnRegister
            // 
            btnRegister.Location = new Point(53, 197);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(221, 36);
            btnRegister.TabIndex = 3;
            btnRegister.Text = "注册";
            btnRegister.UseVisualStyleBackColor = true;
            btnRegister.Click += btnRegister_Click;
            // 
            // txtRegConfirmPwd
            // 
            txtRegConfirmPwd.Location = new Point(53, 143);
            txtRegConfirmPwd.Name = "txtRegConfirmPwd";
            txtRegConfirmPwd.PasswordChar = '*';
            txtRegConfirmPwd.Size = new Size(221, 23);
            txtRegConfirmPwd.TabIndex = 2;
            // 
            // txtRegPassword
            // 
            txtRegPassword.Location = new Point(53, 91);
            txtRegPassword.Name = "txtRegPassword";
            txtRegPassword.PasswordChar = '*';
            txtRegPassword.Size = new Size(221, 23);
            txtRegPassword.TabIndex = 1;
            // 
            // txtRegUsername
            // 
            txtRegUsername.Location = new Point(53, 40);
            txtRegUsername.Name = "txtRegUsername";
            txtRegUsername.Size = new Size(221, 23);
            txtRegUsername.TabIndex = 0;
            // 
            // tabPageModifyPwd
            // 
            tabPageModifyPwd.Controls.Add(lblConfirmNewPwd);
            tabPageModifyPwd.Controls.Add(txtConfirmNewPwd);
            tabPageModifyPwd.Controls.Add(lblNewPwd);
            tabPageModifyPwd.Controls.Add(lblOldPwd);
            tabPageModifyPwd.Controls.Add(lblModifyUsername);
            tabPageModifyPwd.Controls.Add(btnModifyPwd);
            tabPageModifyPwd.Controls.Add(txtNewPwd);
            tabPageModifyPwd.Controls.Add(txtOldPwd);
            tabPageModifyPwd.Controls.Add(txtModifyUsername);
            tabPageModifyPwd.Location = new Point(4, 26);
            tabPageModifyPwd.Name = "tabPageModifyPwd";
            tabPageModifyPwd.Size = new Size(338, 281);
            tabPageModifyPwd.TabIndex = 2;
            tabPageModifyPwd.Text = "修改密码";
            tabPageModifyPwd.UseVisualStyleBackColor = true;
            // 
            // lblConfirmNewPwd
            // 
            lblConfirmNewPwd.AutoSize = true;
            lblConfirmNewPwd.Location = new Point(56, 147);
            lblConfirmNewPwd.Name = "lblConfirmNewPwd";
            lblConfirmNewPwd.Size = new Size(68, 17);
            lblConfirmNewPwd.TabIndex = 8;
            lblConfirmNewPwd.Text = "确认新密码";
            // 
            // txtConfirmNewPwd
            // 
            txtConfirmNewPwd.Location = new Point(55, 166);
            txtConfirmNewPwd.Name = "txtConfirmNewPwd";
            txtConfirmNewPwd.PasswordChar = '*';
            txtConfirmNewPwd.Size = new Size(212, 23);
            txtConfirmNewPwd.TabIndex = 3;
            // 
            // lblNewPwd
            // 
            lblNewPwd.AutoSize = true;
            lblNewPwd.Location = new Point(55, 102);
            lblNewPwd.Name = "lblNewPwd";
            lblNewPwd.Size = new Size(44, 17);
            lblNewPwd.TabIndex = 6;
            lblNewPwd.Text = "新密码";
            // 
            // lblOldPwd
            // 
            lblOldPwd.AutoSize = true;
            lblOldPwd.Location = new Point(55, 57);
            lblOldPwd.Name = "lblOldPwd";
            lblOldPwd.Size = new Size(44, 17);
            lblOldPwd.TabIndex = 5;
            lblOldPwd.Text = "旧密码";
            // 
            // lblModifyUsername
            // 
            lblModifyUsername.AutoSize = true;
            lblModifyUsername.Location = new Point(55, 11);
            lblModifyUsername.Name = "lblModifyUsername";
            lblModifyUsername.Size = new Size(44, 17);
            lblModifyUsername.TabIndex = 4;
            lblModifyUsername.Text = "用户名";
            // 
            // btnModifyPwd
            // 
            btnModifyPwd.Location = new Point(55, 216);
            btnModifyPwd.Name = "btnModifyPwd";
            btnModifyPwd.Size = new Size(212, 35);
            btnModifyPwd.TabIndex = 4;
            btnModifyPwd.Text = "确认修改";
            btnModifyPwd.UseVisualStyleBackColor = true;
            btnModifyPwd.Click += btnModifyPwd_Click;
            // 
            // txtNewPwd
            // 
            txtNewPwd.Location = new Point(55, 121);
            txtNewPwd.Name = "txtNewPwd";
            txtNewPwd.PasswordChar = '*';
            txtNewPwd.Size = new Size(212, 23);
            txtNewPwd.TabIndex = 2;
            // 
            // txtOldPwd
            // 
            txtOldPwd.Location = new Point(55, 75);
            txtOldPwd.Name = "txtOldPwd";
            txtOldPwd.PasswordChar = '*';
            txtOldPwd.Size = new Size(212, 23);
            txtOldPwd.TabIndex = 1;
            // 
            // txtModifyUsername
            // 
            txtModifyUsername.Location = new Point(55, 31);
            txtModifyUsername.Name = "txtModifyUsername";
            txtModifyUsername.Size = new Size(212, 23);
            txtModifyUsername.TabIndex = 0;
            // 
            // FrmLogin
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(344, 306);
            Controls.Add(tabControlMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "登录界面";
            tabControlMain.ResumeLayout(false);
            tabPageLogin.ResumeLayout(false);
            tabPageLogin.PerformLayout();
            tabPageRegister.ResumeLayout(false);
            tabPageRegister.PerformLayout();
            tabPageModifyPwd.ResumeLayout(false);
            tabPageModifyPwd.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControlMain;
        private TabPage tabPageLogin;
        private TabPage tabPageRegister;
        private TabPage tabPageModifyPwd;
        private Button btnLogin;
        private CheckBox chkRememberPwd;
        private TextBox txtPassword;
        private TextBox txtUsername;
        private Label lblPassword;
        private Label lblUserName;
        private Button btnRegister;
        private TextBox txtRegConfirmPwd;
        private TextBox txtRegPassword;
        private TextBox txtRegUsername;
        private Label lblRegConfirmPwd;
        private Label lblRegPassword;
        private Label lblRegUsername;
        private TextBox txtNewPwd;
        private TextBox txtOldPwd;
        private TextBox txtModifyUsername;
        private Label lblNewPwd;
        private Label lblOldPwd;
        private Label lblModifyUsername;
        private Button btnModifyPwd;
        private Label lblConfirmNewPwd;
        private TextBox txtConfirmNewPwd;
    }
}
