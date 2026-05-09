﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace ChatControl.Controls
{
    /// <summary>
    /// 聊天主界面用户控件
    /// 功能：显示聊天气泡、自动回复、时间戳、分组显示、消息选择复制
    /// 布局：上方聊天区域（panelChat）+ 下方输入区域（panelInput，含txtInput和btnSend）
    /// </summary>
    public partial class ChatMainControl : UserControl
    {
        #region 样式配置（头像改为可配置占位）
        // ==================== 气泡样式配置 ====================
        /// <summary>自己消息的气泡背景色（绿色）</summary>
        private readonly Color _selfColor = Color.FromArgb(181, 236, 208);
        /// <summary>男性AI消息的气泡背景色（浅天蓝色）</summary>
        private readonly Color _maleAiColor = Color.LightSkyBlue;
        /// <summary>女性AI消息的气泡背景色（粉色）</summary>
        private readonly Color _femaleAiColor = Color.FromArgb(248, 205, 222);
        /// <summary>气泡内部文字与边框的间距</summary>
        private readonly int _bubblePadding = 8;
        /// <summary>两条消息之间的垂直间距</summary>
        private readonly int _bubbleMargin = 10;
        /// <summary>气泡圆角的半径值</summary>
        private readonly int _cornerRadius = 12;
        /// <summary>消息正文的字体大小</summary>
        private readonly float _fontSize = 9.5f;
        /// <summary>时间戳气泡的字体大小</summary>
        private readonly float _timestampFontSize = 9.5f;
        /// <summary>气泡最大宽度占可用宽度的比例（0.6=60%，为头像留空间）</summary>
        private readonly double _maxBubbleWidthRatio = 0.6;
        /// <summary>右侧滚动条的宽度</summary>
        private readonly int _scrollBarWidth = 18;
        /// <summary>时间戳气泡的高度</summary>
        private readonly int _timestampHeight = 25;
        /// <summary>时间戳与下方消息的间距</summary>
        private readonly int _timestampMargin = 5;
        /// <summary>时间戳气泡的背景色</summary>
        private readonly Color _timestampBgColor = Color.FromArgb(240, 240, 240);
        /// <summary>两条消息超过此时间间隔（分钟）则显示新的时间戳</summary>
        private readonly int _timeGroupInterval = 5;

        // ==================== 头像样式配置 ====================
        /// <summary>头像的直径（正方形边长，绘制时为圆形）</summary>
        private readonly int _avatarSize = 40;
        /// <summary>头像与气泡之间的水平间距</summary>
        private readonly int _avatarMargin = 8;
        /// <summary>自己头像的占位背景色（绿色）</summary>
        private readonly Color _selfAvatarBg = Color.FromArgb(181, 236, 208);
        /// <summary>男性AI头像的占位背景色（浅天蓝色）</summary>
        private readonly Color _maleAiAvatarBg = Color.LightSkyBlue;
        /// <summary>女性AI头像的占位背景色（粉色）</summary>
        private readonly Color _femaleAiAvatarBg = Color.FromArgb(248, 205, 222);
        /// <summary>头像的白色边框颜色</summary>
        private readonly Color _avatarBorderColor = Color.White;
        #endregion

        #region 事件
        /// <summary>
        /// 消息发送事件（用于外部存储消息到数据库）
        /// </summary>
        public event EventHandler<MessageEventArgs>? MessageSent;

        /// <summary>
        /// AI响应接收事件（用于外部（如FrmChatMain）处理大模型响应）
        /// </summary>
        public event EventHandler<AiResponseEventArgs>? AiResponseReceived;

        /// <summary>
        /// 重新生成AI回复事件（用于外部删除数据库中的AI消息）
        /// </summary>
        public event EventHandler? RegenerateResponseRequested;

        /// <summary>
        /// 回溯事件：删除选中消息及其后续所有消息
        /// </summary>
        public event EventHandler<MessageRollbackEventArgs>? MessageRollbackRequested;

        /// <summary>
        /// 消息更新事件：用于更新消息的ID和TotalMessageCount
        /// </summary>
        public event EventHandler<MessageUpdateEventArgs>? MessageUpdated;
        #endregion

        #region 属性
        /// <summary>
        /// 当前AI角色的性别
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? AiGender { get; set; }

        /// <summary>
        /// 当前登录用户名
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? LoginUsername { get; set; }
        #endregion

        #region 数据结构
        /// <summary>
        /// 聊天消息的数据结构
        /// 用于存储单条消息的所有属性和绘制相关信息
        /// </summary>
        private class ChatMessage
        {
            /// <summary>数据库中的消息ID</summary>
            public int Id { get; set; }
            /// <summary>消息的文本内容</summary>
            public string Text { get; set; } = string.Empty;
            /// <summary>是否为发送者自己的消息（true=自己，false=对方）</summary>
            public bool IsSelf { get; set; }
            /// <summary>消息绘制时测量出的尺寸（用于计算布局）</summary>
            public Size MeasuredSize { get; set; } = Size.Empty;
            /// <summary>上次计算尺寸时的最大宽度（用于判断是否需要重算）</summary>
            public int LastCalculatedMaxWidth { get; set; } = 0;
            /// <summary>气泡的绘制区域矩形（用于点击检测）</summary>
            public Rectangle DrawRect { get; set; } = Rectangle.Empty;
            /// <summary>消息发送时间</summary>
            public DateTime SendTime { get; set; } = DateTime.Now;
            /// <summary>是否需要显示时间戳（两消息间隔超过5分钟则显示）</summary>
            public bool ShowTimestamp { get; set; } = false;
            /// <summary>头像的绘制区域矩形（用于点击检测和绘制）</summary>
            public Rectangle AvatarRect { get; set; } = Rectangle.Empty;
            /// <summary>用户与AI对话消息总数（用于记忆生成触发计数）</summary>
            public int TotalMessageCount { get; set; }
        }

        #endregion

        #region 运行时状态变量
        /// <summary>存储所有聊天消息的列表</summary>
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        /// <summary>当前垂直滚动位置（像素）</summary>
        private int _scrollY = 0;
        /// <summary>所有消息的总高度（用于计算滚动条）</summary>
        private int _totalHeight = 0;
        /// <summary>当前气泡的最大宽度（根据容器宽度和比例计算）</summary>
        private int _currentMaxBubbleWidth = 0;
        /// <summary>上次容器宽度（用于检测宽度变化触发重算）</summary>
        private int _lastContainerWidth = 0;
        /// <summary>消息正文的字体（微软雅黑）</summary>
        private readonly Font _messageFont;
        /// <summary>时间戳的字体（微软雅黑）</summary>
        private readonly Font _timestampFont;
        /// <summary>垂直滚动条控件</summary>
        private readonly VScrollBar _vScrollBar = new VScrollBar();
        /// <summary>是否正在执行调整大小的防抖操作（防止重复触发）</summary>
        private bool _isResizing = false;
        /// <summary>实际最大滚动值（用于限制滚动范围）</summary>
        private int _actualMaxScrollValue = 0;

        /// <summary>当前选中的消息（用于复制和高亮显示）</summary>
        private ChatMessage? _selectedMessage;
        /// <summary>鼠标按下时的位置（用于文本选择）</summary>
        private Point _mouseDownPoint;
        /// <summary>是否正在进行文本选择操作</summary>
        private bool _isSelecting = false;
        /// <summary>右键复制菜单</summary>
        private readonly ContextMenuStrip _copyMenu = new ContextMenuStrip();

        /// <summary>缓存的时间戳背景画刷</summary>
        private static SolidBrush? _timestampBgBrush;
        /// <summary>缓存的头像背景画刷（自己）</summary>
        private static SolidBrush? _selfAvatarBrush;
        /// <summary>缓存的头像背景画刷（AI）</summary>
        private static SolidBrush? _aiAvatarBrush;
        /// <summary>缓存的气泡背景画刷（自己）</summary>
        private static SolidBrush? _selfBubbleBrush;
        /// <summary>缓存的气泡背景画刷（男性AI）</summary>
        private static SolidBrush? _maleAiBubbleBrush;
        /// <summary>缓存的气泡背景画刷（女性AI）</summary>
        private static SolidBrush? _femaleAiBubbleBrush;
        /// <summary>缓存的男性AI头像背景画刷</summary>
        private static SolidBrush? _maleAiAvatarBrush;
        /// <summary>缓存的选中气泡背景画刷（自己）</summary>
        private static SolidBrush? _selfSelectedBubbleBrush;
        /// <summary>缓存的选中气泡背景画刷（AI）</summary>
        private static SolidBrush? _aiSelectedBubbleBrush;
        /// <summary>缓存的头像边框画笔</summary>
        private static Pen? _avatarPen;
        /// <summary>缓存的时间戳文字颜色</summary>
        private static Color _timestampTextColor = Color.FromArgb(100, 100, 100);
        /// <summary>缓存的消息文字颜色</summary>
        private static Color _messageTextColor = Color.Black;
        #endregion

        #region 构造函数 - 初始化
        /// <summary>
        /// 构造函数：初始化聊天控件
        /// 设置字体、滚动条、事件绑定等
        /// </summary>
        public ChatMainControl()
        {
            // 初始化组件（由Designer.cs生成）
            InitializeComponent();

            // 创建字体对象（微软雅黑）
            _messageFont = new Font("微软雅黑", _fontSize);
            _timestampFont = new Font("微软雅黑", _timestampFontSize);

            // 设置输入框字体为10.5磅
            txtInput.Font = new Font("微软雅黑", 10.5f, FontStyle.Regular);

            // ==================== panelChat聊天面板配置 ====================
            // 启用双缓冲，减少绘制时的闪烁
            panelChat.DoubleBuffered(true);
            // 关闭自动滚动（使用自定义滚动条）
            panelChat.AutoScroll = false;
            // 禁用水平滚动条
            panelChat.HorizontalScroll.Enabled = false;
            panelChat.HorizontalScroll.Visible = false;
            // 关闭调整大小时的重绘（配合防抖机制）
            SetResizeRedraw(panelChat, false);

            // 初始化GDI+缓存对象
            InitializeGdiCache();

            // ==================== 滚动条配置 ====================
            _vScrollBar.Dock = DockStyle.Right;          // 停靠在右侧
            _vScrollBar.Width = _scrollBarWidth;          // 设置宽度
            _vScrollBar.Minimum = 0;                      // 最小值
            _vScrollBar.Maximum = 0;                      // 初始时无可滚动内容
            _vScrollBar.Value = 0;                        // 初始位置
            // 滚动时更新_scrollY并重绘
            _vScrollBar.Scroll += (s, e) =>
            {
                // 根据滚动事件类型处理
                if (e.Type == ScrollEventType.ThumbTrack)
                {
                    // 拖动滑块时，限制Value在有效范围内
                    int clampedValue = Math.Clamp(e.NewValue, _vScrollBar.Minimum, _actualMaxScrollValue);
                    e.NewValue = clampedValue;
                    _scrollY = clampedValue;
                }
                else if (e.Type == ScrollEventType.EndScroll)
                {
                    // 释放滑块时，确保滚动位置在有效范围内
                    _scrollY = Math.Clamp(_scrollY, _vScrollBar.Minimum, _actualMaxScrollValue);
                    _vScrollBar.Value = _scrollY;
                }
                else
                {
                    _scrollY = Math.Clamp(e.NewValue, _vScrollBar.Minimum, _actualMaxScrollValue);
                }
                panelChat.Invalidate();
            };
            // 将滚动条添加到panelChat
            panelChat.Controls.Add(_vScrollBar);
            _vScrollBar.BringToFront();

            // ==================== 调整大小事件处理（防抖机制） ====================
            // 使用Timer延迟执行，避免频繁重算
            panelChat.Resize += (s, e) =>
            {
                // 防止重复触发
                if (_isResizing) return;
                _isResizing = true;

                // 记录调整大小前的高度
                int oldHeight = panelChat.ClientSize.Height;

                // 创建50ms延迟的定时器
                var timer = new System.Windows.Forms.Timer { Interval = 50 };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    _isResizing = false;

                    // 记录调整大小后的高度
                    int newHeight = panelChat.ClientSize.Height;

                    // 检查是否当前已经滚动到底部
                    bool wasAtBottom = _vScrollBar.Visible && _scrollY >= _actualMaxScrollValue - 10; // 允许10像素的误差

                    // 重算所有消息尺寸（容器宽度可能已改变）
                    RecalculateAllMessageSizes();
                    // 更新滚动条状态
                    UpdateScrollBar();

                    if (wasAtBottom || newHeight < oldHeight) // 窗口缩小或之前在底部
                    {
                        // 如果之前在底部，或者窗口缩小了，保持在底部
                        ScrollToBottom();
                    }
                    else
                    {
                        // 否则恢复到之前的滚动位置
                        _scrollY = Math.Clamp(_scrollY, 0, _vScrollBar.Maximum);
                        _vScrollBar.Value = _scrollY;
                    }

                    // 重绘
                    panelChat.Invalidate();
                    // 重新计算标签位置
                    CenterChatNameLabel();
                };
                timer.Start();
            };

            // ==================== 发送消息事件绑定 ====================
            // 点击发送按钮
            btnSend.Click += (s, e) => SendMessage();
            // 点击插入括号按钮
            btnInsertBracket.Click += (s, e) => InsertBrackets();
            // 点击重试回复按钮
            btnRetryReply.Click += (s, e) =>
            {
                // 找到最后一条AI消息
                if (_messages.Count > 0 && !_messages[_messages.Count - 1].IsSelf)
                {
                    _selectedMessage = _messages[_messages.Count - 1];
                    RegenerateResponse(s, e);
                }
            };
            // 输入框回车键发送（Shift+Enter换行）
            txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;  // 阻止发出"咚"声
                    SendMessage();
                }
            };

            // ==================== 绘制和鼠标事件绑定 ====================
            // 绑定Paint事件用于绘制聊天内容
            panelChat.Paint += PanelChat_Paint;
            // 绑定鼠标滚轮事件用于滚动
            panelChat.MouseWheel += PanelChat_MouseWheel;

            // ==================== 右键复制菜单 ====================
            _copyMenu.Items.Add("复制", null, CopySelectedText);
            _copyMenu.Items.Add("重新生成", null, RegenerateResponse);
            _copyMenu.Items.Add("回溯", null, MessageRollback);
            // 绑定鼠标事件用于消息选择
            panelChat.MouseDown += PanelChat_MouseDown;
            panelChat.MouseMove += PanelChat_MouseMove;
            panelChat.MouseUp += PanelChat_MouseUp;
            panelChat.MouseClick += PanelChat_MouseClick;

            // ==================== 聊天名称标签事件 ====================
            // 监听文本变化事件，重新计算位置
            lbl_CurrentChatName.TextChanged += (s, e) => CenterChatNameLabel();
            // 初始计算位置
            CenterChatNameLabel();
        }

        /// <summary>
        /// 居中聊天名称标签
        /// </summary>
        private void CenterChatNameLabel()
        {
            if (lbl_CurrentChatName == null || splitContainer1 == null)
                return;

            // 计算标签宽度
            int labelWidth = TextRenderer.MeasureText(lbl_CurrentChatName.Text, lbl_CurrentChatName.Font).Width;
            // 计算居中位置
            int centerX = (splitContainer1.Panel1.Width - labelWidth) / 2;
            // 设置标签位置
            lbl_CurrentChatName.Location = new Point(centerX, (splitContainer1.Panel1.Height - lbl_CurrentChatName.Height) / 2);
        }
        #endregion

        #region 文本选择与复制（适配头像区域）
        /// <summary>
        /// 鼠标按下事件处理
        /// 记录按下位置，尝试选中该位置的消息
        /// </summary>
        private void PanelChat_MouseDown(object? sender, MouseEventArgs e)
        {
            // 只处理左键
            if (e.Button != MouseButtons.Left) return;
            _isSelecting = true;  // 开始选择
            _mouseDownPoint = e.Location;  // 记录位置
            _selectedMessage = GetMessageAtPoint(e.Location);  // 获取点击的消息
            panelChat.Invalidate();  // 重绘（显示选中状态）
        }

        /// <summary>
        /// 鼠标移动事件处理
        /// 选中状态下移动时更新选择（当前未实现文本选择，仅重绘）
        /// </summary>
        private void PanelChat_MouseMove(object? sender, MouseEventArgs e)
        {
            // 如果没有在选择或没有选中的消息，则忽略
            if (!_isSelecting || _selectedMessage == null) return;
            // 当前仅触发重绘，实际文本选择功能未实现
            panelChat.Invalidate();
        }

        /// <summary>
        /// 鼠标释放事件处理
        /// 结束选择状态
        /// </summary>
        private void PanelChat_MouseUp(object? sender, MouseEventArgs e)
        {
            _isSelecting = false;
            // 不要在这里调用Invalidate，因为会导致选中状态被清除
            // panelChat.Invalidate();
        }

        /// <summary>
        /// 鼠标点击事件处理
        /// 左键点击空白处取消选中，右键点击显示复制菜单
        /// </summary>
        private void PanelChat_MouseClick(object? sender, MouseEventArgs e)
        {
            // 右键点击
            if (e.Button == MouseButtons.Right)
            {
                var msg = GetMessageAtPoint(e.Location);
                if (msg != null)
                {
                    _selectedMessage = msg;
                    // 检查是否是最后一条AI消息
                    bool isLastAiMessage = _messages.Count > 0 && msg == _messages[_messages.Count - 1] && !msg.IsSelf;
                    // 显示或隐藏"重新生成"选项
                    var regenerateItem = _copyMenu.Items[1];
                    regenerateItem.Visible = isLastAiMessage;
                    // "回溯"选项始终显示
                    var rollbackItem = _copyMenu.Items[2];
                    rollbackItem.Visible = true;
                    // 在点击位置显示上下文菜单
                    _copyMenu.Show(panelChat, e.Location);
                    panelChat.Invalidate();  // 重绘以显示选中状态
                }
            }
            // 左键点击
            else if (e.Button == MouseButtons.Left)
            {
                var msg = GetMessageAtPoint(e.Location);
                if (msg != null)
                {
                    _selectedMessage = msg;
                    panelChat.Invalidate();  // 重绘以显示选中状态
                }
                else
                {
                    _selectedMessage = null;  // 取消选中
                    panelChat.Invalidate();
                }
            }
        }

        /// <summary>
        /// 复制菜单项点击事件
        /// 将当前选中的消息文本复制到剪贴板
        /// </summary>
        private void CopySelectedText(object? sender, EventArgs e)
        {
            if (_selectedMessage != null)
                Clipboard.SetText(_selectedMessage.Text);
        }

        /// <summary>
        /// 重新生成AI回复
        /// </summary>
        private void RegenerateResponse(object? sender, EventArgs e)
        {
            if (_selectedMessage != null && _messages.Count > 0 && _selectedMessage == _messages[_messages.Count - 1] && !_selectedMessage.IsSelf)
            {
                // 触发重新生成事件，通知外部删除数据库中的AI消息
                RegenerateResponseRequested?.Invoke(this, EventArgs.Empty);

                // 删除最后一条AI消息
                _messages.RemoveAt(_messages.Count - 1);

                // 强制重置总高度和上次计算宽度，确保重新计算
                _totalHeight = 0;
                _lastContainerWidth = 0;
                // 重新计算总高度
                RecalculateAllMessageSizes();
                // 更新滚动条
                UpdateScrollBar();
                // 重绘
                panelChat.Invalidate();
                // 滚动到底部
                ScrollToBottom();

                // 找到用户的最后一条消息
                string userMessage = string.Empty;
                for (int i = _messages.Count - 1; i >= 0; i--)
                {
                    if (_messages[i].IsSelf)
                    {
                        userMessage = _messages[i].Text;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(userMessage))
                {
                    // 触发AI响应事件，重新发送请求
                    AiResponseReceived?.Invoke(this, new AiResponseEventArgs { UserMessage = userMessage, SendTime = DateTime.Now });
                }
            }
        }

        /// <summary>
        /// 回溯菜单项点击事件
        /// 删除选中消息及其后续所有消息
        /// </summary>
        private void MessageRollback(object? sender, EventArgs e)
        {
            if (_selectedMessage != null)
            {
                // 获取选中消息在列表中的索引（与UI删除逻辑一致）
                int selectedIndex = _messages.IndexOf(_selectedMessage);
                
                // 构建回溯事件参数（包含消息定位信息）
                var args = new MessageRollbackEventArgs
                {
                    MessageId = _selectedMessage.Id,
                    TotalMessageCount = _selectedMessage.TotalMessageCount,
                    IsUserMessage = _selectedMessage.IsSelf,
                    MessageText = _selectedMessage.Text,
                    SendTime = _selectedMessage.SendTime
                };

                // 触发事件，通知外部处理（数据库操作）
                MessageRollbackRequested?.Invoke(this, args);

                // 删除本地UI中的消息（只删除选中消息之后的消息）
                if (selectedIndex >= 0)
                {
                    // 删除选中消息之后的所有消息（不包括选中消息本身）
                    if (selectedIndex < _messages.Count - 1)
                    {
                        _messages.RemoveRange(selectedIndex + 1, _messages.Count - (selectedIndex + 1));
                    }

                    // 重新计算布局和滚动条
                    _totalHeight = 0;
                    _lastContainerWidth = 0;
                    RecalculateAllMessageSizes();
                    UpdateScrollBar();
                    panelChat.Invalidate();
                    ScrollToBottom();
                }
            }
        }

        /// <summary>
        /// 根据点击坐标获取对应的消息
        /// 跳过头像区域，只在气泡区域返回消息
        /// </summary>
        /// <param name="point">点击的屏幕坐标</param>
        /// <returns>如果点击在气泡上返回对应消息，否则返回null</returns>
        private ChatMessage? GetMessageAtPoint(Point point)
        {
            // 计算可用宽度（减去滚动条占用的宽度）
            int availableWidth = panelChat.ClientSize.Width - (_vScrollBar.Visible ? _scrollBarWidth : 0);
            int currentY = -_scrollY;  // 考虑滚动偏移

            // 遍历所有消息，检测点击位置
            foreach (var msg in _messages)
            {
                // 如果显示时间戳，偏移Y坐标
                if (msg.ShowTimestamp)
                {
                    currentY += _timestampHeight + _timestampMargin;
                }

                // 计算气泡尺寸
                int bubbleWidth = Math.Min(_currentMaxBubbleWidth, msg.MeasuredSize.Width + _bubblePadding * 2);
                int bubbleHeight = msg.MeasuredSize.Height + _bubblePadding * 2;

                // 计算气泡X坐标（自己靠右，对方靠左）
                int bubbleX = msg.IsSelf
                    ? availableWidth - bubbleWidth - _avatarSize - _avatarMargin * 2
                    : _avatarSize + _avatarMargin * 2;

                // 计算头像X坐标（自己靠右，对方靠左）
                int avatarX = msg.IsSelf
                    ? availableWidth - _avatarSize - _avatarMargin
                    : _avatarMargin;

                // 头像区域矩形
                Rectangle avatarRect = new Rectangle(avatarX, currentY, _avatarSize, _avatarSize);

                // 如果点击在头像区域，跳过此消息（头像点击不选中消息）
                if (avatarRect.Contains(point))
                {
                    currentY += Math.Max(bubbleHeight, _avatarSize) + _bubbleMargin;
                    continue;
                }

                // 气泡区域矩形
                Rectangle msgRect = new Rectangle(bubbleX, currentY, bubbleWidth, bubbleHeight);

                // 如果点击在气泡区域，返回该消息
                if (msgRect.Contains(point))
                {
                    msg.AvatarRect = avatarRect;
                    return msg;
                }

                // 移动到下一条消息的Y坐标
                currentY += Math.Max(bubbleHeight, _avatarSize) + _bubbleMargin;
            }
            return null;  // 没有点击在任何消息上
        }
        #endregion

        #region 基础功能
        /// <summary>
        /// 通过反射设置控件的ResizeRedraw属性
        /// 该属性控制调整大小时是否触发重绘
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="enable">是否启用resize重绘</param>
        private void SetResizeRedraw(Control control, bool enable)
        {
            try
            {
                PropertyInfo? prop = control.GetType().GetProperty("ResizeRedraw", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(control, enable, null);
            }
            catch { }
        }

        /// <summary>
        /// 鼠标滚轮事件处理
        /// 控制垂直滚动条的位置
        /// </summary>
        private void PanelChat_MouseWheel(object? sender, MouseEventArgs e)
        {
            // 如果滚动条不可见，则忽略
            if (!_vScrollBar.Visible) return;

            // 根据滚动方向计算新的值（Delta > 0 向上滚动）
            int delta = e.Delta > 0 ? -_vScrollBar.SmallChange : _vScrollBar.SmallChange;
            int newValue = Math.Clamp(_vScrollBar.Value + delta, _vScrollBar.Minimum, _actualMaxScrollValue);

            // 如果值有变化，更新滚动条和滚动位置
            if (newValue != _vScrollBar.Value)
            {
                _vScrollBar.Value = newValue;
                _scrollY = newValue;
                panelChat.Invalidate();
            }
        }
        #endregion

        #region 发送消息
        /// <summary>
        /// 发送消息的核心方法
        /// 获取输入框文本，添加到自己的消息列表，然后模拟对方自动回复
        /// </summary>
        private void SendMessage()
        {
            // 获取并清理输入文本
            string text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // 立即清空输入框并刷新UI，确保文字立即消失
            txtInput.Clear();
            txtInput.Refresh();

            // 添加自己的消息到列表
            AddMessage(text, true);
            // 滚动到底部
            ScrollToBottom();

            // 将消息发送事件触发移到后台线程，避免阻塞UI
            string messageText = text;
            DateTime sendTime = DateTime.Now;
            _ = Task.Run(() =>
            {
                MessageSent?.Invoke(this, new MessageEventArgs { Text = messageText, IsSelf = true, SendTime = sendTime });
            });
        }

        /// <summary>
        /// 外部调用发送消息（用于自动发送开场白等场景）
        /// </summary>
        /// <param name="text">要发送的消息文本</param>
        public void SendMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // 添加自己的消息到列表
            AddMessage(text, true);
            // 滚动到底部
            ScrollToBottom();

            // 触发消息发送事件（用于存储消息到数据库）
            MessageSent?.Invoke(this, new MessageEventArgs { Text = text, IsSelf = true, SendTime = DateTime.Now });
        }

        /// <summary>
        /// 添加一条消息到聊天列表
        /// </summary>
        /// <param name="text">消息文本</param>
        /// <param name="isSelf">是否为发送者自己的消息</param>
        private void AddMessage(string text, bool isSelf)
        {
            // 创建消息对象
            var msg = new ChatMessage
            {
                Text = text,
                IsSelf = isSelf,
                SendTime = DateTime.Now  // 记录发送时间
            };

            // 判断是否需要显示时间戳（根据与上一条消息的时间间隔）
            msg.ShowTimestamp = ShouldShowTimestamp(msg);

            // 添加到消息列表
            _messages.Add(msg);

            // 计算消息尺寸
            int availableWidth = panelChat.ClientSize.Width - (_vScrollBar.Visible ? _scrollBarWidth : 0);
            if (availableWidth > 0)
            {
                // 更新气泡最大宽度
                _currentMaxBubbleWidth = (int)(availableWidth * _maxBubbleWidthRatio);

                // 测量文本尺寸（考虑换行）
                msg.MeasuredSize = TextRenderer.MeasureText(
                    msg.Text, _messageFont,
                    new Size(_currentMaxBubbleWidth - _bubblePadding * 2, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                msg.LastCalculatedMaxWidth = _currentMaxBubbleWidth;

                // 计算消息总高度
                int bubbleHeight = msg.MeasuredSize.Height + _bubblePadding * 2;
                int msgHeight = Math.Max(bubbleHeight, _avatarSize);
                // 时间戳高度单独计算，不加入msgHeight
                int timestampHeight = msg.ShowTimestamp ? _timestampHeight + _timestampMargin : 0;
                // 只有当不是最后一条消息时才添加_bubbleMargin
                if (_messages.Count > 1)
                {
                    msgHeight += _bubbleMargin;
                }
                // 为最后一条消息添加底部间距
                if (_messages.Count == 1)
                {
                    msgHeight += _bubbleMargin;
                }
                // 消息总高度 = 消息高度 + 时间戳高度
                _totalHeight += msgHeight + timestampHeight;  // 累加到总高度
            }

            // 更新滚动条并重绘
            UpdateScrollBar();
            panelChat.Invalidate();
        }

        /// <summary>
        /// 添加AI回复消息到聊天列表（由外部调用，如LLM响应）
        /// </summary>
        /// <param name="text">消息文本</param>
        public void AddAiMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            AddMessage(text, false);
        }

        /// <summary>
        /// 清空聊天记录
        /// </summary>
        public void ClearMessages()
        {
            _messages.Clear();
            _totalHeight = 0;
            UpdateScrollBar();
            panelChat.Invalidate();
        }

        /// <summary>
        /// 添加历史消息
        /// </summary>
        /// <param name="text">消息文本</param>
        /// <param name="isSelf">是否为自己的消息</param>
        /// <param name="sendTime">发送时间</param>
        // 批量添加历史消息的标志
        private bool _isAddingHistoryBatch = false;
        // 批量添加时记录的滚动条可见性状态
        private bool _batchScrollBarVisible = false;

        /// <summary>
        /// 添加历史消息
        /// </summary>
        /// <param name="text">消息文本</param>
        /// <param name="isSelf">是否为自己的消息</param>
        /// <param name="sendTime">发送时间</param>
        /// <param name="messageId">消息的数据库ID</param>
        /// <param name="totalMessageCount">消息的total_message_count值</param>
        public void AddHistoryMessage(string text, bool isSelf, DateTime sendTime, int messageId = 0, int totalMessageCount = 0)
        {
            var msg = new ChatMessage
            {
                Id = messageId,
                Text = text,
                IsSelf = isSelf,
                SendTime = sendTime,
                TotalMessageCount = totalMessageCount
            };

            msg.ShowTimestamp = ShouldShowTimestamp(msg);
            _messages.Add(msg);

            // 使用批量添加时记录的滚动条状态
            int availableWidth = panelChat.ClientSize.Width - (_batchScrollBarVisible ? _scrollBarWidth : 0);
            if (availableWidth > 0)
            {
                _currentMaxBubbleWidth = (int)(availableWidth * _maxBubbleWidthRatio);
                msg.MeasuredSize = TextRenderer.MeasureText(
                    msg.Text, _messageFont,
                    new Size(_currentMaxBubbleWidth - _bubblePadding * 2, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                msg.LastCalculatedMaxWidth = _currentMaxBubbleWidth;

                int bubbleHeight = msg.MeasuredSize.Height + _bubblePadding * 2;
                int msgHeight = Math.Max(bubbleHeight, _avatarSize);
                // 时间戳高度单独计算，不加入msgHeight
                int timestampHeight = msg.ShowTimestamp ? _timestampHeight + _timestampMargin : 0;
                // 只有当不是最后一条消息时才添加_bubbleMargin
                if (_messages.Count > 1)
                {
                    msgHeight += _bubbleMargin;
                }
                // 为最后一条消息添加底部间距
                if (_messages.Count == 1)
                {
                    msgHeight += _bubbleMargin;
                }
                // 消息总高度 = 消息高度 + 时间戳高度
                _totalHeight += msgHeight + timestampHeight;
            }

            // 批量添加模式下不立即更新，否则立即更新
            if (!_isAddingHistoryBatch)
            {
                UpdateScrollBar();
                panelChat.Invalidate();
            }
        }

        /// <summary>
        /// 获取选中消息的文本内容
        /// 用于回溯功能
        /// </summary>
        /// <returns>选中消息的文本，如果无选中消息则返回空字符串</returns>
        public string GetSelectedMessageText()
        {
            return _selectedMessage?.Text ?? string.Empty;
        }

        /// <summary>
        /// 触发重新生成AI回复
        /// 用于外部调用（如回溯后重新发送用户消息）
        /// </summary>
        public void TriggerRegenerateResponse()
        {
            Console.WriteLine($"TriggerRegenerateResponse called. _messages.Count={_messages.Count}");
            // 找到最后一条AI消息
            if (_messages.Count > 0 && !_messages[_messages.Count - 1].IsSelf)
            {
                Console.WriteLine("最后一条消息是AI消息，准备重新生成");
                // 保存当前选中的消息（不修改 _selectedMessage，避免影响回溯逻辑）
                var lastAiMessage = _messages[_messages.Count - 1];
                // 临时设置 _selectedMessage 用于 RegenerateResponse
                var originalSelected = _selectedMessage;
                _selectedMessage = lastAiMessage;
                RegenerateResponse(this, EventArgs.Empty);
                // 恢复原来的选中消息
                _selectedMessage = originalSelected;
            }
            else
            {
                Console.WriteLine($"最后一条消息不是AI消息或消息列表为空，不重新生成。LastMessage.IsSelf={(_messages.Count > 0 ? _messages[_messages.Count - 1].IsSelf.ToString() : "N/A")}");
            }
        }

        /// <summary>
        /// 更新消息的ID和TotalMessageCount
        /// 用于在消息存储到数据库后更新UI中的消息对象
        /// </summary>
        /// <param name="messageId">消息的数据库ID</param>
        /// <param name="totalMessageCount">消息的total_message_count值</param>
        /// <param name="messageText">消息文本（用于匹配）</param>
        /// <param name="isSelf">是否为自己的消息（用于匹配）</param>
        /// <param name="sendTime">发送时间（用于匹配）</param>
        public void UpdateMessageInfo(int messageId, int totalMessageCount, string messageText, bool isSelf, DateTime sendTime)
        {
            // 找到匹配的消息
            var message = _messages.LastOrDefault(msg => 
                msg.Text == messageText && 
                msg.IsSelf == isSelf && 
                msg.SendTime == sendTime);

            if (message != null)
            {
                message.Id = messageId;
                message.TotalMessageCount = totalMessageCount;
            }
        }

        /// <summary>
        /// 开始批量添加历史消息（性能优化）
        /// 清空现有消息列表，准备重新加载
        /// </summary>
        public void BeginBatchAddHistory()
        {
            _isAddingHistoryBatch = true;
            // 记录当前滚动条状态
            _batchScrollBarVisible = _vScrollBar.Visible;
            // 清空现有消息列表，确保重新加载时不会重复
            _messages.Clear();
            _totalHeight = 0;
            _lastContainerWidth = 0;
        }

        /// <summary>
        /// 结束批量添加历史消息并更新界面
        /// </summary>
        public void EndBatchAddHistory()
        {
            _isAddingHistoryBatch = false;
            _batchScrollBarVisible = false;
            UpdateScrollBar();
            panelChat.Invalidate();
        }

        /// <summary>
        /// 判断新消息是否需要显示时间戳
        /// 规则：如果消息列表为空，或与最后一条消息间隔超过_timeGroupInterval分钟，则显示
        /// </summary>
        /// <param name="newMsg">新消息</param>
        /// <returns>是否需要显示时间戳</returns>
        private bool ShouldShowTimestamp(ChatMessage newMsg)
        {
            // 列表为空，需要显示
            if (_messages.Count == 0)
                return true;

            // 获取最后一条消息
            var lastMsg = _messages[_messages.Count - 1];
            // 计算时间间隔
            TimeSpan interval = newMsg.SendTime - lastMsg.SendTime;
            // 超过指定分钟数则显示
            return interval.TotalMinutes >= _timeGroupInterval;
        }
        #endregion

        #region 重算尺寸（容器宽度变化时）
        /// <summary>
        /// 当容器宽度变化时，重新计算所有消息的尺寸
        /// 原因：气泡最大宽度取决于容器宽度
        /// </summary>
        private void RecalculateAllMessageSizes()
        {
            // 计算可用宽度
            int availableWidth = panelChat.ClientSize.Width - (_vScrollBar.Visible ? _scrollBarWidth : 0);
            if (availableWidth <= 0) return;

            // 计算新的气泡最大宽度
            _currentMaxBubbleWidth = (int)(availableWidth * _maxBubbleWidthRatio);

            // 如果宽度没变，不需要重算
            if (_currentMaxBubbleWidth == _lastContainerWidth) return;
            _lastContainerWidth = _currentMaxBubbleWidth;

            // 重置总高度
            _totalHeight = 0;

            // 遍历所有消息，重新测量尺寸
            foreach (var msg in _messages)
            {
                // 只有宽度变化了才需要重新测量
                if (msg.LastCalculatedMaxWidth != _currentMaxBubbleWidth)
                {
                    msg.MeasuredSize = TextRenderer.MeasureText(
                        msg.Text, _messageFont,
                        new Size(_currentMaxBubbleWidth - _bubblePadding * 2, int.MaxValue),
                        TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                    msg.LastCalculatedMaxWidth = _currentMaxBubbleWidth;
                }

                // 重新计算消息高度
                int bubbleHeight = msg.MeasuredSize.Height + _bubblePadding * 2;
                int msgHeight = Math.Max(bubbleHeight, _avatarSize) + _bubbleMargin;
                if (msg.ShowTimestamp)
                {
                    msgHeight += _timestampHeight + _timestampMargin;
                }
                _totalHeight += msgHeight;
            }
        }

        /// <summary>
        /// 初始化GDI+缓存对象
        /// 避免在绘制时频繁创建和销毁画刷、画笔等对象，减少垃圾回收，提高性能
        /// </summary>
        private void InitializeGdiCache()
        {
            _timestampBgBrush = new SolidBrush(_timestampBgColor);
            _selfAvatarBrush = new SolidBrush(_selfAvatarBg);
            _aiAvatarBrush = new SolidBrush(_femaleAiAvatarBg);  // AI女性头像用粉色
            _maleAiAvatarBrush = new SolidBrush(_maleAiAvatarBg);  // AI男性头像用浅天蓝色
            _selfBubbleBrush = new SolidBrush(_selfColor);
            _maleAiBubbleBrush = new SolidBrush(_maleAiColor);
            _femaleAiBubbleBrush = new SolidBrush(_femaleAiColor);
            _selfSelectedBubbleBrush = new SolidBrush(Color.FromArgb(255, 200, 200, 200));
            _aiSelectedBubbleBrush = new SolidBrush(Color.FromArgb(255, 220, 220, 220));
            _avatarPen = new Pen(_avatarBorderColor, 2);
        }
        #endregion

        #region 绘制（气泡 + 居中时间戳 + 占位圆形头像）
        /// <summary>
        /// 绘制聊天内容的主要方法
        /// 在panelChat的Paint事件中被调用
        /// 绘制顺序：时间戳 -> 头像 -> 气泡 -> 文字
        /// </summary>
        private void PanelChat_Paint(object? sender, PaintEventArgs e)
        {
            // 安全检查
            if (sender == null || panelChat.IsDisposed) return;

            // 获取Graphics对象
            Graphics g = e.Graphics;
            // 设置高质量绘制模式
            g.SmoothingMode = SmoothingMode.AntiAlias;  // 抗锯齿
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;  // 清晰文字
            g.Clip = new Region(panelChat.ClientRectangle);  // 限制绘制区域

            // 计算可见区域参数
            int availableWidth = panelChat.ClientSize.Width - (_vScrollBar.Visible ? _scrollBarWidth : 0);
            int currentY = -_scrollY;  // 考虑滚动偏移的起始Y
            int clientHeight = panelChat.ClientSize.Height;

            // 遍历所有消息进行绘制
            foreach (var msg in _messages)
            {
                // ========== 1. 绘制时间戳（如果需要显示）==========
                if (msg.ShowTimestamp)
                {
                    // 格式化时间文本
                    string timeText = msg.SendTime.ToString("yyyy-MM-dd HH:mm");
                    // 测量文本尺寸
                    Size timeTextSize = TextRenderer.MeasureText(timeText, _timestampFont);
                    // 计算时间戳气泡宽度（文本宽度 + 左右padding）
                    int timestampWidth = timeTextSize.Width + 20;
                    // 水平居中
                    int timestampX = (availableWidth - timestampWidth) / 2;

                    // 时间戳气泡矩形
                    Rectangle timestampRect = new Rectangle(
                        timestampX,
                        currentY,
                        timestampWidth,
                        _timestampHeight);

                    // 只绘制可视区域内的内容
                    if (currentY + _timestampHeight > 0 && currentY < clientHeight)
                    {
                        // 使用缓存的画刷绘制圆角矩形
                        using (GraphicsPath path = RoundedRectangle(0, 0, timestampWidth, _timestampHeight, 8))
                        {
                            // 使用变换绘制圆角矩形（保持代码简洁）
                            g.TranslateTransform(timestampRect.X, timestampRect.Y);
                            g.FillPath(_timestampBgBrush!, path);
                            g.TranslateTransform(-timestampRect.X, -timestampRect.Y);
                        }

                        // 绘制时间文本（灰色）
                        TextRenderer.DrawText(g, timeText, _timestampFont, timestampRect,
                            _timestampTextColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    // 移动到时间戳下方
                    currentY += _timestampHeight + _timestampMargin;
                }

                // ========== 2. 计算气泡和头像的位置 ==========
                // 气泡宽度（不超过最大宽度）
                int bubbleWidth = Math.Min(_currentMaxBubbleWidth, msg.MeasuredSize.Width + _bubblePadding * 2);
                int textHeight = msg.MeasuredSize.Height;
                int bubbleHeight = textHeight + _bubblePadding * 2;
                int msgHeight = Math.Max(bubbleHeight, _avatarSize);  // 消息高度至少为头像高度

                // 判断是否为最后一条消息
                bool isLastMessage = (msg == _messages[_messages.Count - 1]);
                int actualBubbleMargin = isLastMessage ? _bubbleMargin : _bubbleMargin;

                // 气泡X坐标：自己靠右，对方靠左
                int bubbleX = msg.IsSelf
                    ? availableWidth - bubbleWidth - _avatarSize - _avatarMargin * 2
                    : _avatarSize + _avatarMargin * 2;

                // 头像X坐标：自己在最右，对方在最左
                int avatarX = msg.IsSelf
                    ? availableWidth - _avatarSize - _avatarMargin
                    : _avatarMargin;

                // 头像Y坐标：与气泡垂直居中对齐
                int avatarY = currentY + (msgHeight - _avatarSize) / 2;

                // 记录绘制区域（用于点击检测）
                msg.DrawRect = new Rectangle(bubbleX, currentY, bubbleWidth, bubbleHeight);
                msg.AvatarRect = new Rectangle(avatarX, avatarY, _avatarSize, _avatarSize);

                // ========== 跳过可视区域外的内容（性能优化）==========
                if (currentY + msgHeight < 0 || currentY > clientHeight)
                {
                    currentY += msgHeight + actualBubbleMargin;
                    continue;
                }

                // ========== 3. 绘制占位圆形头像 ==========
                // 选择缓存的画刷
                SolidBrush avatarBrush;
                if (msg.IsSelf)
                    avatarBrush = _selfAvatarBrush!;  // 自己头像用中海洋绿色
                else if (string.Equals(AiGender, "女", StringComparison.OrdinalIgnoreCase))
                    avatarBrush = _aiAvatarBrush!;  // AI女性头像用粉色
                else
                    avatarBrush = _maleAiAvatarBrush!;  // AI男性头像用浅天蓝色

                using (GraphicsPath path = new GraphicsPath())
                {
                    // 创建椭圆路径（圆形）
                    path.AddEllipse(avatarX, avatarY, _avatarSize, _avatarSize);
                    // 填充圆形背景
                    g.FillPath(avatarBrush, path);
                    // 绘制白色边框
                    g.DrawPath(_avatarPen!, path);
                }

                // ========== 4. 绘制气泡背景 ==========
                // 选中状态变色
                SolidBrush bubbleBrush;
                if (msg == _selectedMessage)
                {
                    // 选中状态使用稍微深一点的颜色
                    if (msg.IsSelf)
                        bubbleBrush = _selfSelectedBubbleBrush!;
                    else if (string.Equals(AiGender, "女", StringComparison.OrdinalIgnoreCase))
                        bubbleBrush = new SolidBrush(Color.FromArgb(255, 220, 180, 190)); // 深粉色
                    else
                        bubbleBrush = new SolidBrush(Color.FromArgb(100, 180, 220, 255)); // 深蓝色
                }
                else
                {
                    if (msg.IsSelf)
                        bubbleBrush = _selfBubbleBrush!;
                    else if (string.Equals(AiGender, "女", StringComparison.OrdinalIgnoreCase))
                        bubbleBrush = _femaleAiBubbleBrush!;
                    else
                        bubbleBrush = _maleAiBubbleBrush!;
                }

                using (GraphicsPath path = RoundedRectangle(0, 0, bubbleWidth, bubbleHeight, _cornerRadius))
                {
                    // 使用变换绘制（保持代码简洁）
                    g.TranslateTransform(bubbleX, currentY);
                    g.FillPath(bubbleBrush, path);
                    g.TranslateTransform(-bubbleX, -currentY);
                }

                // ========== 5. 绘制消息文字 ==========
                Rectangle textRect = new Rectangle(
                    bubbleX + _bubblePadding,      // 左边距
                    currentY + _bubblePadding,    // 上边距
                    bubbleWidth - _bubblePadding * 2,  // 文字区域宽度
                    textHeight);                   // 文字区域高度

                TextRenderer.DrawText(g, msg.Text, _messageFont, textRect,
                    _messageTextColor, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

                // ========== 6. 移动到下一条消息的位置 ==========
                currentY += msgHeight + actualBubbleMargin;
            }
        }

        #endregion

        #region 滚动条管理
        /// <summary>
        /// 更新滚动条的状态和参数
        /// 根据总内容高度和可视区域高度决定滚动条的显示和参数
        /// </summary>
        private void UpdateScrollBar()
        {
            // 如果内容总高度不超过可视区域，隐藏滚动条
            if (_totalHeight <= panelChat.ClientSize.Height)
            {
                _vScrollBar.Visible = false;
                _vScrollBar.Maximum = 0;
                _vScrollBar.LargeChange = 0;
                _vScrollBar.SmallChange = 0;
                _scrollY = 0;
                _actualMaxScrollValue = 0;
                _vScrollBar.Value = 0;
            }
            else
            {
                // 显示滚动条
                _vScrollBar.Visible = true;
                // 实际最大滚动值 = 总高度 - 可视区域高度
                int actualMaxValue = Math.Max(0, _totalHeight - panelChat.ClientSize.Height);
                _actualMaxScrollValue = actualMaxValue;
                // 大步长 = 可视区域高度的一半（每次翻页）
                int largeChange = Math.Max(1, panelChat.ClientSize.Height / 2);
                // 小步长 = 20像素（每次滚动一行）
                int smallChange = 20;

                // Windows Forms滚动条的Maximum属性需要考虑LargeChange
                // 正确的Maximum值 = 实际最大滚动值 + LargeChange - 1
                _vScrollBar.Maximum = actualMaxValue + largeChange - 1;
                _vScrollBar.LargeChange = largeChange;
                _vScrollBar.SmallChange = smallChange;

                // 确保当前滚动位置在有效范围内
                _scrollY = Math.Clamp(_scrollY, 0, actualMaxValue);
                if (_vScrollBar.Value != _scrollY)
                    _vScrollBar.Value = _scrollY;
            }
            panelChat.Invalidate();
        }

        /// <summary>
        /// 插入括号并将光标移动到括号中间
        /// </summary>
        public void InsertBrackets()
        {
            // 保存当前光标位置
            int start = txtInput.SelectionStart;
            // 插入括号
            txtInput.Text = txtInput.Text.Insert(start, "（）");
            // 将光标移动到括号中间
            txtInput.SelectionStart = start + 1;
            // 聚焦到输入框
            txtInput.Focus();
        }

        /// <summary>
        /// 滚动到聊天内容底部
        /// 用于发送消息后自动滚动
        /// </summary>
        public void ScrollToBottom()
        {
            // 如果内容总高度不超过可视区域，直接重绘即可
            if (!_vScrollBar.Visible || _totalHeight <= panelChat.ClientSize.Height)
            {
                panelChat.Invalidate();
                return;
            }
            // 计算实际最大滚动值 = 总高度 - 可视区域高度
            int actualMaxValue = Math.Max(0, _totalHeight - panelChat.ClientSize.Height);
            // 设置滚动位置到最底部
            _scrollY = actualMaxValue;
            _vScrollBar.Value = _scrollY;
            panelChat.Invalidate();
        }

        #endregion

        #region 圆角工具方法
        /// <summary>
        /// 创建一个圆角矩形的GraphicsPath
        /// 用于绘制带圆角的气泡背景
        /// </summary>
        /// <param name="x">矩形左上角X坐标</param>
        /// <param name="y">矩形左上角Y坐标</param>
        /// <param name="width">矩形宽度</param>
        /// <param name="height">矩形高度</param>
        /// <param name="radius">圆角半径</param>
        /// <returns>圆角矩形的路径对象</returns>
        private GraphicsPath RoundedRectangle(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            // 左上角圆弧
            path.AddArc(x, y, radius, radius, 180, 90);
            // 右上角圆弧
            path.AddArc(x + width - radius, y, radius, radius, 270, 90);
            // 右下角圆弧
            path.AddArc(x + width - radius, y + height - radius, radius, radius, 0, 90);
            // 左下角圆弧
            path.AddArc(x, y + height - radius, radius, radius, 90, 90);
            // 闭合路径
            path.CloseAllFigures();
            return path;
        }

        #endregion
    }

    /// <summary>
    /// 回溯事件参数
    /// </summary>
    public class MessageRollbackEventArgs : EventArgs
    {
        /// <summary>选中消息的数据库ID</summary>
        public int MessageId { get; set; }
        /// <summary>选中消息的total_message_count值</summary>
        public int TotalMessageCount { get; set; }
        /// <summary>是否为用户消息（true=用户，false=AI）</summary>
        public bool IsUserMessage { get; set; }
        /// <summary>选中消息的文本内容（用于消息定位）</summary>
        public string MessageText { get; set; } = string.Empty;
        /// <summary>选中消息的发送时间（用于消息定位）</summary>
        public DateTime SendTime { get; set; }
    }

    /// <summary>
    /// 消息事件参数
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// 消息文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 是否为自己的消息
        /// </summary>
        public bool IsSelf { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime { get; set; }
    }

    /// <summary>
    /// 消息更新事件参数
    /// 用于在消息存储到数据库后更新ID和TotalMessageCount
    /// </summary>
    public class MessageUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// 消息的数据库ID
        /// </summary>
        public int MessageId { get; set; }
        /// <summary>
        /// 消息的total_message_count值
        /// </summary>
        public int TotalMessageCount { get; set; }
        /// <summary>
        /// 消息文本（用于匹配）
        /// </summary>
        public string MessageText { get; set; } = string.Empty;
        /// <summary>
        /// 是否为自己的消息（用于匹配）
        /// </summary>
        public bool IsSelf { get; set; }
        /// <summary>
        /// 发送时间（用于匹配）
        /// </summary>
        public DateTime SendTime { get; set; }
    }

    /// <summary>
    /// AI响应事件参数
    /// </summary>
    public class AiResponseEventArgs : EventArgs
    {
        /// <summary>
        /// 用户发送的消息文本
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime { get; set; }
    }

    /// <summary>
    /// Control类的扩展方法类
    /// 提供一些WinForms控件的扩展功能
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// 设置控件的双缓冲模式
        /// 双缓冲可以减少控件重绘时的闪烁
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="enable">是否启用双缓冲</param>
        public static void DoubleBuffered(this Control control, bool enable)
        {
            try
            {
                // 通过反射设置控件的DoubleBuffered属性
                var prop = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(control, enable, null);
            }
            catch { }
        }
    }
}