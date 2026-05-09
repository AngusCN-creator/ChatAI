using System;
using System.Data.SQLite;
using System.IO;

namespace ChatAI.Data
{
    /// <summary>
    /// SQLite 数据库帮助类
    /// 功能：自动创建Data.db、创建7张业务表、提供连接字符串
    /// 对应文档：AI聊天工具数据库设计（最终优化版）
    /// </summary>
    public static class SqliteDbHelper
    {
        /// <summary>
        /// 数据库文件名
        /// </summary>
        private static readonly string dbFileName = "Data.db";

        /// <summary>
        /// 数据库完整路径（缓存）
        /// </summary>
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbFileName);

        /// <summary>
        /// 公开连接字符串（供所有仓储类使用，缓存）
        /// </summary>
        public static readonly string ConnectionString = $"Data Source={DbPath};Version=3;";

        /// <summary>
        /// 初始化数据库与表（程序启动调用一次）
        /// </summary>
        public static void InitDatabaseAndTables()
        {
            try
            {
                bool isNewDatabase = false;

                // 不存在则创建数据库
                if (!File.Exists(DbPath))
                {
                    SQLiteConnection.CreateFile(DbPath);
                    ExecuteSql(GetCreateTablesSql()); // 包含 Sys_Secret
                    isNewDatabase = true; // 标记：这是全新库
                }

                // ==============================================
                // ✅ 密钥初始化：新库生成随机根密钥 / 旧库加载
                // ==============================================
                ChatAI.Data.EncryptionHelper.InitializeRootKey(isNewDatabase);
            }
            catch (Exception ex)
            {
                throw new Exception($"数据库初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 执行非查询SQL（建表、增、删、改）
        /// </summary>
        private static void ExecuteSql(string sql)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 执行INSERT/UPDATE/DELETE等非查询SQL，返回受影响行数
        /// </summary>
        public static int ExecuteNonQuery(string sql, object? param = null)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand(sql, conn);

            if (param != null)
            {
                foreach (var prop in param.GetType().GetProperties())
                {
                    object? value = prop.GetValue(param);
                    
                    // 特殊处理DateTime类型，转换为ISO 8601格式的字符串
                    if (value is DateTime dateTime)
                    {
                        value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    
                    cmd.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
                }
            }

            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行查询SQL，返回SQLiteDataReader
        /// </summary>
        public static SQLiteDataReader ExecuteReader(string sql, System.Collections.Generic.Dictionary<string, object> parameters)
        {
            var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            var cmd = new SQLiteCommand(sql, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
            }

            return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// 执行标量查询（返回单个值，比如 SELECT WrapRoot FROM Sys_Secret WHERE Id=1）
        /// </summary>
        public static T ExecuteScalar<T>(string sql, object? param = null)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SQLiteCommand(sql, conn);

            if (param != null)
            {
                foreach (var prop in param.GetType().GetProperties())
                {
                    object? value = prop.GetValue(param);
                    if (value is DateTime dateTime)
                    {
                        value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    cmd.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
                }
            }

            object? result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return default!;

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// 执行增删改SQL（公开版本，供仓储类使用）
        /// </summary>
        public static int ExecuteSqlPublic(string sql, object? param = null)
        {
            return ExecuteNonQuery(sql, param);
        }

        /// <summary>
        /// 建表语句（最终定稿版）
        /// 所有表均包含 user_account，实现多用户数据隔离
        /// </summary>
        private static string GetCreateTablesSql()
        {
            return @"
-- =============================================
-- 数据库名称: Data.db
-- 功能: AI聊天助手完整数据存储
-- =============================================

-- ==========================
-- 加密密钥表
-- 作用: 存储加密密钥
-- ==========================
CREATE TABLE IF NOT EXISTS Sys_Secret (
    Id INTEGER PRIMARY KEY CHECK(Id=1),
    WrapRoot TEXT NOT NULL
);

-- ==========================
-- 用户登录信息表
-- 作用: 存储账号、密码、记住密码标记
-- ==========================
CREATE TABLE IF NOT EXISTS user_login (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    remember_me INTEGER NOT NULL DEFAULT 0,
    account TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL
);

-- ==========================
-- 用户信息表
-- 作用: 存储用户资料、昵称、人设
-- ==========================
CREATE TABLE IF NOT EXISTS user_profile (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_account TEXT NOT NULL,
    nickname TEXT NOT NULL,
    gender TEXT NULL,
    persona TEXT NULL,
    system_prompt TEXT NULL
);

-- ==========================
-- API配置表
-- 作用: 存储大模型接口信息
-- ==========================
CREATE TABLE IF NOT EXISTS api_config (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_account TEXT NOT NULL,
    provider TEXT NOT NULL,
    model TEXT NOT NULL,
    endpoint TEXT NULL,
    api_key TEXT NULL,
    enable_local_model INTEGER NULL DEFAULT 0,
    local_model_memory_limit INTEGER NULL DEFAULT 0
);

-- ==========================
-- 会话设置表
-- 作用: 对话参数、记忆生成规则
-- ==========================
CREATE TABLE IF NOT EXISTS session_settings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_account TEXT NOT NULL,
    no_role INTEGER NOT NULL DEFAULT 0,
    history_count INTEGER NOT NULL DEFAULT 10,
    max_prompt_tokens INTEGER NOT NULL DEFAULT 1000,
    temperature REAL NOT NULL DEFAULT 0.7,
    memory_trigger INTEGER NOT NULL DEFAULT 20,
    memory_prompt TEXT NULL
);

-- ==========================
-- AI人物信息表
-- 作用: 存储每个AI角色资料
-- ==========================
CREATE TABLE IF NOT EXISTS ai_character (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_account TEXT NOT NULL,
    ai_name TEXT NOT NULL,
    ai_gender TEXT NULL,
    ai_persona TEXT NULL,
    ai_style TEXT NULL,
    ai_habit TEXT NULL,
    ai_opening TEXT NULL,
    create_time TEXT NOT NULL
);

-- ==========================
-- 聊天消息记录表
-- 作用: 存储用户与AI的每一条对话
-- ==========================
CREATE TABLE IF NOT EXISTS chat_message (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_account TEXT NOT NULL,
    ai_name TEXT NOT NULL,
    ai_character_id INTEGER NOT NULL,
    sender TEXT NOT NULL,
    content TEXT NOT NULL,
    total_message_count INTEGER NOT NULL DEFAULT 0,
    create_time TEXT NOT NULL
);

-- ==========================
-- AI记忆表
-- 作用: 存储AI自动总结的对话记忆
-- ==========================
CREATE TABLE IF NOT EXISTS ai_memory (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_account TEXT NOT NULL,
    ai_name TEXT NOT NULL,
    ai_character_id INTEGER NOT NULL,
    memory_content TEXT NOT NULL,
    create_time TEXT NOT NULL
);
            ";
        }
    }
}