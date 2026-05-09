using System;
using System.Data.SQLite;
using System.Collections.Generic;
using ChatAI.UI.Forms;

namespace ChatAI.Data
{
    /// <summary>
    /// 用户数据仓库（UserRepository）
    /// 
    /// 该类负责所有数据库操作，包括：
    /// 1. AI角色管理（增删改查）
    /// 2. 聊天消息管理（增删查）
    /// 3. API配置管理（保存/加载）
    /// 4. 用户配置管理（保存/加载）
    /// 5. 会话设置管理（保存/加载）
    /// 6. AI长期记忆管理（增删改查）
    /// 
    /// 所有敏感数据（如人设、消息内容、API密钥等）均通过EncryptionHelper进行加密存储。
    /// </summary>
    public class UserRepository
    {
        /// <summary>
        /// 添加AI角色到数据库
        /// </summary>
        /// <param name="ai">AI角色实体</param>
        /// <param name="errorMessage">错误信息输出</param>
        /// <returns>是否添加成功</returns>
        public bool AddAiCharacter(AiCharacter ai, out string errorMessage)
        {
            try
            {
                string sql = @"
            INSERT INTO ai_character (
                user_account,
                ai_name,
                ai_gender,
                ai_persona,
                ai_style,
                ai_habit,
                ai_opening,
                create_time
            )
            VALUES (
                @UserAccount,
                @AiName,
                @AiGender,
                @AiPersona,
                @AiStyle,
                @AiHabit,
                @AiOpening,
                @CreateTime
            );
            SELECT last_insert_rowid();";

                errorMessage = string.Empty;
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", ai.UserAccount ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiName", ai.AiName ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiGender", ai.AiGender ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiPersona", EncryptionHelper.Encrypt(ai.AiPersona ?? string.Empty));
                cmd.Parameters.AddWithValue("@AiStyle", EncryptionHelper.Encrypt(ai.AiStyle ?? string.Empty));
                cmd.Parameters.AddWithValue("@AiHabit", EncryptionHelper.Encrypt(ai.AiHabit ?? string.Empty));
                cmd.Parameters.AddWithValue("@AiOpening", EncryptionHelper.Encrypt(ai.AiOpening ?? string.Empty));
                cmd.Parameters.AddWithValue("@CreateTime", ai.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    ai.Id = Convert.ToInt32(result);
                }
                return ai.Id > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 更新AI角色信息
        /// </summary>
        public bool UpdateAiCharacter(AiCharacter ai, out string errorMessage)
        {
            try
            {
                string sql = @"
            UPDATE ai_character SET
                ai_name = @AiName,
                ai_gender = @AiGender,
                ai_persona = @AiPersona,
                ai_style = @AiStyle,
                ai_habit = @AiHabit,
                ai_opening = @AiOpening
            WHERE id = @Id AND user_account = @UserAccount";

                errorMessage = string.Empty;
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", ai.Id);
                cmd.Parameters.AddWithValue("@UserAccount", ai.UserAccount ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiName", ai.AiName ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiGender", ai.AiGender ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiPersona", EncryptionHelper.Encrypt(ai.AiPersona ?? string.Empty));
                cmd.Parameters.AddWithValue("@AiStyle", EncryptionHelper.Encrypt(ai.AiStyle ?? string.Empty));
                cmd.Parameters.AddWithValue("@AiHabit", EncryptionHelper.Encrypt(ai.AiHabit ?? string.Empty));
                cmd.Parameters.AddWithValue("@AiOpening", EncryptionHelper.Encrypt(ai.AiOpening ?? string.Empty));
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 根据用户账号加载所有AI角色（用于会话列表展示）
        /// 读取AI人物所有信息按创建时间倒序
        /// </summary>
        public List<ChatControl.Models.SessionInfo> LoadAiCharacter(string userAccount)
        {
            List<ChatControl.Models.SessionInfo> sessionList = new List<ChatControl.Models.SessionInfo>();

            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                // 查询语句：把所有需要的字段都查出来，按创建时间倒序
                const string sql = @"
    SELECT id, user_account, ai_name, ai_gender, ai_persona, ai_style, ai_habit, ai_opening, create_time
    FROM ai_character
    WHERE user_account = @UserAccount
    ORDER BY create_time DESC";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", userAccount);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string? aiPersonaEncrypted = reader["ai_persona"]?.ToString();
                    string? aiStyleEncrypted = reader["ai_style"]?.ToString();
                    string? aiHabitEncrypted = reader["ai_habit"]?.ToString();
                    string? aiOpeningEncrypted = reader["ai_opening"]?.ToString();
                    string aiPersona = string.Empty;
                    string aiStyle = string.Empty;
                    string aiHabit = string.Empty;
                    string aiOpening = string.Empty;
                    try { aiPersona = EncryptionHelper.Decrypt(aiPersonaEncrypted ?? string.Empty); } catch { aiPersona = aiPersonaEncrypted ?? string.Empty; }
                    try { aiStyle = EncryptionHelper.Decrypt(aiStyleEncrypted ?? string.Empty); } catch { aiStyle = aiStyleEncrypted ?? string.Empty; }
                    try { aiHabit = EncryptionHelper.Decrypt(aiHabitEncrypted ?? string.Empty); } catch { aiHabit = aiHabitEncrypted ?? string.Empty; }
                    try { aiOpening = EncryptionHelper.Decrypt(aiOpeningEncrypted ?? string.Empty); } catch { aiOpening = aiOpeningEncrypted ?? string.Empty; }

                    // 创建新版 SessionInfo（无冗余、无重复）
                    var session = new ChatControl.Models.SessionInfo
                    {
                        // 一一对应数据库字段赋值
                        Id = Convert.ToInt32(reader["id"]),
                        UserAccount = reader["user_account"]?.ToString(),
                        AiName = reader["ai_name"]?.ToString() ?? "未命名",
                        AiGender = reader["ai_gender"]?.ToString() ?? "男",
                        AiPersona = aiPersona,
                        AiStyle = aiStyle,
                        AiHabit = aiHabit,
                        AiOpening = aiOpening,
                        CreateTime = Convert.ToDateTime(reader["create_time"])
                    };

                    sessionList.Add(session);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载AI角色失败：{ex.Message}");
            }

            return sessionList;
        }

        /// <summary>
        /// 根据ID获取单个AI角色
        /// </summary>
        public AiCharacter? GetAiCharacterById(int id)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                const string sql = @"
    SELECT id, user_account, ai_name, ai_gender, ai_persona, ai_style, ai_habit, ai_opening, create_time
    FROM ai_character
    WHERE id = @Id";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string? aiPersonaEncrypted = reader["ai_persona"]?.ToString();
                    string? aiStyleEncrypted = reader["ai_style"]?.ToString();
                    string? aiHabitEncrypted = reader["ai_habit"]?.ToString();
                    string? aiOpeningEncrypted = reader["ai_opening"]?.ToString();
                    string aiPersona = string.Empty;
                    string aiStyle = string.Empty;
                    string aiHabit = string.Empty;
                    string aiOpening = string.Empty;
                    try { aiPersona = EncryptionHelper.Decrypt(aiPersonaEncrypted ?? string.Empty); } catch { aiPersona = aiPersonaEncrypted ?? string.Empty; }
                    try { aiStyle = EncryptionHelper.Decrypt(aiStyleEncrypted ?? string.Empty); } catch { aiStyle = aiStyleEncrypted ?? string.Empty; }
                    try { aiHabit = EncryptionHelper.Decrypt(aiHabitEncrypted ?? string.Empty); } catch { aiHabit = aiHabitEncrypted ?? string.Empty; }
                    try { aiOpening = EncryptionHelper.Decrypt(aiOpeningEncrypted ?? string.Empty); } catch { aiOpening = aiOpeningEncrypted ?? string.Empty; }

                    return new AiCharacter
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        UserAccount = reader["user_account"]?.ToString(),
                        AiName = reader["ai_name"]?.ToString(),
                        AiGender = reader["ai_gender"]?.ToString(),
                        AiPersona = aiPersona,
                        AiStyle = aiStyle,
                        AiHabit = aiHabit,
                        AiOpening = aiOpening,
                        CreateTime = Convert.ToDateTime(reader["create_time"])
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取AI角色失败：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 存储聊天消息到数据库
        /// </summary>
        /// <param name="message">聊天消息实体</param>
        /// <param name="errorMessage">错误信息输出</param>
        /// <param name="messageId">新插入消息的ID</param>
        /// <param name="newTotalCount">新消息的total_message_count值（用于记忆生成触发判断）</param>
        /// <returns>是否存储成功</returns>
        public bool AddChatMessage(ChatMessage message, out string errorMessage, out int messageId, out int newTotalCount)
        {
            try
            {
                int currentCount = GetChatMessageCount(message.AiCharacterId, message.UserAccount ?? string.Empty);
                newTotalCount = currentCount + 1;

                string sql = @"
            INSERT INTO chat_message (
                user_account,
                ai_name,
                ai_character_id,
                sender,
                content,
                create_time,
                total_message_count
            )
            VALUES (
                @UserAccount,
                @AiName,
                @AiCharacterId,
                @Sender,
                @Content,
                @CreateTime,
                @TotalMessageCount
            )
            RETURNING id";

                errorMessage = string.Empty;
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", message.UserAccount ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiName", message.AiName ?? string.Empty);
                cmd.Parameters.AddWithValue("@AiCharacterId", message.AiCharacterId);
                cmd.Parameters.AddWithValue("@Sender", message.Sender ?? string.Empty);
                cmd.Parameters.AddWithValue("@Content", EncryptionHelper.Encrypt(message.Content ?? string.Empty));
                cmd.Parameters.AddWithValue("@CreateTime", message.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@TotalMessageCount", newTotalCount);

                object? result = cmd.ExecuteScalar();
                messageId = result != null ? Convert.ToInt32(result) : 0;
                return messageId > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                messageId = 0;
                newTotalCount = 0;
                return false;
            }
        }

        /// <summary>
        /// 删除指定AI角色的最后一条AI消息
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否成功</returns>
        public bool DeleteLastAiMessage(string userAccount, int aiCharacterId, out string errorMessage)
        {
            try
            {
                string sql = @"
            DELETE FROM chat_message 
            WHERE id = (
                SELECT MAX(id) 
                FROM chat_message 
                WHERE user_account = @UserAccount 
                AND ai_character_id = @AiCharacterId 
                AND sender = 'ai'
            )";

                var parameters = new
                {
                    UserAccount = userAccount,
                    AiCharacterId = aiCharacterId
                };

                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 获取用户的最后一条消息
        /// </summary>
        /// <param name="userAccount">用户账户</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <returns>用户的最后一条消息内容，如果未找到返回空字符串</returns>
        public string GetLastUserMessage(string userAccount, int aiCharacterId)
        {
            try
            {
                string sql = @"
            SELECT content 
            FROM chat_message 
            WHERE user_account = @UserAccount 
            AND ai_character_id = @AiCharacterId 
            AND sender = 'user'
            ORDER BY id DESC 
            LIMIT 1";

                var parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "UserAccount", userAccount },
                    { "AiCharacterId", aiCharacterId }
                };

                using (var reader = SqliteDbHelper.ExecuteReader(sql, parameters))
                {
                    if (reader.Read())
                    {
                        return reader["content"]?.ToString() ?? string.Empty;
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最后一条用户消息失败：{ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 删除指定消息ID之后的所有消息（包含指定消息）
        /// 用于回溯功能
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="messageId">指定的消息ID</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否成功</returns>
        public bool DeleteMessagesAfter(int messageId, int aiCharacterId, string userAccount, out string errorMessage)
        {
            try
            {
                // 首先获取要删除的消息的创建时间
                string getTimeSql = @"SELECT create_time FROM chat_message 
                                      WHERE id = @MessageId AND user_account = @UserAccount AND ai_character_id = @AiCharacterId";
                var getTimeParams = new Dictionary<string, object>
                {
                    { "@MessageId", messageId },
                    { "@UserAccount", userAccount },
                    { "@AiCharacterId", aiCharacterId }
                };

                string? createTime = null;
                using (var reader = SqliteDbHelper.ExecuteReader(getTimeSql, getTimeParams))
                {
                    if (reader.Read())
                    {
                        createTime = reader.GetString(0);
                    }
                }

                if (string.IsNullOrEmpty(createTime))
                {
                    errorMessage = "未找到指定的消息";
                    return false;
                }

                // 使用创建时间来删除后续消息（与查询逻辑保持一致）
                string sql = @"
            DELETE FROM chat_message
            WHERE user_account = @UserAccount
            AND ai_character_id = @AiCharacterId
            AND create_time > @CreateTime";

                var parameters = new
                {
                    UserAccount = userAccount,
                    AiCharacterId = aiCharacterId,
                    CreateTime = createTime
                };

                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 删除指定消息之后的所有消息（使用消息文本和发送时间定位）
        /// 用于回溯功能，与UI删除逻辑保持一致
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="messageText">消息文本内容</param>
        /// <param name="sendTime">消息发送时间</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否成功</returns>
        public bool DeleteMessagesAfter(string messageText, DateTime sendTime, int aiCharacterId, string userAccount, out string errorMessage)
        {
            try
            {
                // 使用消息文本和发送时间来定位消息（与UI逻辑一致）
                string getTimeSql = @"SELECT create_time FROM chat_message 
                                      WHERE content = @MessageText 
                                      AND create_time = @SendTime
                                      AND user_account = @UserAccount 
                                      AND ai_character_id = @AiCharacterId";
                var getTimeParams = new Dictionary<string, object>
                {
                    { "@MessageText", EncryptionHelper.Encrypt(messageText) },
                    { "@SendTime", sendTime.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@UserAccount", userAccount },
                    { "@AiCharacterId", aiCharacterId }
                };

                string? createTime = null;
                using (var reader = SqliteDbHelper.ExecuteReader(getTimeSql, getTimeParams))
                {
                    if (reader.Read())
                    {
                        createTime = reader.GetString(0);
                    }
                }

                if (string.IsNullOrEmpty(createTime))
                {
                    // 如果精确匹配失败，尝试使用消息文本查找最新的匹配消息
                    string getTimeRangeSql = @"SELECT create_time FROM chat_message 
                                                WHERE content = @MessageText 
                                                AND user_account = @UserAccount 
                                                AND ai_character_id = @AiCharacterId
                                                ORDER BY create_time DESC LIMIT 1";
                    var rangeParams = new Dictionary<string, object>
                    {
                        { "@MessageText", EncryptionHelper.Encrypt(messageText) },
                        { "@UserAccount", userAccount },
                        { "@AiCharacterId", aiCharacterId }
                    };

                    using (var reader = SqliteDbHelper.ExecuteReader(getTimeRangeSql, rangeParams))
                    {
                        if (reader.Read())
                        {
                            createTime = reader.GetString(0);
                        }
                    }
                }

                if (string.IsNullOrEmpty(createTime))
                {
                    errorMessage = "未找到指定的消息";
                    return false;
                }

                // 使用创建时间来删除后续消息（与查询逻辑保持一致）
                string sql = @"
            DELETE FROM chat_message
            WHERE user_account = @UserAccount
            AND ai_character_id = @AiCharacterId
            AND create_time > @CreateTime";

                var parameters = new
                {
                    UserAccount = userAccount,
                    AiCharacterId = aiCharacterId,
                    CreateTime = createTime
                };

                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 删除指定消息ID之后生成的记忆
        /// 用于回溯功能，删除回溯点之后生成的记忆
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="messageTotalCount">回溯点的total_message_count值</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否成功</returns>
        public bool DeleteMemoriesAfterMessage(int aiCharacterId, string userAccount, int messageTotalCount, out string errorMessage)
        {
            try
            {
                string sql = @"
            DELETE FROM ai_memory
            WHERE user_account = @UserAccount
            AND ai_character_id = @AiCharacterId
            AND id IN (
                SELECT id FROM ai_memory
                WHERE user_account = @UserAccount
                AND ai_character_id = @AiCharacterId
                ORDER BY create_time DESC
            )
            AND create_time > (
                SELECT MAX(create_time) FROM chat_message
                WHERE user_account = @UserAccount
                AND ai_character_id = @AiCharacterId
                AND total_message_count <= @MessageTotalCount
            )";

                var parameters = new
                {
                    UserAccount = userAccount,
                    AiCharacterId = aiCharacterId,
                    MessageTotalCount = messageTotalCount
                };

                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 根据AI角色ID加载聊天历史记录
        /// </summary>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="userAccount">用户账号</param>
        /// <returns>聊天消息列表</returns>
        public List<ChatMessage> LoadChatMessages(int aiCharacterId, string userAccount)
        {
            List<ChatMessage> messages = new List<ChatMessage>();

            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                const string sql = @"
    SELECT id, user_account, ai_name, ai_character_id, sender, content, create_time, total_message_count
    FROM chat_message
    WHERE ai_character_id = @AiCharacterId AND user_account = @UserAccount
    ORDER BY create_time ASC";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AiCharacterId", aiCharacterId);
                cmd.Parameters.AddWithValue("@UserAccount", userAccount);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string? contentEncrypted = reader["content"]?.ToString();
                    string content = string.Empty;
                    try { content = EncryptionHelper.Decrypt(contentEncrypted ?? string.Empty); } catch { content = contentEncrypted ?? string.Empty; }

                    var message = new ChatMessage
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        UserAccount = reader["user_account"]?.ToString(),
                        AiName = reader["ai_name"]?.ToString(),
                        AiCharacterId = Convert.ToInt32(reader["ai_character_id"]),
                        Sender = reader["sender"]?.ToString(),
                        Content = content,
                        CreateTime = Convert.ToDateTime(reader["create_time"]),
                        TotalMessageCount = reader["total_message_count"] != DBNull.Value ? Convert.ToInt32(reader["total_message_count"]) : 0
                    };

                    messages.Add(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载聊天记录失败：{ex.Message}");
            }

            return messages;
        }

        /// <summary>
        /// 保存API配置到数据库
        /// </summary>
        /// <param name="apiConfig">API配置实体</param>
        /// <param name="errorMessage">错误信息输出</param>
        /// <returns>是否保存成功</returns>
        public bool SaveApiConfig(ChatAI.Data.Entities.ApiConfig apiConfig, out string errorMessage)
        {
            try
            {
                // 先检查是否已存在该用户的API配置
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                // 检查是否已存在
                const string checkSql = @"
    SELECT COUNT(*) FROM api_config 
    WHERE user_account = @UserAccount";

                using var checkCmd = new SQLiteCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@UserAccount", apiConfig.UserAccount);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                string sql;
                if (count > 0)
                {
                    // 更新现有配置
                    sql = @"
    UPDATE api_config SET
        provider = @Provider,
        model = @Model,
        endpoint = @Endpoint,
        api_key = @ApiKey,
        enable_local_model = @EnableLocalModel,
        local_model_memory_limit = @LocalModelMemoryLimit
    WHERE user_account = @UserAccount";
                }
                else
                {
                    // 插入新配置
                    sql = @"
    INSERT INTO api_config (
        user_account,
        provider,
        model,
        endpoint,
        api_key,
        enable_local_model,
        local_model_memory_limit
    )
    VALUES (
        @UserAccount,
        @Provider,
        @Model,
        @Endpoint,
        @ApiKey,
        @EnableLocalModel,
        @LocalModelMemoryLimit
    )";
                }

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", apiConfig.UserAccount);
                cmd.Parameters.AddWithValue("@Provider", apiConfig.Provider);
                cmd.Parameters.AddWithValue("@Model", apiConfig.Model);
                cmd.Parameters.AddWithValue("@Endpoint", apiConfig.Endpoint ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ApiKey", EncryptionHelper.Encrypt(apiConfig.ApiKey ?? string.Empty));
                cmd.Parameters.AddWithValue("@EnableLocalModel", apiConfig.EnableLocalModel ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LocalModelMemoryLimit", apiConfig.LocalModelMemoryLimit ?? (object)DBNull.Value);

                errorMessage = string.Empty;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 加载用户的API配置
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <returns>API配置实体</returns>
        public ChatAI.Data.Entities.ApiConfig? LoadApiConfig(string userAccount)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                const string sql = @"
    SELECT id, user_account, provider, model, endpoint, api_key, enable_local_model, local_model_memory_limit
    FROM api_config
    WHERE user_account = @UserAccount LIMIT 1";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", userAccount);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string? apiKeyEncrypted = reader["api_key"]?.ToString();
                    string apiKey = string.Empty;

                    if (!string.IsNullOrEmpty(apiKeyEncrypted))
                    {
                        try
                        {
                            apiKey = EncryptionHelper.Decrypt(apiKeyEncrypted);
                        }
                        catch
                        {
                            apiKey = apiKeyEncrypted;
                        }
                    }

                    return new ChatAI.Data.Entities.ApiConfig
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        UserAccount = reader["user_account"]?.ToString() ?? string.Empty,
                        Provider = reader["provider"]?.ToString() ?? string.Empty,
                        Model = reader["model"]?.ToString() ?? string.Empty,
                        Endpoint = reader["endpoint"]?.ToString(),
                        ApiKey = apiKey,
                        EnableLocalModel = reader.IsDBNull(reader.GetOrdinal("enable_local_model")) ? null : reader.GetBoolean(reader.GetOrdinal("enable_local_model")),
                        LocalModelMemoryLimit = reader.IsDBNull(reader.GetOrdinal("local_model_memory_limit")) ? null : reader.GetInt32(reader.GetOrdinal("local_model_memory_limit"))
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载API配置失败：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取用户配置
        /// </summary>
        /// <param name="userAccount">用户名</param>
        /// <returns>用户配置实体</returns>
        public UserProfile? GetUserProfile(string userAccount)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                const string sql = @"
    SELECT id, user_account, nickname, gender, persona, system_prompt
    FROM user_profile
    WHERE user_account = @UserAccount LIMIT 1";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", userAccount);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string? nicknameEncrypted = reader["nickname"]?.ToString();
                    string? personaEncrypted = reader["persona"]?.ToString();
                    string? systemPromptEncrypted = reader["system_prompt"]?.ToString();
                    string nickname = string.Empty;
                    string persona = string.Empty;
                    string systemPrompt = string.Empty;
                    try { nickname = EncryptionHelper.Decrypt(nicknameEncrypted ?? string.Empty); } catch { nickname = nicknameEncrypted ?? string.Empty; }
                    try { persona = EncryptionHelper.Decrypt(personaEncrypted ?? string.Empty); } catch { persona = personaEncrypted ?? string.Empty; }
                    try { systemPrompt = EncryptionHelper.Decrypt(systemPromptEncrypted ?? string.Empty); } catch { systemPrompt = systemPromptEncrypted ?? string.Empty; }

                    return new UserProfile
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        UserAccount = reader["user_account"]?.ToString() ?? string.Empty,
                        Nickname = nickname,
                        Gender = reader["gender"]?.ToString(),
                        Persona = persona,
                        SystemPrompt = systemPrompt
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取用户配置失败：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 保存用户配置到数据库
        /// </summary>
        /// <param name="userProfile">用户配置实体</param>
        /// <param name="errorMessage">错误信息输出</param>
        /// <returns>是否保存成功</returns>
        public bool SaveUserProfile(UserProfile userProfile, out string errorMessage)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                const string checkSql = @"
    SELECT COUNT(*) FROM user_profile
    WHERE user_account = @UserAccount";

                using var checkCmd = new SQLiteCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@UserAccount", userProfile.UserAccount);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                string sql;
                if (count > 0)
                {
                    sql = @"
    UPDATE user_profile SET
        nickname = @Nickname,
        gender = @Gender,
        persona = @Persona,
        system_prompt = @SystemPrompt
    WHERE user_account = @UserAccount";
                }
                else
                {
                    sql = @"
    INSERT INTO user_profile (
        user_account,
        nickname,
        gender,
        persona,
        system_prompt
    )
    VALUES (
        @UserAccount,
        @Nickname,
        @Gender,
        @Persona,
        @SystemPrompt
    )";
                }

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserAccount", userProfile.UserAccount);
                cmd.Parameters.AddWithValue("@Nickname", EncryptionHelper.Encrypt(userProfile.Nickname ?? string.Empty));
                cmd.Parameters.AddWithValue("@Gender", userProfile.Gender ?? string.Empty);
                cmd.Parameters.AddWithValue("@Persona", EncryptionHelper.Encrypt(userProfile.Persona ?? string.Empty));
                cmd.Parameters.AddWithValue("@SystemPrompt", EncryptionHelper.Encrypt(userProfile.SystemPrompt ?? string.Empty));

                errorMessage = string.Empty;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 加载会话设置
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <returns>会话设置实体，如果不存在则返回null</returns>
        public Data.Entities.SessionSettings? LoadSessionSettings(string userAccount)
        {
            try
            {
                string sql = "SELECT id, user_account, no_role, history_count, max_prompt_tokens, temperature, memory_trigger, memory_prompt FROM session_settings WHERE user_account = @UserAccount";
                var parameters = new Dictionary<string, object> { { "@UserAccount", userAccount } };
                using var reader = SqliteDbHelper.ExecuteReader(sql, parameters);
                if (reader.Read())
                {
                    string memoryPromptEncrypted = reader.IsDBNull(7) ? null : reader.GetString(7);
                    string memoryPrompt = null;
                    if (!string.IsNullOrEmpty(memoryPromptEncrypted))
                    {
                        try
                        {
                            memoryPrompt = EncryptionHelper.Decrypt(memoryPromptEncrypted);
                        }
                        catch
                        {
                            // 如果解密失败（可能是旧数据未加密），直接使用原值
                            memoryPrompt = memoryPromptEncrypted;
                        }
                    }
                    
                    return new Data.Entities.SessionSettings
                    {
                        Id = reader.GetInt32(0),
                        UserAccount = reader.GetString(1),
                        NoRole = reader.GetInt32(2) == 1,
                        HistoryCount = reader.GetInt32(3),
                        MaxPromptTokens = reader.GetInt32(4),
                        Temperature = reader.GetDouble(5),
                        MemoryTrigger = reader.GetInt32(6),
                        MemoryPrompt = memoryPrompt
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载会话设置失败：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存会话设置
        /// </summary>
        /// <param name="settings">会话设置实体</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否保存成功</returns>
        public bool SaveSessionSettings(Data.Entities.SessionSettings settings, out string errorMessage)
        {
            try
            {
                // 检查用户账号是否为空
                if (string.IsNullOrWhiteSpace(settings.UserAccount))
                {
                    errorMessage = "用户账号不能为空";
                    return false;
                }
                
                // 检查是否已存在
                var existingSettings = LoadSessionSettings(settings.UserAccount);
                if (existingSettings != null)
                {
                    // 更新现有记录
                string sql = @"
                    UPDATE session_settings 
                    SET no_role = @NoRole, 
                        history_count = @HistoryCount, 
                        max_prompt_tokens = @MaxPromptTokens, 
                        temperature = @Temperature, 
                        memory_trigger = @MemoryTrigger, 
                        memory_prompt = @MemoryPrompt 
                    WHERE user_account = @UserAccount";
                var updateParams = new
                {
                    UserAccount = settings.UserAccount,
                    NoRole = settings.NoRole ? 1 : 0,
                    HistoryCount = settings.HistoryCount,
                    MaxPromptTokens = settings.MaxPromptTokens,
                    Temperature = settings.Temperature,
                    MemoryTrigger = settings.MemoryTrigger,
                    MemoryPrompt = EncryptionHelper.Encrypt(settings.MemoryPrompt ?? string.Empty)
                };
                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, updateParams) > 0;
                }
                else
                {
                    // 插入新记录
                    string sql = @"
                        INSERT INTO session_settings (
                            user_account, 
                            no_role, 
                            history_count, 
                            max_prompt_tokens, 
                            temperature, 
                            memory_trigger, 
                            memory_prompt 
                        ) VALUES (
                            @UserAccount, 
                            @NoRole, 
                            @HistoryCount, 
                            @MaxPromptTokens, 
                            @Temperature, 
                            @MemoryTrigger, 
                            @MemoryPrompt 
                        )";
                    var insertParams = new
                    {
                        UserAccount = settings.UserAccount,
                        NoRole = settings.NoRole ? 1 : 0,
                        HistoryCount = settings.HistoryCount,
                        MaxPromptTokens = settings.MaxPromptTokens,
                        Temperature = settings.Temperature,
                        MemoryTrigger = settings.MemoryTrigger,
                        MemoryPrompt = EncryptionHelper.Encrypt(settings.MemoryPrompt ?? string.Empty)
                    };
                    errorMessage = string.Empty;
                    return SqliteDbHelper.ExecuteNonQuery(sql, insertParams) > 0;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 加载AI的长期记忆
        /// 根据用户账号和AI角色ID查询记忆内容
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="limit">返回的记忆数量，默认5条</param>
        /// <returns>记忆内容列表，如果不存在则返回空列表</returns>
        public List<string> GetAIMemories(string userAccount, int aiCharacterId, int limit = 5)
        {
            var memories = new List<string>();
            try
            {
                string sql = "SELECT memory_content FROM ai_memory WHERE user_account = @UserAccount AND ai_character_id = @AiCharacterId ORDER BY create_time DESC LIMIT @Limit";
                var parameters = new Dictionary<string, object>
                {
                    { "@UserAccount", userAccount },
                    { "@AiCharacterId", aiCharacterId },
                    { "@Limit", limit }
                };
                using var reader = SqliteDbHelper.ExecuteReader(sql, parameters);
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string? memoryContentEncrypted = reader.GetString(0);
                        string memoryContent = string.Empty;
                        try { memoryContent = EncryptionHelper.Decrypt(memoryContentEncrypted ?? string.Empty); } catch { memoryContent = memoryContentEncrypted ?? string.Empty; }
                        memories.Add(memoryContent);
                    }
                }
                return memories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载AI记忆失败：{ex.Message}");
                return memories;
            }
        }

        /// <summary>
        /// 加载AI的长期记忆（兼容旧方法）
        /// 根据用户账号和AI角色ID查询记忆内容
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <returns>记忆内容，如果不存在则返回null</returns>
        public string? GetAIMemory(string userAccount, int aiCharacterId)
        {
            var memories = GetAIMemories(userAccount, aiCharacterId, 1);
            return memories.Count > 0 ? memories[0] : null;
        }

        /// <summary>
        /// 加载用户长期记忆（兼容旧方法）
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <returns>记忆内容，如果不存在则返回null</returns>
        public string? GetUserMemory(string userAccount, int aiCharacterId)
        {
            return GetAIMemory(userAccount, aiCharacterId);
        }

        /// <summary>
        /// 加载用户长期记忆（兼容旧方法）
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="limit">返回的记忆数量，默认5条</param>
        /// <returns>记忆内容列表，如果不存在则返回空列表</returns>
        public List<string> GetUserMemories(string userAccount, int aiCharacterId, int limit = 5)
        {
            return GetAIMemories(userAccount, aiCharacterId, limit);
        }

        /// <summary>
        /// 记忆项，包含ID、内容和创建时间
        /// </summary>
        public class MemoryItem
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 加载AI的长期记忆（包含ID）
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <returns>记忆项列表，如果不存在则返回空列表</returns>
        public List<MemoryItem> GetMemoriesWithIds(string userAccount, int aiCharacterId)
        {
            var memories = new List<MemoryItem>();
            try
            {
                string sql = "SELECT id, memory_content, create_time FROM ai_memory WHERE user_account = @UserAccount AND ai_character_id = @AiCharacterId ORDER BY create_time DESC";
                var parameters = new Dictionary<string, object>
                {
                    { "@UserAccount", userAccount },
                    { "@AiCharacterId", aiCharacterId }
                };
                using var reader = SqliteDbHelper.ExecuteReader(sql, parameters);
                while (reader.Read())
                {
                    string? memoryContentEncrypted = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    string memoryContent = string.Empty;
                    try { memoryContent = EncryptionHelper.Decrypt(memoryContentEncrypted ?? string.Empty); } catch { memoryContent = memoryContentEncrypted ?? string.Empty; }

                    var memory = new MemoryItem
                    {
                        Id = reader.GetInt32(0),
                        Content = memoryContent,
                        CreateTime = reader.IsDBNull(2) ? DateTime.Now : DateTime.Parse(reader.GetString(2))
                    };
                    memories.Add(memory);
                }
                return memories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载记忆失败：{ex.Message}");
                return memories;
            }
        }

        /// <summary>
        /// 删除记忆
        /// </summary>
        /// <param name="memoryId">记忆ID</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteMemory(int memoryId, out string errorMessage)
        {
            try
            {
                string sql = "DELETE FROM ai_memory WHERE id = @MemoryId";
                var parameters = new { MemoryId = memoryId };
                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 更新记忆
        /// </summary>
        /// <param name="memoryId">记忆ID</param>
        /// <param name="memoryContent">新的记忆内容</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否更新成功</returns>
        public bool UpdateMemory(int memoryId, string memoryContent, out string errorMessage)
        {
            try
            {
                string sql = "UPDATE ai_memory SET memory_content = @MemoryContent, create_time = @CreateTime WHERE id = @MemoryId";
                var parameters = new
                {
                    MemoryContent = memoryContent,
                    CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    MemoryId = memoryId
                };
                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 保存AI的长期记忆
        /// 每次都插入新的记忆记录，不更新现有记忆
        /// </summary>
        /// <param name="userAccount">用户账号</param>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="aiName">AI名称</param>
        /// <param name="memoryContent">记忆内容</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否保存成功</returns>
        public bool SaveAIMemory(string userAccount, int aiCharacterId, string aiName, string memoryContent, out string errorMessage)
        {
            try
            {
                // 插入新记忆（每次都创建新记录，不更新现有记忆）
                string insertSql = "INSERT INTO ai_memory (user_account, ai_name, ai_character_id, memory_content, create_time) VALUES (@UserAccount, @AiName, @AiCharacterId, @MemoryContent, @CreateTime)";
                var insertParams = new
                {
                    UserAccount = userAccount,
                    AiName = aiName,
                    AiCharacterId = aiCharacterId,
                    MemoryContent = EncryptionHelper.Encrypt(memoryContent),
                    CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                errorMessage = string.Empty;
                return SqliteDbHelper.ExecuteNonQuery(insertSql, insertParams) > 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// 获取指定数量的最新聊天记录
        /// 用于构建对话上下文和记忆生成
        /// </summary>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="userAccount">用户账号</param>
        /// <param name="count">要获取的记录数量</param>
        /// <returns>聊天消息列表</returns>
        public List<ChatMessageEntity> GetRecentChatMessages(int aiCharacterId, string userAccount, int count)
        {
            var messages = new List<ChatMessageEntity>();
            try
            {
                string sql = @"SELECT id, user_account, ai_name, ai_character_id, sender, content, create_time
                              FROM chat_message
                              WHERE ai_character_id = @AiCharacterId AND user_account = @UserAccount
                              ORDER BY create_time DESC LIMIT @Count";
                var parameters = new Dictionary<string, object>
                {
                    { "@AiCharacterId", aiCharacterId },
                    { "@UserAccount", userAccount },
                    { "@Count", count }
                };
                using var reader = SqliteDbHelper.ExecuteReader(sql, parameters);
                while (reader.Read())
                {
                    string? contentEncrypted = reader.GetString(5);
                    string content = string.Empty;
                    try { content = EncryptionHelper.Decrypt(contentEncrypted ?? string.Empty); } catch { content = contentEncrypted ?? string.Empty; }

                    messages.Add(new ChatMessageEntity
                    {
                        Id = reader.GetInt32(0),
                        UserAccount = reader.GetString(1),
                        AiName = reader.GetString(2),
                        AiCharacterId = reader.GetInt32(3),
                        Sender = reader.GetString(4),
                        Content = content,
                        CreateTime = DateTime.Parse(reader.GetString(6))
                    });
                }
                // 反转列表，使最早的消息在前
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取聊天记录失败：{ex.Message}");
                return messages;
            }
        }

        /// <summary>
        /// 获取聊天记录总数
        /// 用于判断是否需要触发记忆生成
        /// </summary>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="userAccount">用户账号</param>
        /// <returns>消息总数</returns>
        public int GetChatMessageCount(int aiCharacterId, string userAccount)
        {
            try
            {
                string sql = "SELECT COALESCE(MAX(total_message_count), 0) FROM chat_message WHERE ai_character_id = @AiCharacterId AND user_account = @UserAccount";
                var parameters = new Dictionary<string, object>
                {
                    { "@AiCharacterId", aiCharacterId },
                    { "@UserAccount", userAccount }
                };
                using var reader = SqliteDbHelper.ExecuteReader(sql, parameters);
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取消息总数失败：{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取指定会话的最大消息总数（用于记忆生成触发计数）
        /// </summary>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="userAccount">用户账号</param>
        /// <returns>最大消息总数</returns>
        public int GetMaxTotalMessageCount(int aiCharacterId, string userAccount)
        {
            try
            {
                string sql = "SELECT COALESCE(MAX(total_message_count), 0) FROM chat_message WHERE ai_character_id = @AiCharacterId AND user_account = @UserAccount";
                var parameters = new Dictionary<string, object>
                {
                    { "@AiCharacterId", aiCharacterId },
                    { "@UserAccount", userAccount }
                };
                using var reader = SqliteDbHelper.ExecuteReader(sql, parameters);
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最大消息总数失败：{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 删除会话及其相关数据（聊天记录、记忆）
        /// </summary>
        /// <param name="aiCharacterId">AI角色ID</param>
        /// <param name="userAccount">用户账号</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteSession(int aiCharacterId, string userAccount, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();

                using var transaction = conn.BeginTransaction();

                try
                {
                    // 1. 删除聊天记录
                    string deleteChatSql = "DELETE FROM chat_message WHERE ai_character_id = @AiCharacterId AND user_account = @UserAccount";
                    using (var cmd = new SQLiteCommand(deleteChatSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@AiCharacterId", aiCharacterId);
                        cmd.Parameters.AddWithValue("@UserAccount", userAccount);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. 删除AI记忆
                    string deleteMemorySql = "DELETE FROM ai_memory WHERE ai_character_id = @AiCharacterId AND user_account = @UserAccount";
                    using (var cmd = new SQLiteCommand(deleteMemorySql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@AiCharacterId", aiCharacterId);
                        cmd.Parameters.AddWithValue("@UserAccount", userAccount);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. 删除AI角色
                    string deleteCharacterSql = "DELETE FROM ai_character WHERE id = @Id AND user_account = @UserAccount";
                    using (var cmd = new SQLiteCommand(deleteCharacterSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Id", aiCharacterId);
                        cmd.Parameters.AddWithValue("@UserAccount", userAccount);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    errorMessage = ex.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// 聊天消息实体类（用于内部数据传递）
    /// </summary>
    public class ChatMessageEntity
    {
        public int Id { get; set; }
        public string? UserAccount { get; set; }
        public string? AiName { get; set; }
        public int AiCharacterId { get; set; }
        public string? Sender { get; set; }
        public string? Content { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 用户仓储接口
    /// 包含：登录、注册、改密、用户存在性、记住密码、加载记住账号
    /// </summary>
    public interface IUserRepository
    {
        bool Login(string username, string password);
        bool IsUsernameExists(string username);
        bool Register(string username, string password);
        bool ChangePassword(string username, string oldPwd, string newPwd);

        // 新增：记住密码相关（从FrmLogin移过来）
        UserAccount? GetRememberMeAccount();
        void UpdateRememberMe(string username, bool isRemember);
    }

    /// <summary>
    /// 用于返回 账号+密码 实体
    /// </summary>
    public class UserAccount
    {
        public string? Account { get; set; }
        public string? Password { get; set; }
    }


    public class SqliteUserRepository : IUserRepository
    {
        /// <summary>
        /// 用户登录验证
        /// </summary>
        public bool Login(string username, string password)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT COUNT(*) FROM user_login
                    WHERE account=@account AND password=@pwd";
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@account", username);
                cmd.Parameters.AddWithValue("@pwd", EncryptionHelper.Encrypt(password));
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"登录验证失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断用户名是否已存在
        /// </summary>
        public bool IsUsernameExists(string username)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT COUNT(*) FROM user_login 
                    WHERE account=@account";
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@account", username);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"判断用户名是否存在失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        public bool Register(string username, string password)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                const string sql = @"
                    INSERT INTO user_login (remember_me, account, password)
                    VALUES (0, @account, @pwd)";
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@account", username);
                cmd.Parameters.AddWithValue("@pwd", EncryptionHelper.Encrypt(password));
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"用户注册失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        public bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                const string sql = @"
                    UPDATE user_login
                    SET password=@newPwd
                    WHERE account=@account AND password=@oldPwd";
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@account", username);
                cmd.Parameters.AddWithValue("@oldPwd", EncryptionHelper.Encrypt(oldPwd));
                cmd.Parameters.AddWithValue("@newPwd", EncryptionHelper.Encrypt(newPwd));
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"修改密码失败：{ex.Message}");
                return false;
            }
        }

        #region 记住密码功能（从FrmLogin完整迁移）
        /// <summary>
        /// 获取被记住密码的账户
        /// </summary>
        public UserAccount? GetRememberMeAccount()
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT account,password FROM user_login
                    WHERE remember_me=1 LIMIT 1";
                using var cmd = new SQLiteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string? passwordEncrypted = reader["password"]?.ToString();
                    string password = string.Empty;
                    if (!string.IsNullOrEmpty(passwordEncrypted))
                    {
                        try
                        {
                            password = EncryptionHelper.Decrypt(passwordEncrypted);
                        }
                        catch
                        {
                            password = passwordEncrypted;
                        }
                    }
                    return new UserAccount
                    {
                        Account = reader["account"]?.ToString(),
                        Password = password
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取记住密码账户失败：{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 更新记住密码状态
        /// </summary>
        public void UpdateRememberMe(string username, bool isRemember)
        {
            try
            {
                using var conn = new SQLiteConnection(SqliteDbHelper.ConnectionString);
                conn.Open();
                const string sql = @"
                    UPDATE user_login SET remember_me=@rememberMe WHERE account=@account";
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@rememberMe", isRemember ? 1 : 0);
                cmd.Parameters.AddWithValue("@account", username);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新记住密码状态失败：{ex.Message}");
            }
        }
        #endregion
    }
}
