using ChatAI.Data;
using ChatAI.Data.Entities;
using ChatAI.Services.ModelFormats;
using ChatControl.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatAI.Services
{
    /// <summary>
    /// 本地模型服务单例（LocalModelServiceSingleton）
    /// 
    /// 提供线程安全的单例访问，确保整个应用中只有一个LocalModelService实例。
    /// 使用双重检查锁定（Double-Checked Locking）模式保证线程安全。
    /// </summary>
    public static class LocalModelServiceSingleton
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static LocalModelService? _instance;

        /// <summary>
        /// 线程同步锁
        /// </summary>
        private static readonly object _lock = new object();
        
        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static LocalModelService Instance
        {
            get
            {
                // 第一次检查（无锁）
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        // 第二次检查（有锁）
                        if (_instance == null)
                        {
                            try
                            {
                                _instance = new LocalModelService();
                                Console.WriteLine("LocalModelService 单例初始化成功");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"LocalModelService 单例初始化失败: {ex.Message}");
                                Console.WriteLine($"异常类型: {ex.GetType().Name}");
                                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                                throw;
                            }
                        }
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// 文本生成服务（TextGenerator）
    /// 
    /// 该类负责：
    /// 1. 构建请求消息（包含历史对话、AI角色信息、用户信息、长期记忆）
    /// 2. 发送请求到本地模型或云端API
    /// 3. 解析响应并提取AI回复内容
    /// 4. 生成和保存AI长期记忆
    /// 
    /// 支持两种模式：
    /// - 本地模型模式：使用LLamaSharp调用本地GGUF格式模型
    /// - 云端API模式：调用远程大模型API（如SiliconFlow）
    /// </summary>
    public class TextGenerator
    {
        /// <summary>
        /// 生成完整的请求信息并保存为TXT文本文件到桌面Json文件夹（仅用于调试）
        /// 
        /// 根据配置选择本地模型格式或云端API格式，将请求内容保存到桌面的Json文件夹。
        /// </summary>
        /// <param name="messageText">消息文本</param>
        /// <param name="session">会话信息</param>
        /// <param name="loginUsername">登录用户名</param>
        public static void GenerateAndSaveJsonRequest(string messageText, SessionInfo session, string loginUsername)
        {
            // 获取API配置、会话设置和用户配置信息
            var userRepo = new UserRepository();
            var apiConfig = userRepo.LoadApiConfig(loginUsername);
            var userProfile = userRepo.GetUserProfile(loginUsername);
            var sessionSettings = userRepo.LoadSessionSettings(loginUsername);

            // 从数据库重新加载最新的会话信息，确保使用最新的AI角色数据
            var updatedSession = userRepo.LoadAiCharacter(loginUsername).Find(s => s.Id == session.Id) ?? session;

            // 检查是否禁用角色信息（纯净模式或AI聊天助手）
            bool noRole = sessionSettings?.NoRole ?? false;
            bool isAiAssistant = updatedSession.AiName == "AI聊天助手";

            // 获取AI的长期记忆（最多5条）
            List<string> aiMemories = userRepo.GetAIMemories(loginUsername, updatedSession.Id, 5);

            // 构建system消息内容（纯净模式只包含记忆，不包含人设信息）
            string systemContent;
            if (noRole || isAiAssistant)
            {
                // 纯净模式：只包含记忆
                StringBuilder sb = new StringBuilder();
                if (aiMemories != null && aiMemories.Count > 0)
                {
                    sb.AppendLine("【AI的长期记忆】");
                    for (int i = 0; i < aiMemories.Count; i++)
                    {
                        sb.AppendLine($"【记忆{i + 1}】");
                        sb.AppendLine(aiMemories[i]);
                        sb.AppendLine();
                    }
                }
                systemContent = sb.ToString();
            }
            else
            {
                // 非纯净模式：包含记忆、人设、用户信息
                systemContent = BuildSystemContent(updatedSession, userProfile, aiMemories);
            }

            // 获取历史消息（根据用户配置的historyCount截取，多获取一条以便排除用户最后一条消息）
            int historyCount = sessionSettings?.HistoryCount ?? 10;
            var historyMessages = userRepo.GetRecentChatMessages(session.Id, loginUsername, historyCount + 1);

            // 检查并移除用户的最后一条消息（因为它会在后面单独添加）
            if (historyMessages.Count > 0)
            {
                var lastMessage = historyMessages[historyMessages.Count - 1];
                if (lastMessage.Sender == "user")
                {
                    historyMessages.RemoveAt(historyMessages.Count - 1);
                }
            }

            // 构建消息列表
            var messagesList = new List<object>();

            // 只有当systemContent不为空时才添加system消息（纯净模式下如果没有记忆则不添加）
            if (!string.IsNullOrWhiteSpace(systemContent))
            {
                messagesList.Add(new { role = "system", content = systemContent });
            }

            // 添加聊天历史记录（根据用户设置的上下文条目数）
            foreach (var msg in historyMessages)
            {
                string role = msg.Sender == "user" ? "user" : "assistant";
                messagesList.Add(new { role = role, content = msg.Content ?? string.Empty });
            }

            // 添加用户最新提问
            messagesList.Add(new { role = "user", content = messageText });

            // 获取温度和最大tokens（使用用户配置的值）
            double temperature = sessionSettings?.Temperature ?? 0.7;
            int maxTokens = sessionSettings?.MaxPromptTokens ?? 1000;

            // 构建消息对象
            var messageObj = new
            {
                model = apiConfig?.Model ?? "deepseek-ai/DeepSeek-V3",
                messages = messagesList.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens
            };

            // 序列化为Json字符串
            string jsonString = System.Text.Json.JsonSerializer.Serialize(messageObj, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // 保存到Json文件夹
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string jsonFolderPath = Path.Combine(desktopPath, "Json");
            if (!Directory.Exists(jsonFolderPath))
            {
                Directory.CreateDirectory(jsonFolderPath);
            }

            // 判断是否使用本地模型
            bool useLocalModel = apiConfig?.EnableLocalModel ?? false;

            if (useLocalModel)
            {
                // 提前定义变量，使其在 catch 块中也可访问
                bool isHermesModel = false;
                string modelName = apiConfig?.Model ?? "Qwen-7B-Chat";
                
                try
                {
                    // 本地模型：使用OpenAI通用格式保存请求体
                    string modelPath = apiConfig.Endpoint ?? string.Empty;
                    
                    // 根据模型路径确定模型类型（用于选择格式和停止词）
                    isHermesModel = !modelPath.Contains("qwen", StringComparison.OrdinalIgnoreCase);
                    
                    // 如果使用本地模型，从路径中提取实际的模型文件名作为请求体中的model值
                    if (!string.IsNullOrEmpty(modelPath))
                    {
                        string modelFileName = Path.GetFileName(modelPath);
                        // 去掉文件扩展名（如 .gguf）
                        modelName = Path.GetFileNameWithoutExtension(modelFileName);
                    }
                    
                    // 获取模型格式以获取正确的停止词
                    var modelFormat = ModelFormatFactory.GetFormat(modelPath);
                    if (modelFormat == null)
                    {
                        modelFormat = new QwenModelFormat(); // 使用默认格式
                    }
                    
                    // 获取停止词
                    string[] stopWords = modelFormat.GetStopWords() ?? new string[0];
                    
                    // 构建消息列表
                    List<object> localMessages;
                    if (isHermesModel)
                    {
                        localMessages = new List<object>();
                        // 获取system内容
                        string localSystemContent = string.Empty;
                        foreach (var msg in messagesList)
                        {
                            var roleProp = msg.GetType().GetProperty("role");
                            var contentProp = msg.GetType().GetProperty("content");
                            if (roleProp != null && contentProp != null)
                            {
                                string role = roleProp.GetValue(msg)?.ToString() ?? string.Empty;
                                if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
                                {
                                    localSystemContent = contentProp.GetValue(msg)?.ToString() ?? string.Empty;
                                    // 压缩system内容，移除多余换行和空行
                                    localSystemContent = System.Text.RegularExpressions.Regex.Replace(localSystemContent, @"\r\n", "\n");
                                    localSystemContent = System.Text.RegularExpressions.Regex.Replace(localSystemContent, @"\n{2,}", "\n");
                                    localSystemContent = localSystemContent.Trim();
                                    break;
                                }
                            }
                        }
                        
                        // 添加system消息
                        if (!string.IsNullOrWhiteSpace(localSystemContent))
                        {
                            localMessages.Add(new { role = "system", content = localSystemContent });
                        }
                        
                        // 添加最后一条用户消息
                        if (!string.IsNullOrWhiteSpace(messageText))
                        {
                            localMessages.Add(new { role = "user", content = messageText.Trim() });
                        }
                    }
                    else
                    {
                        // Qwen模型保留完整历史
                        localMessages = messagesList.ToList();
                    }
                    
                    // 使用用户设置的max_tokens值
                    int localMaxTokens = maxTokens;
                    
                    var localRequest = new
                    {
                        model = modelName,
                        messages = localMessages.ToArray(),
                        temperature = temperature,
                        max_tokens = localMaxTokens,
                        stop = stopWords
                    };
                    
                    string localRequestJson = System.Text.Json.JsonSerializer.Serialize(localRequest, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    
                    string fileName = $"local_model_request_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    string filePath = Path.Combine(jsonFolderPath, fileName);
                    File.WriteAllText(filePath, localRequestJson);
                }
                catch (Exception ex)
                    {
                        Console.WriteLine($"生成本地模型请求体失败: {ex.Message}");
                        // 使用简化格式作为备选
                        var fallbackRequest = new
                        {
                            model = modelName, // 使用数据库中配置的模型名称
                            messages = new[]
                            {
                                new { role = "user", content = messageText ?? "Hello" }
                            },
                            temperature = temperature,
                            max_tokens = maxTokens
                        };
                    
                    string fallbackJson = System.Text.Json.JsonSerializer.Serialize(fallbackRequest, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    string fileName = $"local_model_request_{DateTime.Now:yyyyMMdd_HHmmss}_fallback.txt";
                    string filePath = Path.Combine(jsonFolderPath, fileName);
                    File.WriteAllText(filePath, fallbackJson);
                }
            }
            else
            {
                // 云端模型：保存完整的HTTP请求信息
                StringBuilder sb = new StringBuilder();
                string baseUrl = apiConfig?.Endpoint ?? "https://api.siliconflow.cn/v1";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                string fullUrl = baseUrl + "chat/completions";
                sb.AppendLine($"POST {fullUrl}");
                sb.AppendLine();
                sb.AppendLine("Headers:");
                sb.AppendLine("  Content-Type: application/json");
                sb.AppendLine($"  Authorization: Bearer {apiConfig?.ApiKey ?? "未配置"}");
                sb.AppendLine();
                sb.AppendLine(jsonString);

                string fileName = $"chat_request_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(jsonFolderPath, fileName);
                File.WriteAllText(filePath, sb.ToString());
            }
        }

        /// <summary>
        /// 发送Json请求到模型并获取回复，同时保存请求和响应到Json文件夹
        /// 
        /// 这是文本生成的核心入口方法，负责：
        /// 1. 获取配置信息（API配置、会话设置、用户配置）
        /// 2. 构建请求消息（包含历史对话、记忆、角色信息）
        /// 3. 根据配置选择本地模型或云端API
        /// 4. 发送请求并解析响应
        /// </summary>
        /// <param name="messageText">消息文本</param>
        /// <param name="session">会话信息</param>
        /// <param name="loginUsername">登录用户名</param>
        /// <returns>模型回复的content内容，如果失败则返回null</returns>
        public static async Task<string?> SendRequestAndGetResponse(string messageText, SessionInfo session, string loginUsername)
        {
            Console.WriteLine($"========== SendRequestAndGetResponse 开始 ==========");
            Console.WriteLine($"消息文本: '{messageText}'");
            Console.WriteLine($"会话ID: {session.Id}, 会话名称: '{session.AiName}'");
            Console.WriteLine($"登录用户名: '{loginUsername}'");
            
            // 获取API配置、会话设置和用户配置信息
            var userRepo = new UserRepository();
            var apiConfig = userRepo.LoadApiConfig(loginUsername);
            var userProfile = userRepo.GetUserProfile(loginUsername);
            var sessionSettings = userRepo.LoadSessionSettings(loginUsername);

            // 调试：输出API配置
            Console.WriteLine($"API配置信息:");
            Console.WriteLine($"  apiConfig是否为空: {apiConfig == null}");
            if (apiConfig != null)
            {
                Console.WriteLine($"  EnableLocalModel: {apiConfig.EnableLocalModel} (Nullable<bool>)");
                Console.WriteLine($"  EnableLocalModel.HasValue: {apiConfig.EnableLocalModel.HasValue}");
                Console.WriteLine($"  EnableLocalModel.Value: {apiConfig.EnableLocalModel.GetValueOrDefault(false)}");
                Console.WriteLine($"  Provider: '{apiConfig.Provider ?? "null"}'");
                Console.WriteLine($"  Endpoint: '{apiConfig.Endpoint ?? "null"}'");
                Console.WriteLine($"  Model: '{apiConfig.Model ?? "null"}'");
                Console.WriteLine($"  ApiKey: '{apiConfig.ApiKey ?? "null"}'");
                Console.WriteLine($"  LocalModelMemoryLimit: {apiConfig.LocalModelMemoryLimit}");
            }
            else
            {
                Console.WriteLine("  apiConfig为空，无法使用本地模型！");
            }

            // 检查是否禁用角色信息
            bool noRole = sessionSettings?.NoRole ?? false;

            // 从数据库重新加载最新的会话信息，确保使用最新的AI角色数据
            var updatedSession = userRepo.LoadAiCharacter(loginUsername).Find(s => s.Id == session.Id) ?? session;

            // 获取历史消息（根据用户配置的historyCount截取，多获取一条以便排除用户最后一条消息）
            int historyCount = sessionSettings?.HistoryCount ?? 10;
            var historyMessages = userRepo.GetRecentChatMessages(session.Id, loginUsername, historyCount + 1);

            // 检查并移除用户的最后一条消息（因为它会在后面单独添加）
            if (historyMessages.Count > 0)
            {
                var lastMessage = historyMessages[historyMessages.Count - 1];
                if (lastMessage.Sender == "user")
                {
                    historyMessages.RemoveAt(historyMessages.Count - 1);
                }
            }

            // 获取温度和最大tokens（使用用户配置的值）
            double temperature = sessionSettings?.Temperature ?? 0.7;
            int maxTokens = sessionSettings?.MaxPromptTokens ?? 1000;

            // 检查是否使用本地模型
            bool useLocalModel = apiConfig?.EnableLocalModel ?? false;

            // 使用数据库中配置的模型名称
            string modelName = apiConfig?.Model ?? "deepseek-ai/DeepSeek-V3";

            // 构建消息对象（根据是否启用角色信息选择不同的构建方式）
            bool isAiAssistant = updatedSession.AiName == "AI聊天助手";
            var messageObj = noRole || isAiAssistant ? BuildNoRoleMessageObject(messageText, updatedSession, userProfile, historyMessages, apiConfig, temperature, maxTokens, modelName) :
                BuildMessageObject(messageText, updatedSession, userProfile, historyMessages, apiConfig, temperature, maxTokens, modelName);

            // 序列化为Json字符串
            string jsonString = System.Text.Json.JsonSerializer.Serialize(messageObj, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            // 检查是否使用本地模型
            Console.WriteLine($"是否使用本地模型: {apiConfig?.EnableLocalModel ?? false}");
            
            if (apiConfig == null)
            {
                Console.WriteLine("错误：API配置为空，无法调用本地模型");
            }
            else if (!useLocalModel)
            {
                Console.WriteLine("本地模型未启用，使用云端API...");
            }
            
            Console.WriteLine($"是否使用本地模型(最终判定): {useLocalModel}");
            
            if (useLocalModel)
            {
                Console.WriteLine("调用本地模型服务...");
                Console.WriteLine($"请求体长度: {jsonString.Length} 字符");
                
                // 添加性能计时
                DateTime startTime = DateTime.Now;
                string? localResponse = await SendRequestToLocalModel(jsonString, apiConfig!, sessionSettings);
                TimeSpan duration = DateTime.Now - startTime;
                
                Console.WriteLine($"本地模型响应耗时: {duration.TotalSeconds:F2}秒");
                Console.WriteLine($"本地模型响应: {(localResponse != null ? $"成功，长度: {localResponse.Length}" : "失败")}");
                return localResponse;
            }
            
            Console.WriteLine("使用云端API...");
            Console.WriteLine($"========== SendRequestAndGetResponse 结束（云端） ==========");

            // 构建请求地址
            string baseUrl = apiConfig?.Endpoint ?? "https://api.siliconflow.cn/v1";
            if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }
            string fullUrl = baseUrl + "chat/completions";

            // 创建HTTP客户端
            using var client = new HttpClient();

            // 设置请求头
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiConfig?.ApiKey ?? "");

            // 创建请求内容
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // 发送请求
            var response = await client.PostAsync(fullUrl, content);

            // 读取响应内容
            string responseContent = await response.Content.ReadAsStringAsync();

            // 保存云端API响应到Json文件夹
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string jsonFolderPath = Path.Combine(desktopPath, "Json");
            if (!Directory.Exists(jsonFolderPath))
            {
                Directory.CreateDirectory(jsonFolderPath);
            }
            string responseFileName = $"chat_response_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string responseFilePath = Path.Combine(jsonFolderPath, responseFileName);
            File.WriteAllText(responseFilePath, responseContent);

            // 解析响应，提取content内容
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    // 直接读取content字段获取模型回答
                    return contentProp.GetString();
                }
            }
            catch
            {
                // 解析失败，返回原始响应
            }

            return null;
        }

        /// <summary>
        /// 发送请求到本地模型
        /// 
        /// 负责：
        /// 1. 检查本地模型是否已初始化
        /// 2. 如果未初始化，执行初始化流程
        /// 3. 从JSON请求中提取prompt
        /// 4. 调用本地模型生成响应（带重试机制）
        /// 5. 保存响应到文件
        /// </summary>
        /// <param name="jsonString">JSON格式的请求</param>
        /// <param name="apiConfig">API配置（本地模型使用Endpoint作为模型路径，ApiKey作为GPU模式）</param>
        /// <param name="sessionSettings">会话设置</param>
        /// <returns>模型响应</returns>
        private static async Task<string?> SendRequestToLocalModel(string jsonString, ApiConfig? apiConfig, SessionSettings? sessionSettings)
        {
            try
            {
                Console.WriteLine("========== 本地模型调用开始 ==========");
                
                if (apiConfig == null)
                {
                    Console.WriteLine("错误：API配置为空");
                    return null;
                }

                Console.WriteLine($"EnableLocalModel: {apiConfig.EnableLocalModel}");
                Console.WriteLine($"模型路径: {apiConfig.Endpoint}");
                Console.WriteLine($"GPU模式: {apiConfig.ApiKey}");
                Console.WriteLine($"内存限制: {apiConfig.LocalModelMemoryLimit} MB");

                // 获取模型路径
                string modelPath = apiConfig.Endpoint ?? string.Empty;
                Console.WriteLine($"模型路径(trimmed): '{modelPath}'");

                // 确保本地模型已初始化
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 检查本地模型状态...");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] IsInitialized: {LocalModelServiceSingleton.Instance.IsInitialized}");
                
                if (!LocalModelServiceSingleton.Instance.IsInitialized)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 本地模型未初始化，开始初始化...");
                    string gpuMode = apiConfig.ApiKey ?? "自动选择（推荐）";
                    
                    // 获取内存限制，确保有合理的最小值
                    int memoryLimit = apiConfig.LocalModelMemoryLimit ?? 2048;
                    
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === 内存限制调试信息 ===");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 原始内存限制(apiConfig.LocalModelMemoryLimit): {apiConfig.LocalModelMemoryLimit}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 默认值(如果null): 2048");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 当前memoryLimit值: {memoryLimit}");
                    
                    // 如果内存限制太小或为0，使用默认值2048MB
                    if (memoryLimit < 512)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 警告：内存限制设置过小 ({memoryLimit} MB)，使用默认值2048 MB");
                        memoryLimit = 2048;
                    }
                    
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 最终使用的内存限制: {memoryLimit} MB");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] =========================");

                    if (string.IsNullOrWhiteSpace(modelPath))
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 错误：本地模型路径为空");
                        return null;
                    }

                    if (!LocalModelService.ValidateModelFile(modelPath))
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 错误：模型文件无效或不存在: {modelPath}");
                        return null;
                    }

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 开始初始化本地模型...");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 模型路径: {modelPath}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] GPU模式: {gpuMode}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 内存限制: {memoryLimit} MB");
                    
                    DateTime initStartTime = DateTime.Now;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 调用Initialize开始...");
                    bool success = LocalModelServiceSingleton.Instance.Initialize(modelPath, gpuMode, memoryLimit);
                    TimeSpan initDuration = DateTime.Now - initStartTime;
                    
                    if (!success)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 错误：本地模型初始化失败，耗时: {initDuration.TotalSeconds:F2}秒");
                        return null;
                    }
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 本地模型初始化成功，耗时: {initDuration.TotalSeconds:F2}秒");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 本地模型已初始化，直接使用...");
                }

                // 从JSON中提取prompt（使用模型路径获取正确的格式）
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 开始提取prompt...");
                string prompt = ExtractPromptFromJson(jsonString, modelPath);
                
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 错误：无法从请求中提取prompt");
                    return null;
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Prompt长度: {prompt.Length} 字符");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 开始生成响应...");

                // 本地模型使用更长的超时时间（记忆生成后用户会话需要额外时间）
                int localMaxTokens = sessionSettings?.MaxPromptTokens ?? 1000;
                float localTemperature = (float)(sessionSettings?.Temperature ?? 0.7);
                TimeSpan localTimeout = TimeSpan.FromMinutes(20);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 本地模型配置 - MaxTokens: {localMaxTokens}, 温度: {localTemperature}, 超时时间: {localTimeout.TotalMinutes}分钟");

                // 调用本地模型生成响应（返回原始内容），支持自动重新初始化
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 调用CallLocalModelWithRetry开始...");
                string? rawResponse = await CallLocalModelWithRetry(prompt, localMaxTokens, localTemperature, localTimeout, apiConfig);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CallLocalModelWithRetry返回，响应长度: {rawResponse?.Length ?? 0}");
                
                // 保存本地模型响应到Json文件夹
                if (rawResponse != null)
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string jsonFolderPath = Path.Combine(desktopPath, "Json");
                    if (!Directory.Exists(jsonFolderPath))
                    {
                        Directory.CreateDirectory(jsonFolderPath);
                    }
                    string localTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string localResponseFilePath = Path.Combine(jsonFolderPath, $"local_model_response_{localTimestamp}.txt");
                    File.WriteAllText(localResponseFilePath, rawResponse);
                    Console.WriteLine($"响应已保存到: {localResponseFilePath}");
                }

                Console.WriteLine($"响应生成完成，长度: {rawResponse?.Length ?? 0}");
                Console.WriteLine("========== 本地模型调用结束 ==========");
                
                return rawResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"========== 本地模型调用异常 ==========");
                Console.WriteLine($"异常类型: {ex.GetType().Name}");
                Console.WriteLine($"异常消息: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 调用本地模型生成响应，支持自动重新初始化
        /// 
        /// 当模型连接断开或需要重新初始化时，自动尝试重新初始化并重试。
        /// 最多重试2次。
        /// </summary>
        private static async Task<string?> CallLocalModelWithRetry(string prompt, int maxTokens, float temperature, TimeSpan timeout, ApiConfig apiConfig)
        {
            const int maxRetryAttempts = 2;
            
            // 获取模型格式
            var modelFormat = ModelFormatFactory.GetFormat(apiConfig.Endpoint ?? string.Empty);
            Console.WriteLine($"模型格式: {modelFormat.ModelName}");
            Console.WriteLine($"停止词数量: {modelFormat.GetStopWords().Length}");
            
            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"本地模型调用尝试 {attempt}/{maxRetryAttempts}");
                    Console.WriteLine($"Prompt长度: {prompt.Length}");
                    Console.WriteLine($"MaxTokens: {maxTokens}, Temperature: {temperature}, Timeout: {timeout.TotalMinutes}分钟");
                    
                    DateTime requestStartTime = DateTime.Now;
                    string? response = await LocalModelServiceSingleton.Instance.GenerateResponseAsync(prompt, maxTokens, temperature, timeout, modelFormat.GetStopWords());
                    TimeSpan requestDuration = DateTime.Now - requestStartTime;
                    
                    Console.WriteLine($"本地模型响应耗时: {requestDuration.TotalSeconds:F2}秒");
                    Console.WriteLine($"响应长度: {response?.Length ?? 0}");
                    
                    // 使用模型格式清理响应
                    if (!string.IsNullOrEmpty(response))
                    {
                        response = modelFormat.CleanResponse(response);
                        Console.WriteLine($"清理后响应长度: {response.Length}");
                    }
                    
                    return response;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("连接已断开") || ex.Message.Contains("已失效") || ex.Message.Contains("需要重新初始化"))
                {
                    Console.WriteLine($"本地模型连接断开或需要重新初始化，尝试重新初始化...");
                    Console.WriteLine($"错误消息: {ex.Message}");
                    
                    if (attempt < maxRetryAttempts)
                    {
                        // 尝试重新初始化模型
                        string modelPath = apiConfig.Endpoint ?? string.Empty;
                        string gpuMode = apiConfig.ApiKey ?? "自动选择（推荐）";
                        int memoryLimit = apiConfig.LocalModelMemoryLimit ?? 2048;

                        if (!string.IsNullOrWhiteSpace(modelPath) && LocalModelService.ValidateModelFile(modelPath))
                        {
                            Console.WriteLine($"尝试重新初始化模型: {modelPath}");
                            bool success = LocalModelServiceSingleton.Instance.Initialize(modelPath, gpuMode, memoryLimit);
                            if (success)
                            {
                                Console.WriteLine("本地模型重新初始化成功，继续调用...");
                                continue;
                            }
                            else
                            {
                                Console.WriteLine("本地模型重新初始化失败");
                            }
                        }
                        else
                        {
                            Console.WriteLine("模型路径无效或文件不存在");
                        }
                    }
                    
                    // 重新初始化失败或已达到最大重试次数
                    throw new InvalidOperationException("本地模型连接已断开，重新初始化失败", ex);
                }
            }
            
            return null;
        }

        /// <summary>
        /// 从JSON请求中提取prompt（根据模型类型使用正确的格式）
        /// 
        /// 根据模型路径识别模型类型，使用对应的模型格式构建prompt。
        /// </summary>
        /// <param name="jsonString">JSON格式的请求</param>
        /// <param name="modelPath">模型文件路径（用于识别模型类型）</param>
        /// <returns>提取的prompt</returns>
        private static string ExtractPromptFromJson(string jsonString, string modelPath = "")
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("messages", out var messages) &&
                    messages.GetArrayLength() > 0)
                {
                    // 获取模型格式
                    var modelFormat = ModelFormatFactory.GetFormat(modelPath);
                    
                    // 构建消息列表
                    var messageList = new List<(string role, string content)>();
                    
                    foreach (var msg in messages.EnumerateArray())
                    {
                        if (msg.TryGetProperty("role", out var role) &&
                            msg.TryGetProperty("content", out var content))
                        {
                            string roleStr = role.GetString() ?? string.Empty;
                            string contentStr = content.GetString() ?? string.Empty;
                            
                            if (!string.IsNullOrWhiteSpace(roleStr) && !string.IsNullOrWhiteSpace(contentStr))
                            {
                                messageList.Add((roleStr, contentStr));
                            }
                        }
                    }
                    
                    // 获取最后一条消息作为用户消息（如果有的话）
                    string userMessage = string.Empty;
                    if (messageList.Count > 0)
                    {
                        var lastMsg = messageList[messageList.Count - 1];
                        if (lastMsg.role == "user")
                        {
                            userMessage = lastMsg.content;
                            messageList.RemoveAt(messageList.Count - 1);
                        }
                    }
                    
                    // 分离system消息和对话历史
                    string systemContent = string.Empty;
                    var conversationHistory = new List<(string role, string content)>();
                    
                    foreach (var (role, content) in messageList)
                    {
                        if (role == "system")
                        {
                            systemContent = content;
                        }
                        else
                        {
                            conversationHistory.Add((role, content));
                        }
                    }
                    
                    // 使用模型格式构建prompt
                    return modelFormat.BuildChatPrompt(systemContent, conversationHistory, userMessage);
                }
            }
            catch
            {
                // 解析失败，返回原始字符串
            }
            
            return jsonString;
        }

        /// <summary>
        /// 构建无角色消息对象（用于"AI聊天助手"或用户选择无角色模式的会话）
        /// 
        /// 包含记忆、历史对话和最新提问，但不包含AI角色人设信息。
        /// </summary>
        /// <param name="messageText">最新用户消息文本</param>
        /// <param name="session">会话信息</param>
        /// <param name="userProfile">用户配置信息</param>
        /// <param name="historyMessages">历史消息列表</param>
        /// <param name="apiConfig">API配置信息</param>
        /// <param name="temperature">温度参数，控制回答的随机性</param>
        /// <param name="maxTokens">最大令牌数，控制回答的最大长度</param>
        /// <param name="modelName">模型名称</param>
        /// <returns>构建好的消息对象</returns>
        private static object BuildNoRoleMessageObject(string messageText, SessionInfo session, UserProfile? userProfile, List<ChatMessageEntity> historyMessages, ApiConfig? apiConfig, double temperature, int maxTokens, string modelName)
        {
            // 创建用户仓库实例，用于获取AI记忆
            var userRepo = new UserRepository();
            
            // 获取AI的长期记忆（最多5条）
            List<string> aiMemories = userRepo.GetAIMemories(userProfile?.UserAccount ?? string.Empty, session.Id, 5);

            // 构建system消息内容（仅包含记忆）
            StringBuilder sb = new StringBuilder();
            if (aiMemories != null && aiMemories.Count > 0)
            {
                sb.AppendLine("【AI的长期记忆】");
                for (int i = 0; i < aiMemories.Count; i++)
                {
                    sb.AppendLine($"【记忆{i + 1}】");
                    sb.AppendLine(aiMemories[i]);
                    sb.AppendLine();
                }
            }

            // 构建消息列表
            var messagesList = new List<object>();
            
            // 添加system消息（仅包含记忆）
            if (sb.Length > 0)
            {
                messagesList.Add(new { role = "system", content = sb.ToString() });
            }

            // 添加历史对话（user和assistant成对添加）
            foreach (var msg in historyMessages)
            {
                string role = msg.Sender == "user" ? "user" : "assistant";
                messagesList.Add(new { role = role, content = msg.Content ?? string.Empty });
            }

            // 添加用户最新提问
            messagesList.Add(new { role = "user", content = messageText });

            // 构建消息对象（统一使用标准参数）
            return new
            {
                model = modelName,
                messages = messagesList.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens
            };
        }

        /// <summary>
        /// 构建带角色消息对象（用于自定义AI角色的会话）
        /// 
        /// 包含system消息（人设+记忆）、历史对话和最新提问。
        /// </summary>
        /// <param name="messageText">最新用户消息文本</param>
        /// <param name="session">会话信息（包含AI角色信息）</param>
        /// <param name="userProfile">用户配置信息</param>
        /// <param name="historyMessages">历史消息列表</param>
        /// <param name="apiConfig">API配置信息</param>
        /// <param name="temperature">温度参数，控制回答的随机性</param>
        /// <param name="maxTokens">最大令牌数，控制回答的最大长度</param>
        /// <param name="modelName">模型名称</param>
        /// <returns>构建好的消息对象</returns>
        private static object BuildMessageObject(string messageText, SessionInfo session, UserProfile? userProfile, List<ChatMessageEntity> historyMessages, ApiConfig? apiConfig, double temperature, int maxTokens, string modelName)
        {
            // 创建用户仓库实例，用于获取AI记忆
            var userRepo = new UserRepository();
            
            // 获取AI的长期记忆（最多5条）
            List<string> aiMemories = userRepo.GetAIMemories(userProfile?.UserAccount ?? string.Empty, session.Id, 5);

            // 构建system消息内容（包含记忆、人设、用户信息）
            string systemContent = BuildSystemContent(session, userProfile, aiMemories);

            // 构建消息列表
            var messagesList = new List<object>
            {
                // system消息
                new { role = "system", content = systemContent }
            };

            // 添加历史对话（user和assistant成对添加）
            foreach (var msg in historyMessages)
            {
                string role = msg.Sender == "user" ? "user" : "assistant";
                messagesList.Add(new { role = role, content = msg.Content ?? string.Empty });
            }

            // 添加用户最新提问
            messagesList.Add(new { role = "user", content = messageText });

            // 构建消息对象（统一使用标准参数）
            return new
            {
                model = modelName,
                messages = messagesList.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens
            };
        }

        /// <summary>
        /// 构建system消息内容
        /// 
        /// 优先级：用户自定义system_prompt > 长期记忆 > AI角色信息 > 用户背景信息
        /// </summary>
        /// <param name="session">会话信息（AI角色）</param>
        /// <param name="userProfile">用户配置</param>
        /// <param name="aiMemories">AI的长期记忆列表</param>
        /// <returns>构建好的system消息内容</returns>
        private static string BuildSystemContent(SessionInfo session, UserProfile? userProfile, List<string> aiMemories)
        {
            StringBuilder sb = new StringBuilder();

            // 1. 优先使用用户自定义的system_prompt（完全替换默认提示词）
            if (!string.IsNullOrEmpty(userProfile?.SystemPrompt))
            {
                sb.AppendLine(userProfile.SystemPrompt);
                sb.AppendLine();
            }
            else
            {
                // 2. 如果没有用户自定义system_prompt，使用默认的固定人设
                sb.AppendLine("你是一个专业的演员，严格遵守以下人设全程扮演，绝不跳出角色：");
                sb.AppendLine();
            }
            
            // 3. 如果有AI的长期记忆，添加
            if (aiMemories != null && aiMemories.Count > 0)
            {
                sb.AppendLine("【AI的长期记忆】");
                for (int i = 0; i < aiMemories.Count; i++)
                {
                    sb.AppendLine($"【记忆{i + 1}】");
                    sb.AppendLine(aiMemories[i]);
                    sb.AppendLine();
                }
            }

            // 4. 添加AI角色基本信息
            sb.AppendLine("【AI角色信息】");
            sb.AppendLine($"姓名：{session.AiName}");
            sb.AppendLine($"性别：{session.AiGender}");
            sb.AppendLine($"身份：{session.AiPersona}。");
            sb.AppendLine($"说话风格：{session.AiStyle}");
            sb.AppendLine($"固定口头禅：{session.AiHabit}");
            sb.AppendLine();

            // 5. 添加用户信息
            sb.AppendLine("【用户信息】");
            sb.AppendLine($"用户昵称：{userProfile?.Nickname ?? "未设置"}");
            sb.AppendLine($"性别：{userProfile?.Gender ?? "未设置"}");
            sb.AppendLine($"身份：{userProfile?.Persona ?? "未设置"}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成AI的长期记忆（云端API版本）
        /// 
        /// 根据用户配置的memoryTrigger触发，使用memoryPrompt生成记忆。
        /// 后台独立调用，不占用正常对话上下文。
        /// </summary>
        /// <param name="session">会话信息</param>
        /// <param name="loginUsername">登录用户名</param>
        /// <returns>是否成功生成并保存记忆</returns>
        public static async Task<bool> GenerateMemoryAsync(SessionInfo session, string loginUsername)
        {
            try
            {
                var userRepo = new UserRepository();
                var apiConfig = userRepo.LoadApiConfig(loginUsername);
                var userProfile = userRepo.GetUserProfile(loginUsername);
                var sessionSettings = userRepo.LoadSessionSettings(loginUsername);

                // 检查是否禁用角色信息
                bool noRole = sessionSettings?.NoRole ?? false;
                bool isAiAssistant = session.AiName == "AI聊天助手";

                // 检查是否有记忆生成提示词
                string? memoryPrompt = sessionSettings?.MemoryPrompt;
                if (string.IsNullOrEmpty(memoryPrompt))
                {
                    memoryPrompt = string.Empty;
                }

                // 获取记忆生成条数的聊天记录
                int memoryTriggerCount = sessionSettings?.MemoryTrigger ?? 20;
                var chatMessages = userRepo.GetRecentChatMessages(session.Id, loginUsername, memoryTriggerCount);

                if (chatMessages.Count == 0)
                {
                    return false;
                }

                // 构建历史对话内容
                StringBuilder historySb = new StringBuilder();
                foreach (var msg in chatMessages)
                {
                    string sender = msg.Sender == "user" ? "用户" : session.AiName;
                    historySb.AppendLine($"{sender}：{msg.Content}");
                }

                // 构建消息对象（根据是否启用纯净模式选择不同的构建方式）
                var messageObj = noRole || isAiAssistant ? BuildMemoryExtractionMessageObject(historySb.ToString(), apiConfig, loginUsername) :
                    BuildMemoryExtractionMessageObjectWithRole(historySb.ToString(), session, userProfile, apiConfig, loginUsername);

                // 序列化为Json字符串
                string jsonString = System.Text.Json.JsonSerializer.Serialize(messageObj, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // 构建请求地址
                string baseUrl = apiConfig?.Endpoint ?? "https://api.siliconflow.cn/v1";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                string fullUrl = baseUrl + "chat/completions";

                // 保存到Memory文件夹
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string memoryFolderPath = Path.Combine(desktopPath, "Memory");
                if (!Directory.Exists(memoryFolderPath))
                {
                    Directory.CreateDirectory(memoryFolderPath);
                }
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                StringBuilder memoryRequestSb = new StringBuilder();
                memoryRequestSb.AppendLine($"POST {fullUrl}");
                memoryRequestSb.AppendLine();
                memoryRequestSb.AppendLine("Headers:");
                memoryRequestSb.AppendLine("  Content-Type: application/json");
                memoryRequestSb.AppendLine($"  Authorization: Bearer {apiConfig?.ApiKey ?? "未配置"}");
                memoryRequestSb.AppendLine();
                memoryRequestSb.AppendLine(jsonString);

                string memoryRequestFilePath = Path.Combine(memoryFolderPath, $"memory_request_{timestamp}.txt");
                File.WriteAllText(memoryRequestFilePath, memoryRequestSb.ToString());

                // 创建HTTP客户端
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiConfig?.ApiKey ?? "");

                // 创建请求内容
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // 发送请求
                var response = await client.PostAsync(fullUrl, content);

                // 读取响应内容
                string responseContent = await response.Content.ReadAsStringAsync();

                // 保存响应到Memory文件夹
                string memoryResponseFilePath = Path.Combine(memoryFolderPath, $"memory_response_{timestamp}.txt");
                File.WriteAllText(memoryResponseFilePath, responseContent);

                // 解析响应，提取content内容
                string? memoryContent = null;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentProp))
                    {
                        // 直接读取content字段获取记忆内容
                        memoryContent = contentProp.GetString();
                    }
                }
                catch
                {
                    // 解析失败
                }

                // 如果成功获取记忆内容，保存到数据库
                if (!string.IsNullOrEmpty(memoryContent))
                {
                    bool success = userRepo.SaveAIMemory(loginUsername, session.Id, session.AiName ?? "未知", memoryContent, out string errorMessage);
                    if (success)
                    {
                        Console.WriteLine($"AI记忆生成成功：{memoryContent.Substring(0, Math.Min(50, memoryContent.Length))}...");
                    }
                    else
                    {
                        Console.WriteLine($"AI记忆保存失败：{errorMessage}");
                    }
                    return success;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成AI记忆异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 构建记忆提取消息对象（纯净模式）
        /// 
        /// 用于在后台静默提取用户对话中的长期记忆，不包含角色信息。
        /// </summary>
        /// <param name="historyContent">历史对话内容</param>
        /// <param name="apiConfig">API配置信息</param>
        /// <param name="loginUsername">登录用户名</param>
        /// <returns>构建好的记忆提取消息对象</returns>
        private static object BuildMemoryExtractionMessageObject(string historyContent, ApiConfig? apiConfig, string loginUsername)
        {
            // 从数据库获取会话设置，使用用户自定义的记忆提示词
            var userRepo = new UserRepository();
            var sessionSettings = userRepo.LoadSessionSettings(loginUsername);
            
            // 获取记忆生成提示词
            string memoryPrompt = sessionSettings?.MemoryPrompt ?? "请根据用户对话历史与交互信息，严格提取用户长期偏好、习惯、需求与关键信息。\n以第一人称\"我\"进行总结，内容客观精炼，不虚构、不冗余、无情绪化描述，仅保留长期有效信息。\n按照条目精简输出，用于后续对话个性化参考，严格控制篇幅长度。";
            
            // 获取温度值和最大令牌数，使用用户设置的值，如果没有设置则使用默认值
            double temperature = sessionSettings?.Temperature ?? 0.3;
            int maxTokens = sessionSettings?.MaxPromptTokens ?? 1000;
            
            // 构建消息列表
            var messagesList = new List<object>
            {
                // system消息：定义记忆提取的严格规则
                new { role = "system", content = $"【最高优先级指令】\n你仅负责**后台静默提取并生成长期记忆**，全程关闭聊天对话、寒暄回复、客套话术、反问、结束语、表情符号，完全不回应用户日常对话语句，不与用户交互聊天。\n\n【提取规则】\n{memoryPrompt}\n\n全程隐藏记忆功能，绝不向用户提及记忆、提取、存储相关任何词汇。" },
                // user消息（历史对话）
                new { role = "user", content = historyContent }
            };

            // 构建消息对象（使用用户设置的参数）
            return new
            {
                model = apiConfig?.Model ?? "deepseek-ai/DeepSeek-V3",
                messages = messagesList.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens
            };
        }

        /// <summary>
        /// 构建记忆提取消息对象（非纯净模式）
        /// 
        /// 用于在后台静默提取用户对话中的长期记忆，包含角色信息。
        /// </summary>
        /// <param name="historyContent">历史对话内容</param>
        /// <param name="session">会话信息（包含AI角色信息）</param>
        /// <param name="userProfile">用户配置信息</param>
        /// <param name="apiConfig">API配置信息</param>
        /// <param name="loginUsername">登录用户名</param>
        /// <returns>构建好的记忆提取消息对象</returns>
        private static object BuildMemoryExtractionMessageObjectWithRole(string historyContent, SessionInfo session, UserProfile? userProfile, ApiConfig? apiConfig, string loginUsername)
        {
            // 从数据库获取会话设置，使用用户自定义的记忆提示词
            var userRepo = new UserRepository();
            var sessionSettings = userRepo.LoadSessionSettings(loginUsername);

            // 获取记忆生成提示词
            string memoryPrompt = sessionSettings?.MemoryPrompt ?? string.Empty;
            
            // 获取温度值和最大令牌数，使用用户设置的值，如果没有设置则使用默认值
            double temperature = sessionSettings?.Temperature ?? 0.3;
            int maxTokens = sessionSettings?.MaxPromptTokens ?? 1000;
            
            // 构建system消息内容（包含AI角色信息和用户信息）
            StringBuilder systemSb = new StringBuilder();
            systemSb.AppendLine("【最高优先级指令】");
            systemSb.AppendLine("你仅负责**后台静默提取并生成长期记忆**，全程关闭聊天对话、寒暄回复、客套话术、反问、结束语、表情符号，完全不回应用户日常对话语句，不与用户交互聊天。");
            systemSb.AppendLine();
            systemSb.AppendLine("【提取规则】");
            systemSb.AppendLine(memoryPrompt);
            systemSb.AppendLine();
            
            // 添加AI角色基本信息
            systemSb.AppendLine("【AI角色信息】");
            systemSb.AppendLine($"姓名：{session.AiName}");
            systemSb.AppendLine($"性别：{session.AiGender}");
            systemSb.AppendLine($"身份：{session.AiPersona}。");
            systemSb.AppendLine($"说话风格：{session.AiStyle}");
            systemSb.AppendLine($"固定口头禅：{session.AiHabit}");
            systemSb.AppendLine();
            
            // 添加用户信息
            systemSb.AppendLine("【用户信息】");
            systemSb.AppendLine($"用户昵称：{userProfile?.Nickname ?? "未设置"}");
            systemSb.AppendLine($"性别：{userProfile?.Gender ?? "未设置"}");
            systemSb.AppendLine($"身份：{userProfile?.Persona ?? "未设置"}");

            // 构建消息列表
            var messagesList = new List<object>
            {
                // system消息：包含角色信息和记忆提取规则
                new { role = "system", content = systemSb.ToString() },
                // user消息（历史对话）
                new { role = "user", content = historyContent }
            };

            // 构建消息对象（使用用户设置的参数）
            return new
            {
                model = apiConfig?.Model ?? "deepseek-ai/DeepSeek-V3",
                messages = messagesList.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens
            };
        }

        /// <summary>
        /// 生成AI的长期记忆（Qwen-7B格式，本地模型版本）
        /// 
        /// 根据用户配置的memoryTrigger触发，memoryPrompt生成记忆。
        /// 仅支持本地模型。
        /// </summary>
        /// <param name="session">会话信息</param>
        /// <param name="loginUsername">登录用户名</param>
        /// <returns>是否成功生成并保存记忆</returns>
        public static async Task<bool> GenerateMemoryWithQwenFormatAsync(SessionInfo session, string loginUsername)
        {
            try
            {
                var userRepo = new UserRepository();
                var apiConfig = userRepo.LoadApiConfig(loginUsername);
                var userProfile = userRepo.GetUserProfile(loginUsername);
                var sessionSettings = userRepo.LoadSessionSettings(loginUsername);

                // 检查是否禁用角色信息
                bool noRole = sessionSettings?.NoRole ?? false;
                bool isAiAssistant = session.AiName == "AI聊天助手";

                // 检查是否有记忆生成提示词
                string? memoryPrompt = sessionSettings?.MemoryPrompt;
                if (string.IsNullOrEmpty(memoryPrompt))
                {
                    memoryPrompt = "请根据用户对话历史与交互信息，严格提取用户长期偏好、习惯、需求与关键信息。\n以第一人称\"我\"进行总结，内容客观精炼，不虚构、不冗余、无情绪化描述，仅保留长期有效信息。\n按照条目精简输出，用于后续对话个性化参考，严格控制篇幅长度。";
                }

                // 获取记忆生成条数的聊天记录
                int memoryTriggerCount = sessionSettings?.MemoryTrigger ?? 20;
                var chatMessages = userRepo.GetRecentChatMessages(session.Id, loginUsername, memoryTriggerCount);

                if (chatMessages.Count == 0)
                {
                    return false;
                }

                // 构建历史对话内容
                StringBuilder historySb = new StringBuilder();
                foreach (var msg in chatMessages)
                {
                    string sender = msg.Sender == "user" ? "用户" : session.AiName;
                    historySb.AppendLine($"{sender}：{msg.Content}");
                }

                // 构建Qwen-7B格式的Prompt
                string qwenPrompt = BuildQwenMemoryPrompt(historySb.ToString(), memoryPrompt, session, userProfile, noRole || isAiAssistant);

                // 获取温度值和最大令牌数
                float temperature = (float)(sessionSettings?.Temperature ?? 0.3);
                int maxTokens = sessionSettings?.MaxPromptTokens ?? 1000;

                // 保存到Memory文件夹
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string memoryFolderPath = Path.Combine(desktopPath, "Memory");
                if (!Directory.Exists(memoryFolderPath))
                {
                    Directory.CreateDirectory(memoryFolderPath);
                }
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string memoryRequestFilePath = Path.Combine(memoryFolderPath, $"memory_request_qwen_{timestamp}.txt");
                File.WriteAllText(memoryRequestFilePath, qwenPrompt);

                // 调用本地模型生成响应（Qwen-7B格式仅支持本地模型）
                string? memoryContent = null;
                
                Console.WriteLine($"Qwen记忆生成：检查是否启用本地模型 - EnableLocalModel: {apiConfig?.EnableLocalModel ?? false}");
                
                if (apiConfig?.EnableLocalModel ?? false)
                {
                    Console.WriteLine("Qwen记忆生成：进入本地模型分支");
                    
                    // 使用本地模型
                    if (!LocalModelServiceSingleton.Instance.IsInitialized)
                    {
                        Console.WriteLine("Qwen记忆生成：模型未初始化，开始初始化...");
                        string modelPath = apiConfig.Endpoint ?? string.Empty;
                        string gpuMode = apiConfig.ApiKey ?? "自动选择（推荐）";
                        int memoryLimit = apiConfig.LocalModelMemoryLimit ?? 2048;

                        Console.WriteLine($"Qwen记忆生成：模型路径: {modelPath}");
                        Console.WriteLine($"Qwen记忆生成：GPU模式: {gpuMode}");
                        Console.WriteLine($"Qwen记忆生成：内存限制: {memoryLimit} MB");

                        if (!string.IsNullOrWhiteSpace(modelPath) && LocalModelService.ValidateModelFile(modelPath))
                        {
                            Console.WriteLine("Qwen记忆生成：模型文件有效，开始初始化...");
                            bool success = LocalModelServiceSingleton.Instance.Initialize(modelPath, gpuMode, memoryLimit);
                            if (!success)
                            {
                                Console.WriteLine("Qwen记忆生成：模型初始化失败");
                                return false;
                            }
                            Console.WriteLine("Qwen记忆生成：模型初始化成功");
                        }
                        else
                        {
                            Console.WriteLine("Qwen记忆生成：模型文件无效");
                            return false;
                        }
                    }

                    // 获取模型格式
                    var modelFormat = ModelFormatFactory.GetFormat(apiConfig.Endpoint ?? string.Empty);
                    
                    // 调用本地模型
                    memoryContent = await LocalModelServiceSingleton.Instance.GenerateResponseAsync(qwenPrompt, maxTokens, temperature, TimeSpan.FromMinutes(15), modelFormat.GetStopWords());
                    
                    // 清理响应
                    if (!string.IsNullOrEmpty(memoryContent))
                    {
                        memoryContent = modelFormat.CleanResponse(memoryContent);
                    }
                }

                // 如果成功获取记忆内容，保存到数据库
                if (!string.IsNullOrEmpty(memoryContent))
                {
                    bool success = userRepo.SaveAIMemory(loginUsername, session.Id, session.AiName ?? "未知", memoryContent, out string errorMessage);
                    if (success)
                    {
                        Console.WriteLine($"Qwen格式AI记忆生成成功：{memoryContent.Substring(0, Math.Min(50, memoryContent.Length))}...");
                    }
                    else
                    {
                        Console.WriteLine($"Qwen格式AI记忆保存失败：{errorMessage}");
                    }
                    return success;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Qwen格式AI记忆生成异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 构建Qwen格式的记忆生成Prompt
        /// </summary>
        private static string BuildQwenMemoryPrompt(string historyContent, string memoryPrompt, SessionInfo session, UserProfile? userProfile, bool noRole)
        {
            var modelFormat = new QwenModelFormat();
            
            string systemContent;
            if (noRole)
            {
                systemContent = $"【最高优先级指令】\n你仅负责**后台静默提取并生成长期记忆**，全程关闭聊天对话、寒暄回复、客套话术、反问、结束语、表情符号，完全不回应用户日常对话语句，不与用户交互聊天。\n\n【提取规则】\n{memoryPrompt}\n\n全程隐藏记忆功能，绝不向用户提及记忆、提取、存储相关任何词汇。";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("【最高优先级指令】");
                sb.AppendLine("你仅负责**后台静默提取并生成长期记忆**，全程关闭聊天对话、寒暄回复、客套话术、反问、结束语、表情符号，完全不回应用户日常对话语句，不与用户交互聊天。");
                sb.AppendLine();
                sb.AppendLine("【提取规则】");
                sb.AppendLine(memoryPrompt);
                sb.AppendLine();
                sb.AppendLine("【AI角色信息】");
                sb.AppendLine($"姓名：{session.AiName}");
                sb.AppendLine($"性别：{session.AiGender}");
                sb.AppendLine($"身份：{session.AiPersona}。");
                sb.AppendLine($"说话风格：{session.AiStyle}");
                sb.AppendLine($"固定口头禅：{session.AiHabit}");
                sb.AppendLine();
                sb.AppendLine("【用户信息】");
                sb.AppendLine($"用户昵称：{userProfile?.Nickname ?? "未设置"}");
                sb.AppendLine($"性别：{userProfile?.Gender ?? "未设置"}");
                sb.AppendLine($"身份：{userProfile?.Persona ?? "未设置"}");
                systemContent = sb.ToString();
            }
            
            return modelFormat.BuildMemoryPrompt(systemContent, historyContent);
        }
    }
}
