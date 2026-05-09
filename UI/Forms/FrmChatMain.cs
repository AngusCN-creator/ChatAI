﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using ChatAI.Data;
using ChatControl.Controls;
using ChatControl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatAI.UI.Forms
{
    public partial class FrmChatMain : Form
    {
        // 🔥 加这个：用来标记是不是"退出登录"
        public bool IsLogout = false;
        /// <summary>
        /// 当前登录的用户名（从登录窗口传入）
        /// </summary>
        private readonly string _loginUsername;

        /// <summary>
        /// 数据库仓储对象，用于加载AI角色数据
        /// </summary>
        private readonly UserRepository _userRepo = new UserRepository();

        /// <summary>
        /// 当前选中的会话信息
        /// </summary>
        private ChatControl.Models.SessionInfo? _currentSession;

        /// <summary>
        /// 记忆生成正在进行中（用于防止并发）
        /// </summary>
        private bool _isMemoryGenerating = false;

        /// <summary>
        /// 记忆生成锁对象
        /// </summary>
        private readonly object _memoryLock = new object();

        /// <summary>
        /// 等待处理的记忆生成请求队列
        /// </summary>
        private readonly Queue<int> _pendingMemoryRequests = new Queue<int>();

        /// <summary>
        /// 当前正在执行的记忆生成任务
        /// </summary>
        private Task? _currentMemoryTask = null;

        /// <summary>
        /// 构造函数
        /// 登录成功后，由登录窗口将用户名传入
        /// </summary>
        /// <param name="loginUsername">当前登录用户名</param>
        public FrmChatMain(string loginUsername)
        {
            InitializeComponent();
            splitContainer1.Panel2.BackColor = Color.White;

            // 保存登录用户名，用于后续数据查询
            _loginUsername = loginUsername;

            // 设置窗体标题，显示当前登录用户
            Text = $"当前用户：{_loginUsername}";

            // 绑定创建会话事件：点击创建按钮时打开创建窗口
            sessionListuc1.OnCreateSession += OpenCreateSessionWindow;

            // 订阅：会话选中时触发
            sessionListuc1.SessionSelected += SessionListUC_SessionSelected;

            // 订阅：删除会话时触发
            sessionListuc1.OnDeleteSession += SessionListUC_DeleteSession;
            // 订阅：编辑会话时触发
            sessionListuc1.OnEditSession += SessionListUC_EditSession;
            // 订阅：查看记忆时触发
            sessionListuc1.OnViewMemory += SessionListUC_ViewMemory;

            // 为聊天名称标签添加点击事件
            chatMainControl1.lbl_CurrentChatName.Click += lbl_CurrentChatName_Click;

            // 订阅消息发送事件，用于存储消息和处理AI响应
            chatMainControl1.MessageSent += ChatMainControl_MessageSent;

            // 设置ChatMainControl的LoginUsername属性
            chatMainControl1.LoginUsername = _loginUsername;

            // 订阅重新生成响应事件，用于删除数据库中的AI消息
            chatMainControl1.RegenerateResponseRequested += ChatMainControl_RegenerateResponseRequested;

            // 订阅回溯事件，用于删除选中消息及其后续消息
            chatMainControl1.MessageRollbackRequested += ChatMainControl_MessageRollbackRequested;

            // 绑定会话设置菜单项点击事件
            tsmi_MainMenu_SessionSetting.Click += tsmi_MainMenu_SessionSetting_Click;

            // 绑定AI记忆设置菜单项点击事件
            tsmi_AiMemorySettings.Click += tsmi_AiMemorySettings_Click;



            // ==============================
            // 核心：窗体加载时自动加载会话列表
            // ==============================
            LoadUserAiSessions();
        }

        /// <summary>
        /// 【核心方法】加载当前用户的所有AI角色会话
        /// 从 ai_character 表查询数据，按创建时间倒序加载
        /// </summary>
        /// <param name="autoSelectAiAssistant">是否自动选中"AI聊天助手"会话</param>
        private void LoadUserAiSessions(bool autoSelectAiAssistant = true)
        {
            try
            {
                // 1. 调用仓储层，查询当前用户的所有AI角色
                // 返回 SessionInfo 集合（包含 AiName、AiGender）
                List<ChatControl.Models.SessionInfo> sessionList =
                    _userRepo.LoadAiCharacter(_loginUsername);

                // 2. 按AI名称排序，确保"AI聊天助手"排在最前面（如果存在）
                sessionList = sessionList.OrderBy(s => s.AiName == "AI聊天助手" ? 0 : 1).ToList();

                // 3. 获取会话列表控件
                SessionListUC sessionControl = sessionListuc1;

                // 4. 绑定数据到列表（自动显示 ♂ / ♀ + 昵称）
                sessionControl.LoadSessions(sessionList);

                // 5. 如果存在"AI聊天助手"会话，自动选中它
                var aiAssistantSession = sessionList.FirstOrDefault(s => s.AiName == "AI聊天助手");
                if (aiAssistantSession != null && autoSelectAiAssistant)
                {
                    SessionListUC_SessionSelected(aiAssistantSession);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"加载会话失败：{ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// 打开创建会话窗口
        /// 创建成功后自动刷新会话列表并选中该会话
        /// </summary>
        private void OpenCreateSessionWindow()
        {
            // 创建窗口，并传入当前登录用户名
            CreateSessionForm form = new CreateSessionForm(_loginUsername);

            // 如果用户点击"创建"并成功
            if (form.ShowDialog() == DialogResult.OK)
            {
                // 重新加载会话列表，显示新创建的AI角色
                LoadUserAiSessions();

                // 如果创建了新角色，自动选中并处理
                if (form.CreatedAiCharacter != null)
                {
                    var createdSession = new ChatControl.Models.SessionInfo
                    {
                        Id = form.CreatedAiCharacter.Id,
                        AiName = form.CreatedAiCharacter.AiName,
                        AiGender = form.CreatedAiCharacter.AiGender,
                        AiPersona = form.CreatedAiCharacter.AiPersona,
                        AiStyle = form.CreatedAiCharacter.AiStyle,
                        AiHabit = form.CreatedAiCharacter.AiHabit,
                        AiOpening = form.CreatedAiCharacter.AiOpening
                    };

                    // 选中新创建的角色
                    SessionListUC_SessionSelected(createdSession);

                    // 检查是否有历史记录，如果没有则发送AI开场白
                    int messageCount = _userRepo.GetChatMessageCount(createdSession.Id, _loginUsername);
                    if (messageCount == 0 && !string.IsNullOrEmpty(createdSession.AiOpening))
                    {
                        // 发送AI开场白
                        chatMainControl1.AddAiMessage(createdSession.AiOpening);

                        // 存储AI开场白到数据库
                        var chatMessage = new ChatMessage
                        {
                            UserAccount = _loginUsername ?? string.Empty,
                            AiName = createdSession.AiName ?? string.Empty,
                            AiCharacterId = createdSession.Id,
                            Sender = "ai",
                            Content = createdSession.AiOpening,
                            CreateTime = DateTime.Now
                        };
                        _userRepo.AddChatMessage(chatMessage, out string errorMessage, out int messageId, out int openingTotalCount);
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            CheckAndGenerateMemory(openingTotalCount);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 当会话列表选中一项时，自动进来这里
        /// </summary>
        private void SessionListUC_SessionSelected(SessionInfo session)
        {
            // 1. 保存当前会话
            _currentSession = session;

            // 2. 拿到名字
            string aiName = session.AiName ?? string.Empty;

            // 3. 赋值给聊天界面的标签
            chatMainControl1.lbl_CurrentChatName.Text = aiName;

            // 4. 设置AI性别，用于动态显示颜色
            chatMainControl1.AiGender = session.AiGender;

            // 5. 清空当前聊天记录
            chatMainControl1.ClearMessages();

            // 6. 加载该会话的历史聊天记录
            LoadChatHistory(session.Id);

            // 7. 加载完成后滚动到底部，显示最新消息
            chatMainControl1.ScrollToBottom();
        }

        /// <summary>
        /// 加载聊天历史记录
        /// </summary>
        /// <param name="aiCharacterId">AI角色ID</param>
        private void LoadChatHistory(int aiCharacterId)
        {
            try
            {
                // 开始批量添加模式（性能优化）
                chatMainControl1.BeginBatchAddHistory();

                // 从数据库加载历史消息
                var historyMessages = _userRepo.LoadChatMessages(aiCharacterId, _loginUsername);
                
                Console.WriteLine($"LoadChatHistory: 从数据库加载到 {historyMessages.Count} 条消息");
                foreach (var msg in historyMessages)
                {
                    Console.WriteLine($"  - {msg.Sender}: {msg.Content?.Substring(0, Math.Min(20, msg.Content?.Length ?? 0))}...");
                }

                // 批量添加历史消息
                foreach (var message in historyMessages)
                {
                    chatMainControl1.AddHistoryMessage(
                        message.Content ?? string.Empty,
                        message.Sender == "user",
                        message.CreateTime,
                        message.Id,
                        message.TotalMessageCount
                    );
                }

                // 结束批量添加并更新界面
                chatMainControl1.EndBatchAddHistory();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载聊天历史失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 消息发送事件处理
        /// 用于存储消息到数据库
        /// </summary>
        private async void ChatMainControl_MessageSent(object? sender, ChatControl.Controls.MessageEventArgs e)
        {
            // 只有用户消息需要存储和处理
            if (e.IsSelf)
            {
                try
                {
                    // 如果当前会话为null，先创建"AI聊天助手"会话
                    if (_currentSession == null)
                    {
                        await HandleNoSessionSelected(e.Text, e.SendTime);
                        return;
                    }

                    // 存储用户消息到数据库
                    var chatMessage = new ChatMessage
                    {
                        UserAccount = _loginUsername ?? string.Empty,
                        AiName = _currentSession.AiName ?? string.Empty,
                        AiCharacterId = _currentSession.Id,
                        Sender = "user",
                        Content = e.Text ?? string.Empty,
                        CreateTime = e.SendTime
                    };

                    bool success = _userRepo.AddChatMessage(chatMessage, out string errorMessage, out int messageId, out int msgTotalCount);
                    if (!success && !string.IsNullOrEmpty(errorMessage))
                    {
                        Console.WriteLine($"存储用户消息失败：{errorMessage}");
                    }
                    else if (success)
                    {
                        // 更新UI中的消息对象，设置ID和TotalMessageCount
                        chatMainControl1.UpdateMessageInfo(messageId, msgTotalCount, e.Text, e.IsSelf, e.SendTime);
                        // 触发记忆生成检查
                        CheckAndGenerateMemory(msgTotalCount);

                        // 等待正在进行的记忆生成完成（确保记忆优先）
                        await WaitForMemoryGenerationAsync();

                        // 生成Json格式的消息字符串并保存到桌面（用于调试）
                        GenerateAndSaveJsonMessage(e.Text, _currentSession);

                        // 调用SendUserMessageToAI获取AI回复
                        SendUserMessageToAI(e.Text);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理消息发送事件异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理未选择会话时的消息发送
        /// </summary>
        private async Task HandleNoSessionSelected(string userMessage, DateTime sendTime)
        {
            // 如果没有选中会话，自动创建并使用"AI聊天助手"会话
            var sessionList = _userRepo.LoadAiCharacter(_loginUsername);
            var aiAssistantSession = sessionList.FirstOrDefault(s => s.AiName == "AI聊天助手");

            if (aiAssistantSession == null)
            {
                // 如果"AI聊天助手"会话不存在，创建它
                var aiCharacter = new AiCharacter
                {
                    UserAccount = _loginUsername,
                    AiName = "AI聊天助手",
                    AiGender = "",
                    AiPersona = "",
                    AiStyle = "",
                    AiHabit = "",
                    AiOpening = "你好，我是你的AI聊天助手，有什么我可以帮助你的吗？",
                    CreateTime = DateTime.Now
                };

                // 保存到数据库
                bool success = _userRepo.AddAiCharacter(aiCharacter, out string errorMessage);
                if (success)
                {
                    aiAssistantSession = new ChatControl.Models.SessionInfo
                    {
                        Id = aiCharacter.Id,
                        AiName = "AI聊天助手",
                        AiGender = "",
                        AiPersona = "",
                        AiStyle = "",
                        AiHabit = "",
                        AiOpening = "你好，我是你的AI聊天助手，有什么我可以帮助你的吗？"
                    };
                }
                else
                {
                    // 创建失败，显示错误消息
                    this.Invoke(new Action(() =>
                    {
                        chatMainControl1.AddAiMessage($"(创建会话失败：{errorMessage})\n请先创建一个会话后再发送消息。");
                        chatMainControl1.ScrollToBottom();
                    }));
                    return;
                }
            }

            // 保存当前会话
            _currentSession = aiAssistantSession;

            // 拿到名字
            string aiName = aiAssistantSession.AiName ?? string.Empty;

            // 赋值给聊天界面的标签
            chatMainControl1.lbl_CurrentChatName.Text = aiName;

            // 设置AI性别，用于动态显示颜色
            chatMainControl1.AiGender = aiAssistantSession.AiGender;

            // 重新加载会话列表，确保"AI聊天助手"显示在最前面
            LoadUserAiSessions(false);

            // 存储用户消息到数据库
            try
            {
                var chatMessage = new ChatMessage
                {
                    UserAccount = _loginUsername ?? string.Empty,
                    AiName = _currentSession.AiName ?? string.Empty,
                    AiCharacterId = _currentSession.Id,
                    Sender = "user",
                    Content = userMessage,
                    CreateTime = sendTime
                };
                _userRepo.AddChatMessage(chatMessage, out string errorMessage, out int messageId, out int userMsgTotalCount);
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Console.WriteLine($"存储用户消息失败：{errorMessage}");
                }
                else
                {
                    // 更新UI中的消息对象，设置ID和TotalMessageCount
                    chatMainControl1.UpdateMessageInfo(messageId, userMsgTotalCount, userMessage, true, sendTime);
                    // 触发记忆生成检查
                    CheckAndGenerateMemory(userMsgTotalCount);

                    // 等待正在进行的记忆生成完成（确保记忆优先）
                    await WaitForMemoryGenerationAsync();

                    // 生成Json格式的消息字符串并保存到桌面（用于调试）
                    GenerateAndSaveJsonMessage(userMessage, _currentSession);

                    // 调用SendUserMessageToAI获取AI回复
                    SendUserMessageToAI(userMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"存储用户消息异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 生成完整的Json请求包并保存为TXT文本文件到桌面
        /// </summary>
        /// <param name="messageText">消息文本</param>
        /// <param name="session">会话信息</param>
        private void GenerateAndSaveJsonMessage(string messageText, ChatControl.Models.SessionInfo session)
        {
            // 调用TextGenerator服务生成并保存Json请求包
            ChatAI.Services.TextGenerator.GenerateAndSaveJsonRequest(messageText, session, _loginUsername);
        }

        /// <summary>
        /// 重新发送用户消息到AI（用于回溯功能后让AI接上对话）
        /// </summary>
        /// <param name="userMessage">用户消息文本</param>
        private async void SendUserMessageToAI(string userMessage)
        {
            if (_currentSession == null || string.IsNullOrEmpty(userMessage))
            {
                Console.WriteLine("SendUserMessageToAI失败：会话或消息为空");
                return;
            }

            try
            {
                // 注意：不重新存储用户消息到数据库，因为这条消息已经存在
                // 只需要发送消息给AI并存储AI的回复
                Console.WriteLine("直接发送用户消息给AI，不重复存储到数据库");

                // 调用TextGenerator发送请求给大模型并获取回复
                string? aiResponse = await ChatAI.Services.TextGenerator.SendRequestAndGetResponse(
                    userMessage, _currentSession, _loginUsername);

                if (!string.IsNullOrEmpty(aiResponse))
                {
                    // 在UI线程上添加AI回复到聊天界面
                    this.Invoke(new Action(() =>
                    {
                        chatMainControl1.AddAiMessage(aiResponse);
                        chatMainControl1.ScrollToBottom();
                    }));

                    // 创建聊天消息实体（用于存储AI回复到数据库）
                    var aiChatMessage = new ChatMessage
                    {
                        UserAccount = _loginUsername ?? string.Empty,
                        AiName = _currentSession.AiName ?? string.Empty,
                        AiCharacterId = _currentSession.Id,
                        Sender = "ai",
                        Content = aiResponse,
                        CreateTime = DateTime.Now
                    };

                    // 存储AI回复到数据库
                    _userRepo.AddChatMessage(aiChatMessage, out string aiErrorMessage, out int aiMessageId, out int aiTotalCount);

                    if (!string.IsNullOrEmpty(aiErrorMessage))
                    {
                        Console.WriteLine($"存储AI回复失败：{aiErrorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"AI回复已存储：MessageId={aiMessageId}, TotalCount={aiTotalCount}");
                    }

                    // 触发记忆生成检查
                    CheckAndGenerateMemory(aiTotalCount);
                }
                else
                {
                    // 如果获取回复失败，添加错误提示
                    this.Invoke(new Action(() =>
                    {
                        chatMainControl1.AddAiMessage("(抱歉，模型暂时无法回复，请稍后再试。)");
                        chatMainControl1.ScrollToBottom();
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendUserMessageToAI异常：{ex.Message}");
                // 添加错误提示
                this.Invoke(new Action(() =>
                {
                    chatMainControl1.AddAiMessage("(抱歉，模型暂时无法回复，请稍后再试。)");
                    chatMainControl1.ScrollToBottom();
                }));
            }
        }

        /// <summary>
        /// 重新发送用户消息到AI（用于回溯功能后让AI接上对话）
        /// 使用指定的会话信息，确保请求体从数据库获取最新消息
        /// </summary>
        /// <param name="userMessage">用户消息文本</param>
        /// <param name="session">会话信息</param>
        private async void SendUserMessageToAI(string userMessage, ChatControl.Models.SessionInfo session)
        {
            if (session == null || string.IsNullOrEmpty(userMessage))
            {
                Console.WriteLine("SendUserMessageToAI失败：会话或消息为空");
                return;
            }

            try
            {
                // 注意：不重新存储用户消息到数据库，因为这条消息已经存在
                // 只需要发送消息给AI并存储AI的回复
                Console.WriteLine("回溯后直接发送用户消息给AI，不重复存储到数据库");

                // 调用TextGenerator发送请求给大模型并获取回复
                // 使用传入的session参数，确保请求体从数据库获取最新消息
                string? aiResponse = await ChatAI.Services.TextGenerator.SendRequestAndGetResponse(
                    userMessage, session, _loginUsername);

                if (!string.IsNullOrEmpty(aiResponse))
                {
                    // 在UI线程上添加AI回复到聊天界面
                    this.Invoke(new Action(() =>
                    {
                        chatMainControl1.AddAiMessage(aiResponse);
                        chatMainControl1.ScrollToBottom();
                    }));

                    // 创建聊天消息实体（用于存储AI回复到数据库）
                    var aiChatMessage = new ChatMessage
                    {
                        UserAccount = _loginUsername ?? string.Empty,
                        AiName = session.AiName ?? string.Empty,
                        AiCharacterId = session.Id,
                        Sender = "ai",
                        Content = aiResponse,
                        CreateTime = DateTime.Now
                    };

                    // 存储AI回复到数据库
                    _userRepo.AddChatMessage(aiChatMessage, out string aiErrorMessage, out int aiMessageId, out int aiTotalCount);

                    if (!string.IsNullOrEmpty(aiErrorMessage))
                    {
                        Console.WriteLine($"存储AI回复失败：{aiErrorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"AI回复已存储：MessageId={aiMessageId}, TotalCount={aiTotalCount}");
                    }

                    // 触发记忆生成检查
                    CheckAndGenerateMemory(aiTotalCount);
                }
                else
                {
                    // 如果获取回复失败，添加错误提示
                    this.Invoke(new Action(() =>
                    {
                        chatMainControl1.AddAiMessage("(抱歉，模型暂时无法回复，请稍后再试。)");
                        chatMainControl1.ScrollToBottom();
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendUserMessageToAI异常：{ex.Message}");
                // 添加错误提示
                this.Invoke(new Action(() =>
                {
                    chatMainControl1.AddAiMessage("(抱歉，模型暂时无法回复，请稍后再试。)");
                    chatMainControl1.ScrollToBottom();
                }));
            }
        }

        /// <summary>
        /// 检查是否需要触发记忆生成（异步版本，可等待完成）
        /// 基于存储消息时计算的newTotalCount值来判断，当达到memoryTrigger的整数倍时触发
        /// </summary>
        /// <param name="newTotalCount">新消息的total_message_count值</param>
        /// <returns>记忆生成任务</returns>
        private async Task CheckAndGenerateMemoryAsync(int newTotalCount)
        {
            bool shouldStartNewTask = false;
            bool isGenerating = false;

            // 使用锁确保线程安全
            lock (_memoryLock)
            {
                if (_isMemoryGenerating)
                {
                    // 如果正在生成记忆，将请求加入队列等待处理
                    if (!_pendingMemoryRequests.Contains(newTotalCount))
                    {
                        _pendingMemoryRequests.Enqueue(newTotalCount);
                        Console.WriteLine($"记忆生成正在进行中，将请求({newTotalCount})加入等待队列");
                    }
                    isGenerating = true;
                }
                else
                {
                    _isMemoryGenerating = true;
                    shouldStartNewTask = true;
                }
            }

            // 如果正在生成，等待完成
            if (isGenerating)
            {
                while (true)
                {
                    Task? currentTask = null;
                    lock (_memoryLock)
                    {
                        currentTask = _currentMemoryTask;
                    }
                    
                    if (currentTask == null)
                        break;
                        
                    try
                    {
                        await currentTask;
                    }
                    catch { }
                }
                return;
            }

            // 执行记忆生成（同步等待完成）
            if (shouldStartNewTask)
            {
                _currentMemoryTask = ProcessMemoryGeneration(newTotalCount);
                await _currentMemoryTask;
            }
        }

        /// <summary>
        /// 检查是否需要触发记忆生成（非阻塞版本）
        /// 基于存储消息时计算的newTotalCount值来判断，当达到memoryTrigger的整数倍时触发
        /// </summary>
        /// <param name="newTotalCount">新消息的total_message_count值</param>
        private void CheckAndGenerateMemory(int newTotalCount)
        {
            // 启动异步任务但不等待
            _ = CheckAndGenerateMemoryAsync(newTotalCount);
        }

        /// <summary>
        /// 等待正在进行的记忆生成完成
        /// </summary>
        private async Task WaitForMemoryGenerationAsync()
        {
            lock (_memoryLock)
            {
                if (!_isMemoryGenerating || _currentMemoryTask == null)
                    return;
            }
            
            try
            {
                await _currentMemoryTask;
            }
            catch { }
        }

        /// <summary>
        /// 处理记忆生成任务（包括队列中的等待请求）
        /// </summary>
        private async Task ProcessMemoryGeneration(int initialCount)
        {
            try
            {
                // 处理初始请求
                await ExecuteMemoryGeneration(initialCount);

                // 处理队列中的等待请求
                while (true)
                {
                    int nextCount = 0;
                    lock (_memoryLock)
                    {
                        if (_pendingMemoryRequests.Count == 0)
                            break;
                        nextCount = _pendingMemoryRequests.Dequeue();
                    }

                    Console.WriteLine($"处理队列中的记忆生成请求({nextCount})");
                    await ExecuteMemoryGeneration(nextCount);
                }
            }
            finally
            {
                // 重置标记，允许下次触发
                lock (_memoryLock)
                {
                    _isMemoryGenerating = false;
                }
            }
        }

        /// <summary>
        /// 执行单次记忆生成
        /// </summary>
        private async Task ExecuteMemoryGeneration(int newTotalCount)
        {
            try
            {
                // 获取会话设置
                var sessionSettings = _userRepo.LoadSessionSettings(_loginUsername);
                int memoryTrigger = sessionSettings?.MemoryTrigger ?? 10;

                // 只有当当前会话不为null时，才检查是否需要生成记忆
                if (_currentSession != null)
                {
                    // 当newTotalCount % memoryTrigger == 0时触发记忆生成
                    if (memoryTrigger > 0 && newTotalCount % memoryTrigger == 0)
                    {
                        Console.WriteLine($"达到记忆触发阈值({newTotalCount})，开始生成用户记忆...");

                        // 延迟1秒确保用户消息完全写入数据库，避免并发冲突
                        await Task.Delay(1000);

                        // 调用TextGenerator生成记忆（使用Qwen-7B格式）
                        Console.WriteLine($"=== 开始调用记忆生成方法 ===");
                        bool success = await ChatAI.Services.TextGenerator.GenerateMemoryWithQwenFormatAsync(_currentSession, _loginUsername);
                        Console.WriteLine($"=== 记忆生成方法调用完成，结果: {success} ===");

                        if (success)
                        {
                            Console.WriteLine("用户记忆生成并保存成功！");
                        }
                        else
                        {
                            Console.WriteLine("用户记忆生成失败，将在下一次达到阈值时重试。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行记忆生成异常：{ex.Message}");
            }
        }

        private void tsmi_MainMenu_Logout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定退出登录吗？", "退出登录", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                IsLogout = true;
                this.Close();
            }
        }

        private void tsmi_MainMenu_APIConfig_Click(object sender, EventArgs e)
        {
            FrmApiConfig form = new FrmApiConfig(_loginUsername);
            form.ShowDialog();
        }

        private void FrmChatMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 只有 点右上角× + 不是退出登录 → 才退出程序
            if (e.CloseReason == CloseReason.UserClosing && !IsLogout)
            {
                Program.IsExitApplication = true;
            }
        }

        /// <summary>
        /// 聊天名称标签点击事件
        /// </summary>
        private void lbl_CurrentChatName_Click(object? sender, EventArgs e)
        {
            if (_currentSession == null)
            {
                MessageBox.Show("请先选择一个会话", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 1. 获取当前会话的ID
                int aiId = _currentSession.Id;

                // 2. 调用仓储获取完整的AI角色信息
                AiCharacter? aiCharacter = _userRepo.GetAiCharacterById(aiId);

                if (aiCharacter == null)
                {
                    MessageBox.Show("获取AI角色信息失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. 创建编辑窗口
                CreateSessionForm form = new CreateSessionForm(_loginUsername, aiCharacter);

                // 4. 显示窗口
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // 5. 编辑成功，刷新会话列表
                    LoadUserAiSessions();

                    // 6. 重新加载当前会话数据，确保更新后的数据能立即生效
                    // 重新获取会话信息
                    var updatedSession = _userRepo.LoadAiCharacter(_loginUsername).Find(s => s.Id == aiId);
                    if (updatedSession != null)
                    {
                        // 重新加载当前会话数据
                        SessionListUC_SessionSelected(updatedSession);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑窗口失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tsmi_UserIdentitySetting_Click(object sender, EventArgs e)
        {
            FrmUserProfileSettings form = new FrmUserProfileSettings(_loginUsername);
            form.ShowDialog();
        }

        private void tsmi_MainMenu_SessionSetting_Click(object sender, EventArgs e)
        {
            FrmSessionSettings form = new FrmSessionSettings(_loginUsername);
            form.ShowDialog();
        }

        /// <summary>
        /// 删除会话事件处理
        /// </summary>
        private void SessionListUC_DeleteSession(ChatControl.Models.SessionInfo session)
        {
            if (session == null)
                return;

            // 弹出确认对话框
            var result = MessageBox.Show(
                $"确定要删除与「{session.AiName}」的会话吗？\n\n删除后，将无法恢复该会话的所有聊天记录和记忆。",
                "删除会话确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                // 执行删除操作
                bool success = _userRepo.DeleteSession(session.Id, _loginUsername, out string errorMessage);

                if (success)
                {
                    MessageBox.Show($"会话「{session.AiName}」已删除。", "删除成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 如果删除的是当前选中的会话，清空聊天区域
                    if (_currentSession != null && _currentSession.Id == session.Id)
                    {
                        _currentSession = null;
                        chatMainControl1.ClearMessages();
                        chatMainControl1.lbl_CurrentChatName.Text = string.Empty;
                    }

                    // 重新加载会话列表
                    LoadUserAiSessions();
                }
                else
                {
                    MessageBox.Show($"删除会话失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 编辑会话事件处理
        /// </summary>
        private void SessionListUC_EditSession(ChatControl.Models.SessionInfo session)
        {
            if (session == null)
                return;

            try
            {
                // 1. 获取AI角色信息
                var aiCharacter = _userRepo.GetAiCharacterById(session.Id);
                if (aiCharacter == null)
                {
                    MessageBox.Show("获取AI角色信息失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. 创建编辑窗口
                CreateSessionForm form = new CreateSessionForm(_loginUsername, aiCharacter);

                // 3. 显示窗口
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // 4. 编辑成功，刷新会话列表
                    LoadUserAiSessions();

                    // 5. 重新加载当前会话数据，确保更新后的数据能立即生效
                    var updatedSession = _userRepo.LoadAiCharacter(_loginUsername).Find(s => s.Id == session.Id);
                    if (updatedSession != null)
                    {
                        SessionListUC_SessionSelected(updatedSession);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑窗口失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 查看记忆事件处理
        /// </summary>
        private void SessionListUC_ViewMemory(ChatControl.Models.SessionInfo session)
        {
            if (session == null)
                return;

            try
            {
                // 创建记忆管理窗口
                using var form = new FrmMemoryManager();
                form.LoginUsername = _loginUsername;
                form.SelectedSessionId = session.Id;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开记忆设置窗口失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// AI记忆设置菜单项点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmi_AiMemorySettings_Click(object sender, EventArgs e)
        {
            using var form = new FrmMemoryManager();
            form.LoginUsername = _loginUsername;
            form.ShowDialog();
        }

        /// <summary>
        /// 重新生成响应事件处理
        /// 删除数据库中的最后一条AI消息，并重新发送用户的最后一条消息让AI回复
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChatMainControl_RegenerateResponseRequested(object? sender, EventArgs e)
        {
            if (_currentSession != null)
            {
                Console.WriteLine("=== 开始重新生成响应 ===");
                
                // 删除数据库中的最后一条AI消息
                bool success = _userRepo.DeleteLastAiMessage(_loginUsername, _currentSession.Id, out string errorMessage);
                if (!success && !string.IsNullOrEmpty(errorMessage))
                {
                    Console.WriteLine($"删除AI消息失败：{errorMessage}");
                    return;
                }
                Console.WriteLine("已删除数据库中的最后一条AI消息");
                
                // 获取用户的最后一条消息
                string lastUserMessage = _userRepo.GetLastUserMessage(_loginUsername, _currentSession.Id);
                Console.WriteLine($"获取到用户最后一条消息: {(string.IsNullOrEmpty(lastUserMessage) ? "空" : lastUserMessage.Substring(0, Math.Min(50, lastUserMessage.Length)) + "...")}");
                
                if (!string.IsNullOrEmpty(lastUserMessage))
                {
                    // 注意：不需要调用 LoadChatHistory，因为 ChatControl 的 RegenerateResponse 方法
                    // 已经正确删除了UI上的AI消息并保留了用户消息
                    // 直接发送用户消息让AI回复即可
                    Console.WriteLine("直接发送用户消息给AI重新生成响应...");
                    SendUserMessageToAI(lastUserMessage, _currentSession);
                }
                else
                {
                    Console.WriteLine("未找到用户的最后一条消息，无法重新生成响应");
                }
                
                Console.WriteLine("=== 重新生成响应流程结束 ===");
            }
        }

        /// <summary>
        /// 回溯事件处理
        /// 删除选中消息及其后续所有消息和记忆
        /// </summary>
        private void ChatMainControl_MessageRollbackRequested(object? sender, ChatControl.Controls.MessageRollbackEventArgs e)
        {
            if (_currentSession == null)
            {
                Console.WriteLine("回溯失败：当前会话为null");
                return;
            }

            Console.WriteLine($"回溯请求：MessageId={e.MessageId}, TotalMessageCount={e.TotalMessageCount}, IsUserMessage={e.IsUserMessage}");
            Console.WriteLine($"回溯消息内容：{e.MessageText}");
            Console.WriteLine($"回溯消息时间：{e.SendTime.ToString("yyyy-MM-dd HH:mm:ss")}");

            // 删除选中消息之后的所有消息（包括数据库和UI）
            // 使用消息文本和发送时间定位消息（与UI删除逻辑保持一致）
            bool dbDeleteSuccess = _userRepo.DeleteMessagesAfter(e.MessageText, e.SendTime, _currentSession.Id, _loginUsername, out string errorMessage);
            if (dbDeleteSuccess)
            {
                Console.WriteLine($"成功删除消息[{e.MessageText.Substring(0, Math.Min(20, e.MessageText.Length))}...]之后的所有消息");
            }
            else
            {
                Console.WriteLine($"删除消息失败：{(string.IsNullOrEmpty(errorMessage) ? "未找到匹配的消息" : errorMessage)}");
            }

            // 删除回溯点之后生成的记忆
            _userRepo.DeleteMemoriesAfterMessage(_currentSession.Id, _loginUsername, e.TotalMessageCount, out string memoryErrorMessage);
            if (!string.IsNullOrEmpty(memoryErrorMessage))
            {
                Console.WriteLine($"删除记忆失败：{memoryErrorMessage}");
            }

            // 如果是用户消息，需要重新发送该消息让AI接上对话
            if (e.IsUserMessage)
            {
                Console.WriteLine("触发重新发送用户消息...");
                // 获取右键点击的用户消息内容
                string userMessage = chatMainControl1.GetSelectedMessageText();
                if (!string.IsNullOrEmpty(userMessage))
                {
                    // 重新加载历史消息到UI，确保使用最新的数据库数据（已删除回溯点之后的消息）
                    Console.WriteLine("重新加载历史消息...");
                    LoadChatHistory(_currentSession.Id);
                    
                    // 直接发送用户消息，TextGenerator会从数据库获取最新消息作为上下文
                    SendUserMessageToAI(userMessage, _currentSession);
                }
            }
        }
    }
}