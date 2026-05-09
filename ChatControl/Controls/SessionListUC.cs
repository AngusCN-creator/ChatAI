using ChatControl.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace ChatControl.Controls
{
    public partial class SessionListUC : UserControl
    {
        // 会话选中事件：外部订阅获取当前选中的会话
        public event Action<SessionInfo>? SessionSelected;
        // 创建会话事件：点击按钮触发
        public event Action? OnCreateSession;
        // 删除会话事件：右键菜单触发
        public event Action<SessionInfo>? OnDeleteSession;
        // 编辑会话事件：右键菜单触发
        public event Action<SessionInfo>? OnEditSession;
        // 查看记忆事件：右键菜单触发
        public event Action<SessionInfo>? OnViewMemory;
        //// 选中会话时触发的事件
        //public event Action<string> OnSessionSelected;
        // 本地存储的会话数据列表
        private List<SessionInfo> _sessionList = new List<SessionInfo>();

        // ==============================================
        // 【可自由调整的美化参数】改这里就能一键换风格
        // ==============================================
        private readonly int _leftPadding = 6;        // 符号距离左边框的距离
        private readonly int _iconNameGap = 12;       // 符号和名字之间的间距
        private readonly int _charSpacing = 1;        // 名字每个字之间的像素间距（核心！改这里调字距）
        private readonly Color _selectedBackColor = Color.FromArgb(220, 235, 255); // 选中背景色（浅蓝高级感）
        private readonly Color _selectedForeColor = Color.Black;                    // 选中文字颜色
        private readonly Color _normalBackColor = Color.Gainsboro;                       // 正常背景色
        private readonly Color _normalForeColor = Color.Black;                       // 正常文字颜色

        public SessionListUC()
        {
            InitializeComponent();

            // 开启自定义绘制模式（必须开，否则所有美化无效）
            lstSession.DrawMode = DrawMode.OwnerDrawFixed;
            // 设置列表项高度（适配你的布局）
            lstSession.ItemHeight = 32;
            // 设置列表项字体
            lstSession.Font = new Font("微软雅黑", 12f, FontStyle.Regular);
            // 绑定选中项变化事件
            lstSession.SelectedIndexChanged += LstSession_SelectedIndexChanged;
            // 绑定自定义绘制事件
            lstSession.DrawItem += LstSession_DrawItem;
        }

        /// <summary>
        /// 列表选中项变化事件：通知外部选中的会话
        /// </summary>
        private void LstSession_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 双重校验：索引合法 + 列表不为空，杜绝越界
            if (lstSession.SelectedIndex >= 0
                && lstSession.SelectedIndex < _sessionList.Count
                && _sessionList.Count > 0)
            {
                SessionSelected?.Invoke(_sessionList[lstSession.SelectedIndex]);
            }
        }

        /// <summary>
        /// 创建会话按钮点击事件：触发外部创建逻辑
        /// </summary>
        private void btnCreateSession_Click(object sender, EventArgs e)
        {
            OnCreateSession?.Invoke();
        }

        /// <summary>
        /// 加载会话列表到ListBox
        /// 自动根据性别添加 ♂/♀ 图标
        /// </summary>
        public void LoadSessions(List<SessionInfo>? sessions)
        {
            // 空值防护：避免null导致崩溃
            _sessionList = sessions ?? new List<SessionInfo>();
            // 清空原有列表项
            lstSession.Items.Clear();

            // 遍历会话数据，生成带图标的显示文本
            foreach (var session in _sessionList)
            {
                string icon = GetGenderIcon(session.AiGender);
                // 仅拼接符号+名字，字间距在绘制时动态控制
                string displayText = $"{icon}{session.AiName}";
                lstSession.Items.Add(displayText);
            }
        }

        /// <summary>
        /// 刷新列表：重新加载当前会话数据
        /// </summary>
        public void RefreshList()
        {
            LoadSessions(_sessionList);
        }

        /// <summary>
        /// 强制重绘列表控件
        /// </summary>
        public void ForceRedraw()
        {
            lstSession.Invalidate();
        }

        /// <summary>
        /// 性别图标转换方法：兼容多种输入格式，空值默认?
        /// </summary>
        private string GetGenderIcon(string? gender)
        {
            // 统一处理：空值、空格、大小写，避免格式问题
            var normalized = (gender ?? "").Trim().ToLower();
            return normalized switch
            {
                "男" or "male" or "m" or "1" => "♂",
                "女" or "female" or "f" or "0" => "♀",
                _ => "?"
            };
        }

        /// <summary>
        /// 【终极美化版】列表框自定义绘制事件
        /// 功能：垂直居中+左边距+符号彩色+自定义选中色+名字逐字加间距
        /// </summary>
        private void LstSession_DrawItem(object sender, DrawItemEventArgs e)
        {
            // 🔴 关键修复：索引越界防护
            if (e.Index < 0 || e.Index >= lstSession.Items.Count)
                return;

            // ==============================================
            // 1. 自定义背景色（解决选中太丑的问题）
            // ==============================================
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color backColor = isSelected ? _selectedBackColor : _normalBackColor;
            Color foreColor = isSelected ? _selectedForeColor : _normalForeColor;

            // 绘制自定义背景
            using (Brush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            // ==============================================
            // 2. 获取文本，拆分符号和名字
            // ==============================================
            string text = lstSession.Items[e.Index].ToString()!;
            if (string.IsNullOrEmpty(text))
                return;

            // 拆分：第一个字符是符号，后面是完整名字
            char iconChar = text[0];
            string nameText = text.Substring(1); // 直接取名字，不再提前加空格

            // ==============================================
            // 3. 垂直居中计算（彻底解决文字偏上）
            // ==============================================
            // 创建粗体字体（直接指定字体名称和大小，确保粗体效果）
            Font boldFont = new Font("微软雅黑", 12f, FontStyle.Bold);
            float textHeight = e.Graphics.MeasureString(iconChar.ToString(), boldFont).Height;
            float y = e.Bounds.Y + (e.Bounds.Height - textHeight) / 2;

            // ==============================================
            // 4. 绘制符号（带左边距+彩色+粗体）
            // ==============================================
            float iconX = e.Bounds.X + _leftPadding;
            Brush iconBrush = iconChar switch
            {
                '♂' => Brushes.DodgerBlue,
                '♀' => Brushes.HotPink,
                _ => Brushes.Gray
            };
            e.Graphics.DrawString(iconChar.ToString(), boldFont, iconBrush, iconX, y);

            // ==============================================
            // 5. 绘制名字（逐字加间距，实现「李 少 华」效果）
            // ==============================================
            // 名字的起始X坐标 = 符号X + 符号宽度 + 符号与名字的间距
            float nameStartX = iconX + e.Graphics.MeasureString(iconChar.ToString(), boldFont).Width + _iconNameGap;
            float currentX = nameStartX;

            // 遍历名字的每个字符，逐个绘制，中间加自定义间距
            foreach (char c in nameText)
            {
                // 绘制单个字符
                e.Graphics.DrawString(c.ToString(), lstSession.Font, new SolidBrush(foreColor), currentX, y);
                // 计算当前字符的宽度，为下一个字符定位
                float charWidth = e.Graphics.MeasureString(c.ToString(), lstSession.Font).Width;
                // 移动X坐标：字符宽度 + 自定义字间距
                currentX += charWidth + _charSpacing;
            }

            // ==============================================
            // 6. 绘制焦点框（选中时的虚线框）
            // ==============================================
            e.DrawFocusRectangle();
        }

        /// <summary>
        /// 列表框鼠标按下事件：用于显示右键菜单
        /// </summary>
        private void lstSession_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // 获取右键点击位置的项索引
                int index = lstSession.IndexFromPoint(e.Location);
                if (index >= 0 && index < _sessionList.Count)
                {
                    // 选中右键点击的项
                    lstSession.SelectedIndex = index;
                    // 显示右键菜单
                    contextMenuStrip1.Show(lstSession, e.Location);
                }
            }
        }

        /// <summary>
        /// 删除会话菜单项点击事件
        /// </summary>
        private void tsmiDeleteSession_Click(object? sender, EventArgs e)
        {
            if (lstSession.SelectedIndex >= 0 && lstSession.SelectedIndex < _sessionList.Count)
            {
                var session = _sessionList[lstSession.SelectedIndex];
                OnDeleteSession?.Invoke(session);
            }
        }

        /// <summary>
        /// 编辑会话菜单项点击事件
        /// </summary>
        private void tsmiEditSession_Click(object? sender, EventArgs e)
        {
            if (lstSession.SelectedIndex >= 0 && lstSession.SelectedIndex < _sessionList.Count)
            {
                var session = _sessionList[lstSession.SelectedIndex];
                OnEditSession?.Invoke(session);
            }
        }

        /// <summary>
        /// 查看记忆菜单项点击事件
        /// </summary>
        private void tsmiViewMemory_Click(object? sender, EventArgs e)
        {
            if (lstSession.SelectedIndex >= 0 && lstSession.SelectedIndex < _sessionList.Count)
            {
                var session = _sessionList[lstSession.SelectedIndex];
                OnViewMemory?.Invoke(session);
            }
        }
    }
}