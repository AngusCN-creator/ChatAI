using System;
using System.Windows.Forms;
using ChatAI.Data;

namespace ChatAI.UI.Forms
{
    public partial class FrmLogin : Form
    {
        public string LoginUsername => txtUsername.Text.Trim();

        // 仓储：只依赖接口，不依赖具体实现
        private readonly IUserRepository _userRepo;

        public FrmLogin()
        {
            InitializeComponent();

            try
            {
                SqliteDbHelper.InitDatabaseAndTables();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"数据库初始化失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 使用SQLite仓储
            _userRepo = new SqliteUserRepository();

            this.Load += FrmLogin_Load;
            BindEnterKeySubmit();
        }

        private void FrmLogin_Load(object? sender, EventArgs e)
        {
            // 加载记住密码 → 现在交给仓储
            var rememberAccount = _userRepo.GetRememberMeAccount();
            if (rememberAccount != null)
            {
                txtUsername.Text = rememberAccount.Account;
                txtPassword.Text = rememberAccount.Password;
                chkRememberPwd.Checked = true;
                txtPassword.Focus();
            }
        }

        private void BindEnterKeySubmit()
        {
            txtUsername.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnLogin.PerformClick(); };
            txtPassword.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnLogin.PerformClick(); };

            txtRegUsername.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnRegister.PerformClick(); };
            txtRegPassword.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnRegister.PerformClick(); };
            txtRegConfirmPwd.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnRegister.PerformClick(); };

            txtModifyUsername.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnModifyPwd.PerformClick(); };
            txtOldPwd.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnModifyPwd.PerformClick(); };
            txtNewPwd.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnModifyPwd.PerformClick(); };
            txtConfirmNewPwd.KeyPress += (s, e) => { if (e.KeyChar == (char)13) btnModifyPwd.PerformClick(); };
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(this, "用户名和密码不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = _userRepo.Login(username, password);
            if (!success)
            {
                MessageBox.Show(this, "用户名或密码错误！", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 记住密码 → 交给仓储
            _userRepo.UpdateRememberMe(username, chkRememberPwd.Checked);

            this.DialogResult = DialogResult.OK;
        }

        private void btnRegister_Click(object? sender, EventArgs e)
        {
            string username = txtRegUsername.Text.Trim();
            string pwd = txtRegPassword.Text.Trim();
            string confirmPwd = txtRegConfirmPwd.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pwd))
            {
                MessageBox.Show(this, "用户名和密码不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (username.Length > 20)
            {
                MessageBox.Show(this, "用户名长度不能超过 20 个字符！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRegUsername.Focus();
                return;
            }
            if (pwd.Length > 20)
            {
                MessageBox.Show(this, "密码长度不能超过 20 个字符！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRegPassword.Focus();
                return;
            }

            if (_userRepo.IsUsernameExists(username))
            {
                MessageBox.Show(this, "用户名已存在！", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (pwd != confirmPwd)
            {
                MessageBox.Show(this, "两次密码不一致！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = _userRepo.Register(username, pwd);
            if (ok)
            {
                MessageBox.Show(this, "注册成功！请登录", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtRegUsername.Clear();
                txtRegPassword.Clear();
                txtRegConfirmPwd.Clear();
                tabControlMain.SelectedTab = tabPageLogin;
                txtUsername.Text = username;
                txtPassword.Focus();
            }
            else
            {
                MessageBox.Show(this, "注册失败！", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnModifyPwd_Click(object? sender, EventArgs e)
        {
            string username = txtModifyUsername.Text.Trim();
            string oldPwd = txtOldPwd.Text.Trim();
            string newPwd = txtNewPwd.Text.Trim();
            string confirmNewPwd = txtConfirmNewPwd.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(oldPwd) ||
                string.IsNullOrWhiteSpace(newPwd) ||
                string.IsNullOrWhiteSpace(confirmNewPwd))
            {
                MessageBox.Show(this, "所有项都不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_userRepo.IsUsernameExists(username))
            {
                MessageBox.Show(this, "该用户不存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_userRepo.Login(username, oldPwd))
            {
                MessageBox.Show(this, "旧密码不正确！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPwd == oldPwd)
            {
                MessageBox.Show(this, "新密码不能与旧密码相同！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPwd != confirmNewPwd)
            {
                MessageBox.Show(this, "两次新密码不一致！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = _userRepo.ChangePassword(username, oldPwd, newPwd);
            if (ok)
            {
                MessageBox.Show(this, "密码修改成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControlMain.SelectedTab = tabPageLogin;
                txtUsername.Text = username;
                txtPassword.Focus();
                txtPassword.Clear();
                ClearModifyInputs();
            }
            else
            {
                MessageBox.Show(this, "修改失败！", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearModifyInputs()
        {
            txtModifyUsername.Clear();
            txtOldPwd.Clear();
            txtNewPwd.Clear();
            txtConfirmNewPwd.Clear();
        }
    }
}