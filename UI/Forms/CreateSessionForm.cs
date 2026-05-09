using ChatAI.Data;
using ChatControl.Controls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChatAI.UI.Forms
{
    public partial class CreateSessionForm : Form
    {
        // 新增：存储当前登录用户名
        private string? _loginUsername;

        /// <summary>
        /// 当前编辑的AI角色
        /// </summary>
        private AiCharacter? _editAiCharacter;

        /// <summary>
        /// 编辑模式
        /// </summary>
        public bool IsEditMode => _editAiCharacter != null;

        /// <summary>
        /// 新创建的AI角色信息（创建成功后填充）
        /// </summary>
        public AiCharacter? CreatedAiCharacter { get; private set; }

        #region 对外数据属性
        public string NickName
        {
            get
            {
                if (txtNickName.Text == "请输入角色昵称")
                    return string.Empty;
                return txtNickName.Text.Trim();
            }
        }

        public string Gender => rdoMale.Checked ? "男" : rdoFemale.Checked ? "女" : "未知";

        public string RoleDesc
        {
            get
            {
                if (txtRoleDesc.Text == "请输入人设设定（性格、背景等）")
                    return string.Empty;
                return txtRoleDesc.Text.Trim();
            }
        }

        public string TalkStyle
        {
            get
            {
                if (txtTalkStyle.Text == "请输入表达风格（语言特点等）")
                    return string.Empty;
                return txtTalkStyle.Text.Trim();
            }
        }

        public string Habit
        {
            get
            {
                if (txtHabit.Text == "请输入习惯用语（口头禅等）")
                    return string.Empty;
                return txtHabit.Text.Trim();
            }
        }

        public string Opening
        {
            get
            {
                if (txtOpening.Text == "请输入开场白（例如：好久不见！）")
                    return string.Empty;
                return txtOpening.Text.Trim();
            }
        }
        #endregion

        #region 构造函数
        public CreateSessionForm()
        {
            InitializeComponent();

            // 绑定事件
            Shown += CreateSessionForm_Shown;

            // 输入框焦点事件
            txtNickName.GotFocus += Txt_GotFocus;
            txtNickName.LostFocus += Txt_LostFocus;
            txtRoleDesc.GotFocus += Txt_GotFocus;
            txtRoleDesc.LostFocus += Txt_LostFocus;
            txtTalkStyle.GotFocus += Txt_GotFocus;
            txtTalkStyle.LostFocus += Txt_LostFocus;
            txtHabit.GotFocus += Txt_GotFocus;
            txtHabit.LostFocus += Txt_LostFocus;
            txtOpening.GotFocus += Txt_GotFocus;
            txtOpening.LostFocus += Txt_LostFocus;

            // 按钮事件
            btnCreate.Click += BtnCreate_Click;
            btnCancel.Click += (s, e) => Close();

            // 设置按钮圆角
            SetButtonRound(btnCreate, 8);
            SetButtonRound(btnCancel, 8);
        }
        #endregion
        // 新增：带用户名参数的构造函数
        public CreateSessionForm(string loginUsername) : this()
        {
            _loginUsername = loginUsername; // 赋值给私有字段
            // 可选：在窗口中显示用户名（例如标题栏）
            //this.Text = $"创建会话 - 当前用户：{loginUsername}";
        }
        
        // 新增：编辑模式构造函数
        public CreateSessionForm(string loginUsername, AiCharacter aiCharacter) : this(loginUsername)
        {
            _editAiCharacter = aiCharacter;
            Text = "编辑AI角色";
            btnCreate.Text = "保存修改";
        }
        #region 圆角按钮设置
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
        #endregion

        #region 窗体显示事件
        private void CreateSessionForm_Shown(object? sender, EventArgs e)
        {
            // 统一设置输入框样式
            SetTextBoxStyle(txtNickName, false);
            SetTextBoxStyle(txtRoleDesc, true);
            SetTextBoxStyle(txtTalkStyle, true);
            SetTextBoxStyle(txtHabit, true);
            SetTextBoxStyle(txtOpening, true);

            if (_editAiCharacter != null)
            {
                // 编辑模式：填充现有数据
                txtNickName.Text = _editAiCharacter.AiName ?? string.Empty;
                txtNickName.ForeColor = Color.Black;
                txtNickName.Font = new Font("微软雅黑", 12f, FontStyle.Regular);

                if (_editAiCharacter.AiGender == "女")
                {
                    rdoFemale.Checked = true;
                }
                else
                {
                    rdoMale.Checked = true;
                }
                
                txtRoleDesc.Text = _editAiCharacter.AiPersona ?? string.Empty;
                txtRoleDesc.ForeColor = Color.Black;
                txtRoleDesc.Font = new Font("微软雅黑", 12f, FontStyle.Regular);

                txtTalkStyle.Text = _editAiCharacter.AiStyle ?? string.Empty;
                txtTalkStyle.ForeColor = Color.Black;
                txtTalkStyle.Font = new Font("微软雅黑", 12f, FontStyle.Regular);

                txtHabit.Text = _editAiCharacter.AiHabit ?? string.Empty;
                txtHabit.ForeColor = Color.Black;
                txtHabit.Font = new Font("微软雅黑", 12f, FontStyle.Regular);

                txtOpening.Text = _editAiCharacter.AiOpening ?? string.Empty;
                txtOpening.ForeColor = Color.Black;
                txtOpening.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
            }
            else
            {
                // 新建模式：初始化占位符
                InitPlaceholder(txtNickName, "请输入角色昵称");
                InitPlaceholder(txtRoleDesc, "请输入人设设定（性格、背景等）");
                InitPlaceholder(txtTalkStyle, "请输入表达风格（语言特点等）");
                InitPlaceholder(txtHabit, "请输入习惯用语（口头禅等）");
                InitPlaceholder(txtOpening, "请输入开场白（例如：好久不见！）");
            }
            
            // 把焦点强行给取消按钮，避开所有文本框！
            btnCancel.Focus();
        }

        private void SetTextBoxStyle(TextBox txt, bool isMultiline)
        {
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = BackColor;
            txt.Padding = new Padding(0, 5, 0, 0);
            txt.Multiline = isMultiline;
            txt.ScrollBars = ScrollBars.None;
        }
        #endregion

        #region 占位符处理
        private void InitPlaceholder(TextBox txt, string placeholder)
        {
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                txt.Text = placeholder;
                txt.Font = new Font("微软雅黑", 12f, FontStyle.Italic);
                txt.ForeColor = Color.LightGray;
            }
        }

        private void Txt_GotFocus(object? sender, EventArgs e)
        {
            if (sender is not TextBox txt) return;

            string[] placeholders = {
                "请输入角色昵称",
                "请输入人设设定（性格、背景等）",
                "请输入表达风格（语言特点等）",
                "请输入习惯用语（口头禅等）",
                "请输入开场白（例如：好久不见！）"
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
                    nameof(txtNickName) => "请输入角色昵称",
                    nameof(txtRoleDesc) => "请输入人设设定（性格、背景等）",
                    nameof(txtTalkStyle) => "请输入表达风格（语言特点等）",
                    nameof(txtHabit) => "请输入习惯用语（口头禅等）",
                    nameof(txtOpening) => "请输入开场白（例如：好久不见！）",
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
        #endregion

        #region 创建按钮事件
        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            // 校验
            if (string.IsNullOrWhiteSpace(NickName))
            {
                MessageBox.Show("请输入角色昵称！");
                txtNickName.Focus();
                return;
            }

            // 👇 只加这一段：昵称最多10个字符
            if (NickName.Length > 10)
            {
                MessageBox.Show("角色昵称不能超过 10 个字符！");
                txtNickName.Focus();
                return;
            }

            UserRepository repo = new UserRepository();
            bool success;
            string error;
            AiCharacter? ai = null;

            if (_editAiCharacter != null)
            {
                // 编辑模式：更新现有角色
                _editAiCharacter.AiName = this.NickName;
                _editAiCharacter.AiGender = rdoMale.Checked ? "男" : "女";
                _editAiCharacter.AiPersona = this.RoleDesc;
                _editAiCharacter.AiStyle = this.TalkStyle;
                _editAiCharacter.AiHabit = this.Habit;
                _editAiCharacter.AiOpening = this.Opening;
                
                success = repo.UpdateAiCharacter(_editAiCharacter, out error);
            }
            else
            {
                // 新建模式：创建新角色
                ai = new AiCharacter();
                ai.UserAccount = _loginUsername;
                ai.AiName = this.NickName;
                ai.AiGender = rdoMale.Checked ? "男" : "女";
                ai.AiPersona = this.RoleDesc;
                ai.AiStyle = this.TalkStyle;
                ai.AiHabit = this.Habit;
                ai.AiOpening = this.Opening;
                ai.CreateTime = DateTime.Now;

                success = repo.AddAiCharacter(ai, out error);
            }

            // 4. 结果
            if (success)
            {
                CreatedAiCharacter = ai;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(_editAiCharacter != null ? "更新失败：\r\n" + error : "创建失败：\r\n" + error);
            }
        }
        #endregion
    }
}