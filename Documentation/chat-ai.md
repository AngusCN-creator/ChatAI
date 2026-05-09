# AI聊天工具（ChatAI）工程设计开发方案

> **版本**: v2.1
> **日期**: 2026-05-08
> **技术栈**: C# .NET 10 · WinForms · SQLite (System.Data.SQLite) · OpenAI API 兼容协议 · LLamaSharp (.gguf)
> **架构风格**: 三层架构 + 事件驱动 + 策略模式
> **源码规模**: ChatAI（25个.cs文件）+ ChatControl（5个.cs文件）= 30个文件

---

## 目录

1. [项目概述](#1-项目概述)
2. [解决方案结构](#2-解决方案结构)
3. [程序入口与启动流程](#3-程序入口与启动流程)
4. [数据库设计](#4-数据库设计)（含 Sys_Secret 密钥表）
5. [实体类设计](#5-实体类设计)
6. [数据访问层](#6-数据访问层)（含双层加密实现详情）
7. [服务层](#7-服务层)（含本地模型推理、模型格式策略）
8. [UI表现层 — 窗体](#8-ui表现层--窗体)
9. [UI表现层 — 自定义控件库](#9-ui表现层--自定义控件库)
10. [核心业务流程](#10-核心业务流程)
11. [加密体系](#11-加密体系)（双层架构完整分析）
12. [事件驱动架构](#12-事件驱动架构)
13. [代码质量改进建议](#13-代码质量改进建议)
- [附录 A：文件索引](#附录-a文件索引)
- [附录 B：API请求格式参考](#附录-bapi请求格式参考)
- [附录 C：本地模型配置参考](#附录-c本地模型配置参考)

---

## 1. 项目概述

### 1.1 产品定位

一款桌面端 AI 聊天客户端工具，支持以下核心能力：

- **云端在线大模型**: 通过 OpenAI Chat Completions 兼容协议，对接 11 家 AI 服务商
- **本地离线私有大模型**: 通过 LlamaSharp 加载 .gguf 格式模型文件，支持 GPU 加速推理
- **多会话管理**: 用户可创建多个 AI 角色会话，每个会话拥有独立的人设、对话历史、记忆系统
- **自动长期记忆**: 根据对话条数阈值，自动调用 AI 总结对话并生成长期记忆，注入后续对话上下文
- **全字段加密**: 所有敏感数据（API Key、密码、用户信息等）通过 AES-256-CBC 加密存储

### 1.2 核心特色

| 特性 | 技术实现 |
|------|---------|
| GDI+ 自绘气泡引擎 | `ChatMainControl`（1392行），无第三方UI依赖，手动绘制圆角气泡、头像、时间戳 |
| **双层 AES-256 加密** | `EncryptionHelper` — 外层K2（代码混淆常量）包裹内层K1（随机根密钥），确定性IV支持密文等值查询 |
| **本地模型 LLamaSharp 推理** | `LocalModelService` — 进程内加载 .gguf，CUDA/Vulkan/CPU 自适应，流式推理+停词截断 |
| 自动长期记忆 | 队列化异步记忆生成，`_pendingMemoryRequests` + `_memoryLock` |
| 消息回溯 | 右键菜单触发 `MessageRollbackRequested`，确定性IV密文匹配 + 级联删除消息与记忆 |
| 多模型格式适配 | `IModelFormat` + `ModelFormatFactory` 策略模式（Qwen ChatML / Hermes Llama-3） |
| 本地模型单例 | `LocalModelServiceSingleton` 双重检查锁，避免重复加载 .gguf；每次推理后释放VRAM |
| 云/本地无缝切换 | `ApiConfig.EnableLocalModel` 字段复用（`Endpoint`→文件路径，`ApiKey`→GPU模式）|

---

## 2. 解决方案结构

### 2.1 项目组成

```
解决方案
├── ChatAI/                          # 主应用程序项目
│   ├── Program.cs                   # 程序入口
│   ├── Data/                        # 数据层
│   │   ├── SqliteDbHelper.cs        # 数据库初始化与建表
│   │   ├── EncryptionHelper.cs      # AES加密/解密工具
│   │   ├── UserRepository.cs        # 数据仓库（God Class，1400+行）
│   │   └── Entities/                # 实体类目录
│   │       ├── UserLogin.cs
│   │       ├── UserProfile.cs
│   │       ├── ApiConfig.cs
│   │       ├── SessionSettings.cs
│   │       ├── AiCharacter.cs
│   │       ├── ChatMessage.cs
│   │       └── AiMemory.cs
│   ├── Services/                    # 服务层
│   │   ├── TextGenerator.cs         # 核心AI文本生成服务（~67KB，核心）
│   │   ├── LocalModelService.cs     # 本地模型推理服务（LLamaSharp）
│   │   └── ModelFormats/            # 模型格式策略
│   │       ├── IModelFormat.cs      # 接口定义
│   │       ├── QwenModelFormat.cs   # 通义千问格式
│   │       ├── HermesModelFormat.cs # Hermes格式
│   │       └── ModelFormatFactory.cs# 工厂
│   └── UI/Forms/                    # 表现层（窗体）
│       ├── FrmLogin.cs              # 登录/注册/改密
│       ├── FrmChatMain.cs           # 主聊天窗体（1083行）
│       ├── FrmApiConfig.cs          # API配置
│       ├── FrmSessionSettings.cs    # 会话参数设置
│       ├── FrmMemoryManager.cs      # 记忆管理
│       ├── FrmUserProfileSettings.cs# 用户资料设置
│       └── CreateSessionForm.cs     # 创建/编辑AI角色
│
├── ChatControl/                     # 自定义控件库项目
│   ├── Controls/
│   │   ├── ChatMainControl.cs       # GDI+自绘气泡引擎（1392行）
│   │   ├── SessionListUC.cs         # 会话列表控件（260行）
│   │   └── UCAvatarBox.cs           # 头像控件
│   ├── Models/
│   │   └── SessionInfo.cs           # 会话信息DTO
│   └── Utils/
│       └── GraphicsExtensions.cs    # GDI+扩展方法
│
└── Data.db                          # SQLite数据库文件（运行时生成）
```

### 2.2 依赖关系

```
FrmChatMain ──→ ChatMainControl ──→ (GDI+ 自绘)
FrmChatMain ──→ SessionListUC ──→ (OwnerDraw ListBox)
FrmChatMain ──→ TextGenerator ──→ UserRepository
FrmChatMain ──→ UserRepository ──→ SqliteDbHelper
FrmChatMain ──→ EncryptionHelper
TextGenerator ──→ LocalModelService (本地模型)
TextGenerator ──→ ModelFormatFactory ──→ IModelFormat
FrmLogin ──→ IUserRepository ──→ SqliteUserRepository
所有窗体 ──→ UserRepository (数据操作)
```

---

## 3. 程序入口与启动流程

### 3.1 Program.cs 核心逻辑

```csharp
// 关键全局状态
static bool IsExitApplication = false;  // 全局退出标记

// 启动流程伪代码
while (true)
{
    // 1. 创建并显示登录窗体
    FrmLogin loginForm = new FrmLogin();
    Application.Run(loginForm);

    // 2. 判断登录结果
    if (IsExitApplication)
        break;  // 用户关闭登录窗体 → 退出程序

    // 3. 登录成功 → 进入主窗体
    FrmChatMain chatMain = new FrmChatMain(loginForm.LoggedInUsername);
    Application.Run(chatMain);

    // 4. 主窗体关闭后，检查是否注销
    if (chatMain.IsLogout)
        continue;  // 注销 → 回到登录循环
    else
        break;      // 正常关闭 → 退出程序
}
```

### 3.2 启动时序图

```
用户启动程序
    │
    ▼
┌─────────────┐
│ Program.Main │
└──────┬──────┘
       │ while(true)
       ▼
┌─────────────┐
│ FrmLogin     │ ← Application.Run()
│ 登录/注册    │
└──────┬──────┘
       │ 登录成功
       ▼
┌─────────────┐
│ FrmChatMain  │ ← Application.Run()
│ 主聊天界面   │
└──────┬──────┘
       │
       ├── 注销 → 回到 FrmLogin
       └── 关闭 → IsExitApplication = true → 程序退出
```

---

## 4. 数据库设计

### 4.1 数据库概述

- **类型**: SQLite 3（单文件存储 `Data.db`）
- **访问方式**: System.Data.SQLite
- **加密**: 应用层双层 AES-256-CBC 加密敏感字段（详见第11章）
- **外键约束**: 无物理外键，应用层保证引用完整性
- **连接管理**: `SqliteDbHelper` 中静态缓存连接字符串
- **表总数**: **8张**（含系统密钥表 `Sys_Secret`）

### 4.2 表结构定义

#### 4.2.1 user_login — 用户登录表

```sql
CREATE TABLE IF NOT EXISTS user_login (
    username    TEXT PRIMARY KEY,        -- 用户名（主键）
    password    TEXT NOT NULL            -- 密码（AES加密存储）
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `username` | TEXT | PRIMARY KEY | 用户名，唯一标识 |
| `password` | TEXT | NOT NULL | 密码，AES加密后的密文 |

#### 4.2.2 user_profile — 用户资料表

```sql
CREATE TABLE IF NOT EXISTS user_profile (
    username      TEXT PRIMARY KEY,        -- 用户名（外键→user_login.username）
    nickname      TEXT,                    -- 昵称
    gender        TEXT DEFAULT '男',       -- 性别（"男"/"女"）
    persona       TEXT,                    -- 人设设定
    system_prompt TEXT                     -- 系统提示词
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `username` | TEXT | PRIMARY KEY | 用户名，与 user_login 1:1 对应 |
| `nickname` | TEXT | 可空 | 用户昵称 |
| `gender` | TEXT | DEFAULT '男' | 性别，存储"男"或"女" |
| `persona` | TEXT | 可空 | 用户人设设定文本 |
| `system_prompt` | TEXT | 可空 | 用户自定义系统提示词 |

#### 4.2.3 api_config — API配置表

```sql
CREATE TABLE IF NOT EXISTS api_config (
    username            TEXT PRIMARY KEY,        -- 用户名
    provider            TEXT,                    -- 服务商标识
    base_url            TEXT,                    -- 接口地址
    api_key             TEXT,                    -- API密钥（AES加密）
    model_name          TEXT,                    -- 模型名称
    enable_local_model  INTEGER DEFAULT 0,       -- 是否启用本地模型（0/1）
    local_model_path    TEXT,                    -- 本地模型文件路径
    gpu_accel_mode      TEXT,                    -- GPU加速模式
    memory_limit_mb     INTEGER                  -- 显存/内存上限(MB)
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `username` | TEXT | PRIMARY KEY | 用户名 |
| `provider` | TEXT | 可空 | 服务商标识（如"OpenAI"、"智谱AI"等） |
| `base_url` | TEXT | 可空 | API接口基础URL |
| `api_key` | TEXT | 可空 | API密钥（AES加密存储） |
| `model_name` | TEXT | 可空 | 选择的模型名称 |
| `enable_local_model` | INTEGER | DEFAULT 0 | 0=云端API，1=本地模型 |
| `local_model_path` | TEXT | 可空 | .gguf模型文件绝对路径 |
| `gpu_accel_mode` | TEXT | 可空 | GPU加速模式字符串 |
| `memory_limit_mb` | INTEGER | 可空 | 显存/内存限制（MB） |

#### 4.2.4 session_settings — 会话设置表

```sql
CREATE TABLE IF NOT EXISTS session_settings (
    username          TEXT,                    -- 用户名
    character_id      INTEGER,                 -- AI角色ID（外键→ai_character.id）
    context_length    INTEGER DEFAULT 20,      -- 上下文长度（对话轮数）
    max_tokens        INTEGER DEFAULT 4096,    -- 最大上行Tokens
    temperature       REAL DEFAULT 0.3,        -- 温度值（0.0-2.0）
    auto_memory_count INTEGER DEFAULT 20,      -- 自动记忆触发条数
    memory_prompt     TEXT,                    -- 记忆生成提示词
    no_role           INTEGER DEFAULT 0,       -- 纯净模式（0/1）
    PRIMARY KEY (username, character_id)       -- 复合主键
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `username` | TEXT | — | 用户名 |
| `character_id` | INTEGER | — | AI角色ID，对应 ai_character.id |
| `context_length` | INTEGER | DEFAULT 20 | 发送给API的历史消息轮数 |
| `max_tokens` | INTEGER | DEFAULT 4096 | 单次请求最大上行token数 |
| `temperature` | REAL | DEFAULT 0.3 | AI回复随机性（0.0=确定性，2.0=高度随机） |
| `auto_memory_count` | INTEGER | DEFAULT 20 | 每累计N条消息自动触发记忆生成 |
| `memory_prompt` | TEXT | 可空 | 用于指导AI生成记忆的提示词 |
| `no_role` | INTEGER | DEFAULT 0 | 1=纯净模式，禁用所有角色设定 |

#### 4.2.5 ai_character — AI角色表

```sql
CREATE TABLE IF NOT EXISTS ai_character (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,  -- 角色ID（自增主键）
    username   TEXT,                                -- 所属用户名
    nickname   TEXT NOT NULL,                       -- 角色昵称
    gender     TEXT DEFAULT '男',                   -- 角色性别（"男"/"女"）
    role_desc  TEXT,                                -- 人设设定
    talk_style TEXT,                                -- 表达风格
    habit      TEXT,                                -- 习惯用语
    opening    TEXT                                 -- 开场白
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `id` | INTEGER | PRIMARY KEY AUTOINCREMENT | 自增主键 |
| `username` | TEXT | — | 所属用户名 |
| `nickname` | TEXT | NOT NULL | 角色昵称 |
| `gender` | TEXT | DEFAULT '男' | 角色性别 |
| `role_desc` | TEXT | 可空 | 人设描述（性格、背景等） |
| `talk_style` | TEXT | 可空 | 表达风格（语言特点等） |
| `habit` | TEXT | 可空 | 习惯用语（口头禅等） |
| `opening` | TEXT | 可空 | 开场白文本 |

#### 4.2.6 chat_message — 聊天消息表

```sql
CREATE TABLE IF NOT EXISTS chat_message (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,  -- 消息ID
    character_id INTEGER,                            -- 所属角色ID
    is_user      INTEGER NOT NULL,                   -- 是否用户消息（1=用户，0=AI）
    content      TEXT NOT NULL,                      -- 消息内容（AES加密）
    send_time    TEXT NOT NULL                       -- 发送时间
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `id` | INTEGER | PRIMARY KEY AUTOINCREMENT | 消息自增ID |
| `character_id` | INTEGER | — | 所属AI角色ID |
| `is_user` | INTEGER | NOT NULL | 1=用户发送，0=AI回复 |
| `content` | TEXT | NOT NULL | 消息内容（AES加密） |
| `send_time` | TEXT | NOT NULL | 发送时间字符串 |

#### 4.2.7 ai_memory — AI记忆表

```sql
CREATE TABLE IF NOT EXISTS ai_memory (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,  -- 记忆ID
    character_id INTEGER,                            -- 所属角色ID
    content      TEXT NOT NULL,                      -- 记忆内容（AES加密）
    create_time  TEXT NOT NULL                       -- 创建时间
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `id` | INTEGER | PRIMARY KEY AUTOINCREMENT | 记忆自增ID |
| `character_id` | INTEGER | — | 所属AI角色ID |
| `content` | TEXT | NOT NULL | 记忆内容（AES加密） |
| `create_time` | TEXT | NOT NULL | 记忆创建时间 |

#### 4.2.8 Sys_Secret — 系统密钥表（双层加密核心）

```sql
CREATE TABLE IF NOT EXISTS Sys_Secret (
    Id       INTEGER PRIMARY KEY CHECK(Id=1),  -- 约束只允许一行
    WrapRoot TEXT NOT NULL                     -- 内层根密钥K1（由外层K2加密后存储）
);
```

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | INTEGER | PRIMARY KEY, CHECK(Id=1) | 固定为1，全表只存一行 |
| `WrapRoot` | TEXT | NOT NULL | 内层32字节随机根密钥（被外层混淆密钥AES加密后的Base64） |

> **设计意图**：`Sys_Secret` 是整个双层加密架构的关键桥梁。外层密钥K2永远不落盘（运行时从代码常量推导），内层密钥K1生成后立即用K2加密，密文存入本表。即使数据库文件泄露，攻击者也无法在没有程序二进制的情况下还原K1，从而无法解密任何业务数据。

### 4.3 表关系图

```
user_login ──────┐
(username PK)    │ 1:1
                 ▼
            user_profile
            (username PK)

user_login ──────┐
(username PK)    │ 1:1
                 ▼
            api_config
            (username PK)

user_login ──────┐
(username PK)    │ 1:N
                 ▼
            ai_character
            (id PK, username FK)

ai_character ────┐
(id PK)          │ 1:1
                 ▼
         session_settings
         (username + character_id 复合PK)

ai_character ────┐
(id PK)          │ 1:N
                 ▼
         chat_message
         (id PK, character_id FK)

ai_character ────┐
(id PK)          │ 1:N
                 ▼
         ai_memory
         (id PK, character_id FK)

Sys_Secret
(Id=1, 唯一行)   ← 存储全局根密钥K1密文，所有加密字段依赖此表
```

---

## 5. 实体类设计

所有实体类位于 `ChatAI/Data/Entities/` 命名空间，与数据库列一一对应。

### 5.1 UserLogin

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Username` | `string` | `user_login.username` | 用户名 |
| `Password` | `string` | `user_login.password` | 密码（加密后） |

### 5.2 UserProfile

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Username` | `string` | `user_profile.username` | 用户名 |
| `Nickname` | `string` | `user_profile.nickname` | 昵称 |
| `Gender` | `string` | `user_profile.gender` | 性别 |
| `Persona` | `string` | `user_profile.persona` | 人设设定 |
| `SystemPrompt` | `string` | `user_profile.system_prompt` | 系统提示词 |

### 5.3 ApiConfig

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Username` | `string` | `api_config.username` | 用户名 |
| `Provider` | `string` | `api_config.provider` | 服务商 |
| `BaseUrl` | `string` | `api_config.base_url` | 接口地址 |
| `ApiKey` | `string` | `api_config.api_key` | API密钥（加密） |
| `ModelName` | `string` | `api_config.model_name` | 模型名称 |
| `EnableLocalModel` | `bool` | `api_config.enable_local_model` | 是否本地模型 |
| `LocalModelPath` | `string` | `api_config.local_model_path` | 本地模型路径 |
| `GpuAccelMode` | `string` | `api_config.gpu_accel_mode` | GPU加速模式 |
| `MemoryLimitMB` | `int` | `api_config.memory_limit_mb` | 显存限制 |

### 5.4 SessionSettings

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Username` | `string` | `session_settings.username` | 用户名 |
| `CharacterId` | `int` | `session_settings.character_id` | 角色ID |
| `ContextLength` | `int` | `session_settings.context_length` | 上下文长度 |
| `MaxTokens` | `int` | `session_settings.max_tokens` | 最大tokens |
| `Temperature` | `double` | `session_settings.temperature` | 温度值 |
| `AutoMemoryCount` | `int` | `session_settings.auto_memory_count` | 记忆触发条数 |
| `MemoryPrompt` | `string` | `session_settings.memory_prompt` | 记忆提示词 |
| `NoRole` | `bool` | `session_settings.no_role` | 纯净模式 |

### 5.5 AiCharacter

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Id` | `int` | `ai_character.id` | 角色ID（自增） |
| `Username` | `string` | `ai_character.username` | 所属用户 |
| `Nickname` | `string` | `ai_character.nickname` | 角色昵称 |
| `Gender` | `string` | `ai_character.gender` | 角色性别 |
| `RoleDesc` | `string` | `ai_character.role_desc` | 人设设定 |
| `TalkStyle` | `string` | `ai_character.talk_style` | 表达风格 |
| `Habit` | `string` | `ai_character.habit` | 习惯用语 |
| `Opening` | `string` | `ai_character.opening` | 开场白 |

### 5.6 ChatMessage

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Id` | `int` | `chat_message.id` | 消息ID（自增） |
| `CharacterId` | `int` | `chat_message.character_id` | 所属角色ID |
| `IsUser` | `bool` | `chat_message.is_user` | 是否用户消息 |
| `Content` | `string` | `chat_message.content` | 消息内容（加密） |
| `SendTime` | `string` | `chat_message.send_time` | 发送时间 |

### 5.7 AiMemory

```
命名空间: ChatAI.Data.Entities
```

| 属性名 | 类型 | 对应列 | 说明 |
|--------|------|--------|------|
| `Id` | `int` | `ai_memory.id` | 记忆ID（自增） |
| `CharacterId` | `int` | `ai_memory.character_id` | 所属角色ID |
| `Content` | `string` | `ai_memory.content` | 记忆内容（加密） |
| `CreateTime` | `string` | `ai_memory.create_time` | 创建时间 |

### 5.8 SessionInfo（控件库DTO）

```
命名空间: ChatControl.Models
文件: ChatControl/Models/SessionInfo.cs
```

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `Id` | `int` | 角色ID |
| `UserAccount` | `string?` | 所属用户账号 |
| `AiName` | `string?` | 角色名称（会话列表显示名称） |
| `AiGender` | `string?` | 角色性别 |
| `AiPersona` | `string?` | 角色人设 |
| `AiStyle` | `string?` | 表达风格 |
| `AiHabit` | `string?` | 习惯用语 |
| `AiOpening` | `string?` | 开场白 |
| `CreateTime` | `DateTime` | 创建时间 |

> 注意：`SessionInfo` 不是数据库实体，而是在 `ChatControl` 控件库中使用的轻量级 DTO，由 `AiCharacter` 映射而来，用于避免控件库对数据层的直接依赖。字段名与 `AiCharacter` 不同（如 `Nickname` → `AiName`，`RoleDesc` → `AiPersona`，`TalkStyle` → `AiStyle`，`Opening` → `AiOpening`），在 `FrmChatMain` 加载数据时进行映射转换。

---

## 6. 数据访问层

### 6.1 SqliteDbHelper — 数据库初始化

```
文件: ChatAI/Data/SqliteDbHelper.cs
命名空间: ChatAI.Data
```

#### 职责

- SQLite 数据库文件的创建与初始化
- 7张表的建表 SQL 执行
- 数据库连接字符串的静态缓存与管理

#### 核心实现要点

```csharp
// 静态连接字符串缓存
private static string _connectionString;

// 数据库文件路径
// Data.db 位于应用程序根目录
// 连接字符串格式: "Data Source=Data.db;Version=3;"

// 初始化方法（伪代码）
public static void InitializeDatabase()
{
    // 1. 构建 SQLite 连接字符串
    // 2. 创建 SQLiteConnection 并打开
    // 3. 依次执行 7 条 CREATE TABLE IF NOT EXISTS 语句
    // 4. 关闭连接
    // 5. 缓存连接字符串到 _connectionString
}

// 获取连接方法
public static SQLiteConnection GetConnection()
{
    return new SQLiteConnection(_connectionString);
}
```

#### 建表SQL清单

按 `SqliteDbHelper` 中的执行顺序：
1. `user_login`
2. `user_profile`
3. `api_config`
4. `session_settings`
5. `ai_character`
6. `chat_message`
7. `ai_memory`
8. `Sys_Secret`（系统密钥表，双层加密核心）

### 6.2 EncryptionHelper — AES加密工具（双层安全架构）

```
文件: ChatAI/Data/EncryptionHelper.cs
命名空间: ChatAI.Data
```

#### 双层密钥架构概述

| 层级 | 名称 | 生成时机 | 存储位置 | 作用 |
|------|------|---------|---------|------|
| **外层密钥 K2** | 混淆外壳 SKey | 每次程序运行时动态计算 | 代码二进制碎片 + 运算逻辑（永不落盘） | 加密/解密内层根密钥 K1 |
| **内层密钥 K1** | 随机根密钥 RootKey | 首次创建数据库时生成一次 | `Sys_Secret.WrapRoot`（以K2加密后的密文） | 加密全部业务数据 |

```
【加密层次示意图】

业务明文（密码/消息/API Key等）
    │  K1 加密（AES-256-CBC，确定性IV）
    ▼
密文 → 存入各业务表

K1（32字节随机根密钥）
    │  K2 加密（AES-256-CBC，随机IV）
    ▼
密文 → 存入 Sys_Secret.WrapRoot

K2（外层混淆密钥）= 代码常量碎片 + 交叉穿插 + XOR扰动 + 位旋转 → 永远不存储
```

#### 外层密钥 K2 生成算法（GetRuntimeSkey）

K2 不以任何明文形式存在于代码或磁盘中，每次运行时从三段代码内嵌常量动态推导：

```csharp
private static readonly byte[] seg1 = { 0x17, 0x32, 0xAF, 0x7B, 0x29, 0xD1, 0x55, 0xC3 };
private static readonly byte[] seg2 = { 0xE2, 0x08, 0x4D, 0xBB, 0x77, 0x19, 0x6F, 0x24 };
private static readonly byte[] seg3 = { 0x9C, 0x41, 0x86, 0x30, 0xD8, 0x5A, 0x11, 0x70 };

public static byte[] GetRuntimeSkey()
{
    var pool = seg1.Concat(seg2).Concat(seg3).ToArray(); // 合并24字节

    byte[] temp = new byte[24];
    // 步骤1：跳位交叉合并（每3字节取1，扰乱原始顺序）
    for (int i = 0; i < pool.Length && idx < temp.Length; i += 3) temp[idx++] = pool[i];
    for (int i = 1; i < pool.Length && idx < temp.Length; i += 3) temp[idx++] = pool[i];

    // 步骤2：逐字节 XOR 扰动 + nibble 位旋转
    byte distortSalt = 0x5F;
    for (int i = 0; i < temp.Length; i++) {
        temp[i] = (byte)(temp[i] ^ distortSalt ^ (byte)i);  // 按位置异或
        byte low = (byte)(temp[i] & 0x0F);
        byte high = (byte)(temp[i] & 0xF0);
        temp[i] = (byte)(high | ((low >> 1) | (low << 3)));  // 低4位循环右移
    }

    // 步骤3：拉伸至 AES-256 所需的 32 字节
    _cachedRuntimeSkey = new byte[32];
    for (int i = 0; i < 32; i++) {
        _cachedRuntimeSkey[i] = temp[i % temp.Length];
        _cachedRuntimeSkey[i] ^= (byte)(0x2D + i);          // 最终 XOR 叠加
    }
    return _cachedRuntimeSkey;  // 结果固定，每次运行相同
}
```

#### 内层密钥 K1 初始化（InitializeRootKey）

```csharp
public static void InitializeRootKey(bool isNewDatabase)
{
    if (isNewDatabase) {
        // 新建数据库：密码学安全随机生成 K1
        _rootAesKey = GenerateSecureRandom32ByteKey();  // RandomNumberGenerator.Create()
        // 用 K2 包裹 K1，存入 Sys_Secret
        string wrapped = WrapRootKey(_rootAesKey);      // AES-CBC(K2, randomIV, K1)
        SysSecretRepository.SaveWrappedRootKey(wrapped);
    } else {
        // 已有数据库：从 Sys_Secret 读取密文，用 K2 解包还原 K1
        string wrappedRoot = SysSecretRepository.GetWrappedRootKey();
        _rootAesKey = UnwrapRootKey(wrappedRoot);       // AES-CBC-decrypt(K2, K1)
    }
}
```

#### 业务数据加密（Encrypt / Decrypt）

业务数据统一使用 K1 加密。**IV 采用确定性设计**（由明文SHA-256哈希的前16字节派生），而非随机生成：

```csharp
public static string Encrypt(string plainText)
{
    using var aes = Aes.Create();
    aes.Key = _rootAesKey;          // K1：32字节随机根密钥
    aes.Mode = CipherMode.CBC;

    // 确定性IV：SHA256(plainText)[0..15]
    // 相同明文 → 相同密文（支持 SQL 等值查询）
    byte[] iv = GenerateDeterministicIV(plainText);
    aes.IV = iv;

    // 输出格式：Base64(IV[16字节] + 密文)
    using var encryptor = aes.CreateEncryptor();
    byte[] cipherBytes = encryptor.TransformFinalBlock(...);
    return Convert.ToBase64String(iv.Concat(cipherBytes).ToArray());
}

private static byte[] GenerateDeterministicIV(string plainText)
{
    using var sha256 = SHA256.Create();
    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainText));
    byte[] iv = new byte[16];
    Array.Copy(hash, 0, iv, 0, 16);  // 取哈希值前16字节
    return iv;
}
```

> **确定性IV的权衡设计**：牺牲了单条密文的语义安全性（相同明文产生相同密文），换取了一项关键能力：**消息回溯功能中可用 `WHERE content = @encryptedValue` 直接在数据库层定位消息**，无需先全量解密再过滤。这是一个经过权衡的有意设计，适合本项目场景。

#### 加密参数汇总

| 参数 | K2（外层） | K1（内层业务加密） |
|------|-----------|-----------------|
| 算法 | AES-256-CBC | AES-256-CBC |
| 密钥长度 | 32 bytes（代码推导） | 32 bytes（密码学随机） |
| IV 来源 | 随机生成（每次不同） | 确定性（SHA256 派生） |
| 存储 | 永不存储 | 以K2加密后存 Sys_Secret |
| 作用 | 包裹/解包 K1 | 加密所有业务数据 |

#### 加密字段清单

| 表名 | 加密字段 | 说明 |
|------|---------|------|
| `user_login` | `password` | 用户密码 |
| `user_profile` | `nickname`、`persona`、`system_prompt` | 用户昵称、人设、系统提示词 |
| `api_config` | `api_key` | API密钥 |
| `session_settings` | `memory_prompt` | 记忆生成提示词 |
| `ai_character` | `ai_persona`、`ai_style`、`ai_habit`、`ai_opening` | 角色设定四字段 |
| `chat_message` | `content` | 每条聊天消息内容 |
| `ai_memory` | `memory_content` | 每条长期记忆 |
| `Sys_Secret` | `WrapRoot` | K1本身（以K2加密后的密文）|

#### SysSecretRepository — 系统密钥仓储

```
文件: ChatAI/Data/SysSecretRepository.cs
命名空间: ChatAI.Data
```

| 方法 | 功能 |
|------|------|
| `GetWrappedRootKey()` | 从 `Sys_Secret` 表读取 K1 的包裹密文 |
| `SaveWrappedRootKey(string wrappedRoot)` | 将 K1 的包裹密文写入 `Sys_Secret` 表（INSERT OR REPLACE） |

### 6.3 UserRepository — 数据仓库（God Class）

```
文件: ChatAI/Data/UserRepository.cs
命名空间: ChatAI.Data
规模: 1400+ 行
```

> **注意**: 这是当前代码库中最大的单个类，承担了所有数据访问职责。理想情况下应拆分为多个 Repository（用户、会话、消息、记忆等）。

#### 主要方法分类

##### 用户认证相关

| 方法签名 | 功能 |
|----------|------|
| `bool RegisterUser(string username, string password)` | 注册新用户（密码AES加密后存储） |
| `bool LoginUser(string username, string password)` | 验证登录（加密输入密码后比对） |
| `bool ChangePassword(string username, string oldPassword, string newPassword)` | 修改密码 |

##### 用户资料相关

| 方法签名 | 功能 |
|----------|------|
| `UserProfile GetUserProfile(string username)` | 获取用户资料 |
| `bool SaveUserProfile(UserProfile profile)` | 保存/更新用户资料（所有字段AES加密） |

##### API配置相关

| 方法签名 | 功能 |
|----------|------|
| `ApiConfig GetApiConfig(string username)` | 获取API配置 |
| `bool SaveApiConfig(ApiConfig config)` | 保存API配置（API Key等加密） |

##### AI角色相关

| 方法签名 | 功能 |
|----------|------|
| `List<AiCharacter> GetAiCharacters(string username)` | 获取用户所有AI角色 |
| `AiCharacter GetAiCharacterById(int characterId)` | 根据ID获取角色 |
| `int AddAiCharacter(AiCharacter character)` | 添加新角色，返回自增ID |
| `bool UpdateAiCharacter(AiCharacter character)` | 更新角色信息 |
| `bool DeleteAiCharacter(int characterId)` | 删除角色 |
| `bool DeleteCharacterData(int characterId)` | 级联删除角色的消息和记忆 |

##### 会话设置相关

| 方法签名 | 功能 |
|----------|------|
| `SessionSettings GetSessionSettings(string username, int characterId)` | 获取会话设置 |
| `bool SaveSessionSettings(SessionSettings settings)` | 保存会话设置 |

##### 聊天消息相关

| 方法签名 | 功能 |
|----------|------|
| `void SaveChatMessage(ChatMessage message)` | 保存单条消息（内容AES加密） |
| `List<ChatMessage> LoadChatMessages(int characterId)` | 加载角色全部聊天消息（解密） |
| `int GetMessageCount(int characterId)` | 获取消息总数 |
| `void DeleteMessagesAfter(int characterId, int messageId)` | 删除指定消息ID之后的所有消息 |

##### AI记忆相关

| 方法签名 | 功能 |
|----------|------|
| `void SaveAiMemory(AiMemory memory)` | 保存记忆（内容AES加密） |
| `List<AiMemory> GetMemoriesByCharacterId(int characterId)` | 获取角色全部记忆（解密） |
| `void DeleteMemory(int memoryId)` | 删除单条记忆 |
| `List<AiMemory> GetMemoriesWithIds(List<int> memoryIds)` | 根据ID列表批量获取记忆 |
| `void DeleteMemoriesAfterMessage(int characterId, int messageId)` | 删除指定消息之后的记忆 |
| `int GetLatestMessageId(int characterId)` | 获取最新消息ID |

---

## 7. 服务层

### 7.1 TextGenerator — AI文本生成核心服务

```
文件: ChatAI/Services/TextGenerator.cs
命名空间: ChatAI.Services
规模: ~67KB（最大最复杂的单个文件）
```

#### 职责

- 云端 API 调用（OpenAI Chat Completions 兼容协议）
- 本地模型推理调度
- 记忆自动生成
- 请求 JSON 调试输出

#### 核心方法

##### SendRequestAndGetResponse — 云端API调用

```csharp
public async Task<string> SendRequestAndGetResponse(
    List<ChatMessage> messages,       // 历史消息列表
    SessionSettings settings,         // 会话设置（含temperature等）
    string systemPrompt,              // 系统提示词
    string currentUser,               // 当前用户名
    int characterId                   // 当前角色ID
)
```

**处理流程**:
1. 从 `UserRepository` 获取 `ApiConfig`
2. 检查 `EnableLocalModel`：
   - `true` → 调用本地模型服务（见下方字段复用说明）
   - `false` → 调用云端 API
3. **云端API路径**：
   - 创建 `HttpClient`（注意：每次调用都 new，应改为静态实例）
   - 构建 JSON 请求体：
     ```json
     {
       "model": "<ModelName>",
       "messages": [
         {"role": "system", "content": "<systemPrompt>"},
         {"role": "user", "content": "..."},
         {"role": "assistant", "content": "..."}
       ],
       "temperature": <Temperature>,
       "max_tokens": <MaxTokens>
     }
     ```
   - POST 到 `{BaseUrl}/chat/completions`
   - 设置 `Authorization: Bearer {ApiKey}`
   - 解析响应 JSON，提取 `choices[0].message.content`
4. **本地模型路径**：
   - 通过 `LocalModelServiceSingleton` 获取单例
   - 使用 `ModelFormatFactory.Create(modelName)` 获取对应格式策略
   - 调用 `GenerateResponseAsync(prompt, maxTokens, temperature, timeout, stopWords)` 获取回复

##### ApiConfig 字段复用（云端 vs 本地）

**重要**：启用本地模型时，`ApiConfig` 的多个字段被**复用**存储本地模型参数，而非其字面含义：

| `ApiConfig` 字段 | 云端API含义 | 本地模型含义 |
|-----------------|------------|------------|
| `Endpoint`（`base_url`） | API 服务器基础URL（如 `https://api.siliconflow.cn/v1`） | `.gguf` 模型文件的**完整绝对路径** |
| `ApiKey`（`api_key`） | Bearer 授权令牌（AES加密存储） | GPU 加速模式字符串（如 `"CUDA 12"`、`"Vulkan"`） |
| `ModelName`（`model_name`） | 模型 ID（如 `deepseek-ai/DeepSeek-V3`） | `.gguf` 文件名（仅文件名，不含路径） |
| `LocalModelMemoryLimit`（`memory_limit_mb`） | 不使用 | VRAM / 内存预算（MB） |

##### GenerateMemoryWithQwenFormatAsync — 记忆生成

```csharp
public async Task<string> GenerateMemoryWithQwenFormatAsync(
    string conversationSummary,       // 对话摘要
    string memoryPrompt,              // 记忆生成提示词
    SessionSettings settings          // 会话设置
)
```

**处理流程**:
1. 构建记忆生成专用消息列表（使用 Qwen 格式或标准格式）
2. 系统消息中注入 `memoryPrompt` 作为生成指导
3. 用户消息包含 `conversationSummary`
4. 调用 AI 生成记忆文本
5. 返回生成的记忆字符串

##### GenerateAndSaveJsonRequest — 调试用

```csharp
public void GenerateAndSaveJsonRequest(
    List<ChatMessage> messages,
    SessionSettings settings,
    string systemPrompt
)
```

将构建好的请求 JSON 保存到文件，用于调试和问题排查。

#### LocalModelServiceSingleton — 本地模型单例

```csharp
private static LocalModelService _localModelService;
private static readonly object _lock = new object();

public static LocalModelService LocalModelServiceSingleton
{
    get
    {
        if (_localModelService == null)      // 第一次检查
        {
            lock (_lock)                     // 加锁
            {
                if (_localModelService == null) // 第二次检查
                {
                    _localModelService = new LocalModelService();
                }
            }
        }
        return _localModelService;
    }
}
```

**双重检查锁模式**，确保本地模型只加载一次，多线程安全。

### 7.2 LocalModelService — 本地模型推理服务

```
文件: ChatAI/Services/LocalModelService.cs
命名空间: ChatAI.Services
依赖: LLamaSharp（llama.cpp 的 .NET 原生绑定）
```

#### 架构特点

本服务通过 **LLamaSharp** 在进程内直接加载 `.gguf` 格式模型权重，执行推理，**不依赖 Ollama 或 LM Studio 等外部进程**，所有推理在应用内存空间完成。

#### GPU 硬件检测

采用 WMI + 注册表双通道检测，结果 5 分钟缓存：

```csharp
public static string DetectGpuType()
{
    // 方式1：WMI 查询 Win32_VideoController
    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
    foreach (var obj in searcher.Get())
    {
        string adapterName = obj["Name"]?.ToString() ?? "";
        if (adapterName.Contains("NVIDIA", OrdinalIgnoreCase)) return "NVIDIA";  // → CUDA
        if (adapterName.Contains("AMD", OrdinalIgnoreCase) ||
            adapterName.Contains("Radeon", OrdinalIgnoreCase)) return "AMD";     // → Vulkan
    }
    // 方式2：环境变量兜底
    // NVIDIA_VISIBLE_DEVICES / AMD_VISIBLE_DEVICES
}
```

VRAM 检测采用异步带超时（2秒），估算可用显存为总显存的 85%：

```csharp
public static async Task<long> GetGpuMemoryMBAsync()
{
    var task = Task.Run(() => {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
        // 读取 AdapterRAM，转换为 MB
    });
    // 超时2秒则返回0，让用户手动指定限制
    bool completed = await Task.WhenAny(task, Task.Delay(2000)) == task;
    long totalVram = completed ? task.Result : 0;
    return (long)(totalVram * 0.85);  // 保留15%系统预留
}
```

在 `FrmApiConfig` 配置界面中，VRAM 检测改用 **注册表直接读取**（比 WMI 更快）：

```csharp
// NVIDIA: HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Device0\HardwareInformation.QwordMemorySize
// AMD:    HKLM\SYSTEM\CurrentControlSet\Services\amdkmdag\Device0\HardwareInformation.QwordMemorySize
```

#### 模型初始化（Initialize）

```csharp
public bool Initialize(string modelPath, string gpuMode, int memoryLimitMB)
{
    // AMD Vulkan 设置环境变量
    if (gpuMode == "Vulkan") {
        Environment.SetEnvironmentVariable("LLAMA_BACKEND", "Vulkan");
        Environment.SetEnvironmentVariable("LLAMA_VULKAN", "1");
    }

    // 根据模型类型决定上下文窗口大小
    // Llama-3 / Hermes 系列：4096
    // 其他（Qwen 等）：8192
    int contextSize = modelName.Contains("llama-3") || modelName.Contains("hermes") ? 4096 : 8192;

    // 计算 GPU 分层数量
    int gpuLayers = CalculateGpuLayers(gpuMode, memoryLimitMB, modelName, detectedVram);

    var parameters = new ModelParams(modelPath) {
        ContextSize = (uint)contextSize,
        GpuLayerCount = gpuLayers,       // 0 = 纯CPU推理
    };

    _model   = LLamaWeights.LoadFromFile(parameters);  // 加载模型权重
    _context = _model.CreateContext(parameters);
    _executor = new InteractiveExecutor(_context);
    _isInitialized = true;
}
```

#### GPU 分层计算（CalculateGpuLayers）

每个 Transformer 层的 VRAM 消耗因模型规格不同而异，本服务按以下规则估算：

```csharp
private int CalculateGpuLayers(string gpuMode, int memoryLimitMB, string modelName, long? preDetectedVram)
{
    if (gpuMode == "CPU 模式" || gpuMode == "CPU") return 0;  // 纯CPU不卸载任何层

    // 每层估算 VRAM 消耗
    // Llama-3 / Hermes（参数量较大）：~130 MB/层
    // 其他模型（Qwen 7B 等）：~100 MB/层
    int memoryPerLayer = modelName.Contains("llama-3") || modelName.Contains("hermes") ? 130 : 100;

    long usableMemory = Math.Min(memoryLimitMB, preDetectedVram ?? memoryLimitMB);
    int layers = (int)(usableMemory / memoryPerLayer);

    // 按参数量限制最大层数
    // 7B 模型 → 最多 32 层
    // 13B 模型 → 最多 40 层
    int maxLayers = GetMaxLayersForModel(modelName);
    return Math.Min(layers, maxLayers);
}
```

| GPU模式 | 效果 | 适用场景 |
|--------|------|---------|
| `自动选择（推荐）` | 检测 GPU 类型后自动决定 CUDA/Vulkan | 不确定硬件时使用 |
| `CUDA 12` | NVIDIA GPU 硬件加速 | NVIDIA 显卡 |
| `Vulkan` | AMD/Intel GPU 硬件加速 | AMD 显卡 |
| `CPU 模式` | 纯 CPU 推理，gpuLayers=0 | 无独立显卡 / 显存不足 |

#### 流式推理（GenerateResponseAsync）

```csharp
public async Task<string?> GenerateResponseAsync(
    string prompt, int maxTokens, float temperature,
    TimeSpan timeout, string[]? stopWords = null, int attempt = 1)
{
    // 每次推理前重建 executor，清空 KV Cache，避免历史上下文污染
    currentExecutor = new InteractiveExecutor(_context);

    var inferenceParams = new InferenceParams {
        MaxTokens   = maxTokens,
        AntiPrompts = effectiveStopWords  // 停词触发截断
    };

    string response = "";
    string lastChunk = "";
    int consecutiveRepeatCount = 0;

    await foreach (var text in currentExecutor.InferAsync(prompt, inferenceParams)
                                              .WithCancellation(cts.Token))
    {
        // 连续5个相同块 → 检测到重复循环，提前终止
        if (text == lastChunk && ++consecutiveRepeatCount >= 5) break;
        consecutiveRepeatCount = (text == lastChunk) ? consecutiveRepeatCount : 0;
        lastChunk = text;

        response += text;

        // 停词检查：发现停词后截断并退出
        foreach (var stopWord in effectiveStopWords)
            if (response.Contains(stopWord)) {
                response = response[..response.IndexOf(stopWord)];
                goto endGeneration;
            }
    }
    endGeneration:

    // 推理完成后立即释放 VRAM（保守策略）
    Dispose();
    return response.Trim();
}
```

> **VRAM 管理策略**：每次推理结束后调用 `Dispose()` 释放模型权重，下次推理时重新 `Initialize()`。这是保守的内存管理策略——优点是 VRAM 占用最小，缺点是连续对话时首次加载会有延迟（通常 3-10 秒，取决于模型大小和硬件）。

#### 解码失败自动恢复

```csharp
catch (LLama.Exceptions.LLamaDecodeError ex) {
    // 解码失败时强制重置，下次调用时重新初始化
    Dispose();
    _isInitialized = false;
    throw new InvalidOperationException("本地模型解码失败，需要重新初始化", ex);
}
```

在 `TextGenerator` 中，这个异常会被捕获并自动重试（最多2次）：

```csharp
private static async Task<string?> CallLocalModelWithRetry(...)
{
    for (int attempt = 1; attempt <= 2; attempt++) {
        try {
            return await LocalModelServiceSingleton.Instance.GenerateResponseAsync(...);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("连接已断开") || ex.Message.Contains("需要重新初始化")) {
            // 重新初始化后继续重试
            bool success = LocalModelServiceSingleton.Instance.Initialize(modelPath, gpuMode, memoryLimit);
            if (success) continue;
        }
    }
    return null;
}
```

### 7.3 模型格式策略模式

```
目录: ChatAI/Services/ModelFormats/
```

本地模型推理需要按模型系列要求构建特定格式的 Prompt 字符串。采用策略模式（Strategy Pattern）实现格式的可替换。

#### IModelFormat — 接口定义

```csharp
public interface IModelFormat
{
    /// <summary>构建完整的聊天 Prompt 字符串（含历史对话）</summary>
    string BuildChatPrompt(string systemContent, List<ChatMessage> conversationHistory, string userMessage);

    /// <summary>构建记忆提取专用 Prompt</summary>
    string BuildMemoryPrompt(string systemContent, List<ChatMessage> conversationHistory);

    /// <summary>获取该模型系列的停词列表</summary>
    string[] GetStopWords();

    /// <summary>清理模型输出中的格式残留（停词标记等）</summary>
    string CleanResponse(string response);
}
```

#### QwenModelFormat — 通义千问 ChatML 格式

适用于 `Qwen-7B-Chat`、`Qwen2` 等 ChatML 格式模型：

```csharp
// 格式样本：
// <|im_start|>system
// {systemContent}<|im_end|>
// <|im_start|>user
// {userMessage}<|im_end|>
// <|im_start|>assistant
// {aiReply}<|im_end|>
// ... (历史对话循环)
// <|im_start|>assistant\n  ← 模型从此处开始生成

private const string ImStart = "<|im_start|>";
private const string ImEnd   = "<|im_end|>";

public string[] GetStopWords() => new[] { "<|im_end|>", "<|im_start|>" };
```

#### HermesModelFormat — Hermes / Llama-3 格式

适用于 `Hermes-2-Pro-Llama-3-8B` 等模型：

```csharp
// 格式样本：
// <|begin_of_text|><|start_header_id|>system<|end_header_id|>
//
// {systemContent}
//
// <|eot_id|><|start_header_id|>user<|end_header_id|>
//
// {userMessage}
//
// <|eot_id|><|start_header_id|>assistant<|end_header_id|>
//
// {aiReply}
//
// <|eot_id|>
// ... (循环)
// <|start_header_id|>assistant<|end_header_id|>\n\n  ← 模型从此处开始生成

// ⚠ 注意：停词只用 <|eot_id|> 和 <|end_of_text|>
//   不添加其他停词，否则会导致空回复
public string[] GetStopWords() => new[] { "<|eot_id|>", "<|end_of_text|>" };
```

#### ModelFormatFactory — 工厂

```csharp
public static class ModelFormatFactory
{
    private static readonly Dictionary<string, Type> _exactMatches = new()
    {
        { "Qwen",                              typeof(QwenModelFormat) },
        { "Qwen-7B-Chat-Q4_K_M",              typeof(QwenModelFormat) },
        { "Hermes",                            typeof(HermesModelFormat) },
        { "Hermes-2-Pro-Llama-3-8B.Q4_K_M.gguf", typeof(HermesModelFormat) },
    };

    public static IModelFormat Create(string modelName)
    {
        // 精确匹配
        if (_exactMatches.TryGetValue(modelName, out Type? type))
            return (IModelFormat)Activator.CreateInstance(type)!;

        // 模糊匹配：文件名包含关键词
        if (modelName.Contains("Hermes",     StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("llama-3",    StringComparison.OrdinalIgnoreCase))
            return new HermesModelFormat();

        if (modelName.Contains("Qwen",       StringComparison.OrdinalIgnoreCase))
            return new QwenModelFormat();

        // 默认回退：使用 Qwen ChatML 格式
        return new QwenModelFormat();
    }
}
```

---

## 8. UI表现层 — 窗体

### 8.1 FrmLogin — 登录窗体

```
文件: ChatAI/UI/Forms/FrmLogin.cs + FrmLogin.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "登录界面"
尺寸: 344 × 306
BorderStyle: FixedSingle (不可调整大小)
StartPosition: CenterScreen
```

#### 依赖注入模式

```csharp
// 使用接口解耦
private readonly IUserRepository _userRepository;

// 构造函数注入（默认使用 SqliteUserRepository）
public FrmLogin(IUserRepository userRepository = null)
{
    _userRepository = userRepository ?? new SqliteUserRepository();
    InitializeComponent();
    SetupRememberPassword();
}
```

#### 控件清单

| 控件变量名 | 类型 | 属性 | 说明 |
|-----------|------|------|------|
| `tabControlMain` | `TabControl` | 3个TabPage | 主选项卡容器 |
| `tabPageLogin` | `TabPage` | Text=" 登录" | 登录页 |
| `tabPageRegister` | `TabPage` | Text=" 注册" | 注册页 |
| `tabPageModifyPwd` | `TabPage` | Text="修改密码" | 改密页 |

**登录页控件** (`tabPageLogin`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `txtUsername` | `TextBox` | Size=225×23 | 用户名输入框 |
| `txtPassword` | `TextBox` | PasswordChar='*' | 密码输入框 |
| `chkRememberPwd` | `CheckBox` | Text="记住密码" | 记住密码勾选 |
| `btnLogin` | `Button` | Text="登录", Size=225×35 | 登录按钮 |

**注册页控件** (`tabPageRegister`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `txtRegUsername` | `TextBox` | Size=221×23 | 注册用户名 |
| `txtRegPassword` | `TextBox` | PasswordChar='*' | 注册密码 |
| `txtRegConfirmPwd` | `TextBox` | PasswordChar='*' | 确认密码 |
| `btnRegister` | `Button` | Text="注册", Size=221×36 | 注册按钮 |

**修改密码页控件** (`tabPageModifyPwd`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `txtModifyUsername` | `TextBox` | Size=212×23 | 用户名 |
| `txtOldPwd` | `TextBox` | PasswordChar='*' | 旧密码 |
| `txtNewPwd` | `TextBox` | PasswordChar='*' | 新密码 |
| `txtConfirmNewPwd` | `TextBox` | PasswordChar='*' | 确认新密码 |
| `btnModifyPwd` | `Button` | Text="确认修改", Size=212×35 | 确认修改按钮 |

#### 核心逻辑

```csharp
// 登录成功时
public string LoggedInUsername { get; private set; }

private void btnLogin_Click(object sender, EventArgs e)
{
    // 1. 验证输入非空
    // 2. _userRepository.LoginUser(username, password)
    // 3. 登录成功：
    //    - LoggedInUsername = username
    //    - 如 chkRememberPwd.Checked → 保存到本地
    //    - this.DialogResult = DialogResult.OK
    //    - this.Close()
}

private void btnRegister_Click(object sender, EventArgs e)
{
    // 1. 验证输入非空
    // 2. 验证两次密码一致
    // 3. _userRepository.RegisterUser(username, password)
    // 4. 注册成功 → 切换到登录页并自动填入用户名
}

private void btnModifyPwd_Click(object sender, EventArgs e)
{
    // 1. 验证旧密码正确
    // 2. 验证新密码非空
    // 3. 验证两次新密码一致
    // 4. _userRepository.ChangePassword(username, oldPwd, newPwd)
}

// 记住密码功能
private void SetupRememberPassword()
{
    // 从本地配置读取已保存的用户名和密码
    // 自动填充到 txtUsername 和 txtPassword
    // 勾选 chkRememberPwd
}
```

### 8.2 FrmChatMain — 主聊天窗体

```
文件: ChatAI/UI/Forms/FrmChatMain.cs + FrmChatMain.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "FrmChatMain"
尺寸: 870 × 604
背景色: Gainsboro
字体: 微软雅黑 9pt
```

#### 控件清单

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `ms_MainMenuBar` | `MenuStrip` | Dock=Left, VerticalStackWithOverflow, Font=微软雅黑12pt | 左侧竖向菜单栏 |
| `tsmi_MainMenu_APIConfig` | `ToolStripMenuItem` | Text="🔧", ToolTipText="API设置" | API配置入口 |
| `tsmi_MainMenu_SessionSetting` | `ToolStripMenuItem` | Text="🗨️", ToolTipText="会话设置" | 会话设置入口 |
| `tsmi_UserIdentitySetting` | `ToolStripMenuItem` | Text="👤", ToolTipText="用户信息设置" | 用户资料入口 |
| `tsmi_AiMemorySettings` | `ToolStripMenuItem` | Text="💭", ToolTipText="记忆设置" | 记忆管理入口 |
| `tsmi_MainMenu_Logout` | `ToolStripMenuItem` | Text="❌", ToolTipText="注销" | 注销按钮 |
| `splitContainer1` | `SplitContainer` | Dock=Fill, FixedPanel=Panel1, SplitterDistance=170 | 左右分割容器 |
| `sessionListuc1` | `SessionListUC` | Dock=Fill | 左侧会话列表（自定义控件） |
| `chatMainControl1` | `ChatMainControl` | Dock=Fill | 右侧聊天区域（自定义控件） |
| `toolMenuInfo` | `ToolTip` | — | 菜单项悬停提示 |

#### 窗体布局

```
┌──┬──────────────┬────────────────────────────┐
│🔧│              │                            │
│🗨│  sessionList  │     chatMainControl        │
│👤│    uc1       │        (GDI+自绘气泡)       │
│💭│              │                            │
│❌│              │                            │
│  │  (170px)    │                            │
│  │              │                            │
│35│              │                            │
│px│              │                            │
└──┴──────────────┴────────────────────────────┘
  MenuStrip     SplitterDistance=170      Dock=Fill
```

#### 核心字段

```csharp
// 用户状态
private string _currentUsername;           // 当前登录用户名
private int _currentCharacterId = -1;     // 当前选中角色ID（-1=无选中）

// 记忆系统
private readonly Queue<(int characterId, int messageCount)> _pendingMemoryRequests;
private readonly object _memoryLock = new object();
private bool _isMemoryGenerating = false;  // 记忆生成中标记

// 服务实例
private readonly UserRepository _userRepository;
private readonly TextGenerator _textGenerator;

// 注销标记
public bool IsLogout { get; private set; }
```

#### 事件订阅

```csharp
// 订阅会话列表事件
sessionListuc1.SessionSelected += SessionListUC_SessionSelected;
sessionListuc1.OnCreateSession += SessionListUC_OnCreateSession;
sessionListuc1.OnDeleteSession += SessionListUC_OnDeleteSession;
sessionListuc1.OnEditSession += SessionListUC_OnEditSession;
sessionListuc1.OnViewMemory += SessionListUC_OnViewMemory;

// 订阅聊天控件事件
chatMainControl1.MessageSent += ChatMainControl_MessageSent;
chatMainControl1.AiResponseReceived += ChatMainControl_AiResponseReceived;
chatMainControl1.RegenerateResponseRequested += ChatMainControl_RegenerateResponseRequested;
chatMainControl1.MessageRollbackRequested += ChatMainControl_MessageRollbackRequested;
chatMainControl1.MessageUpdated += ChatMainControl_MessageUpdated;
```

#### 核心流程

##### 发送消息流程

```csharp
private async void ChatMainControl_MessageSent(object sender, string userMessage)
{
    // 1. 检查是否有选中会话
    if (_currentCharacterId == -1)
    {
        HandleNoSessionSelected();  // 自动创建默认会话
        return;
    }

    // 2. 异步处理
    await SendUserMessageToAI(userMessage);
}

private async Task SendUserMessageToAI(string userMessage)
{
    // 1. 保存用户消息到数据库
    //    UserRepository.SaveChatMessage(new ChatMessage {
    //        CharacterId = _currentCharacterId,
    //        IsUser = true,
    //        Content = EncryptionHelper.Encrypt(userMessage),
    //        SendTime = DateTime.Now.ToString(...)
    //    });

    // 2. 加载历史消息
    //    var messages = UserRepository.LoadChatMessages(_currentCharacterId);

    // 3. 获取会话设置和角色信息
    //    var settings = UserRepository.GetSessionSettings(username, characterId);
    //    var character = UserRepository.GetAiCharacterById(characterId);

    // 4. 构建系统提示词
    //    如果 settings.NoRole == false：
    //      systemPrompt = 用户人设 + 角色设定 + AI记忆注入
    //    否则：
    //      systemPrompt = null（纯净模式）

    // 5. 调用 AI 生成回复
    //    var response = await _textGenerator.SendRequestAndGetResponse(
    //        messages, settings, systemPrompt, _currentUsername, _currentCharacterId);

    // 6. 保存 AI 回复到数据库
    //    UserRepository.SaveChatMessage(new ChatMessage {
    //        CharacterId = _currentCharacterId,
    //        IsUser = false,
    //        Content = EncryptionHelper.Encrypt(response),
    //        SendTime = DateTime.Now.ToString(...)
    //    });

    // 7. 在 ChatMainControl 上添加AI回复气泡
    //    chatMainControl1.AddAiMessage(response);

    // 8. 检查是否触发自动记忆
    //    var newTotalCount = UserRepository.GetMessageCount(_currentCharacterId);
    //    CheckAndGenerateMemory(newTotalCount);
}
```

##### 自动记忆生成流程

```csharp
private void CheckAndGenerateMemory(int newTotalCount)
{
    // 1. 获取会话设置
    var settings = UserRepository.GetSessionSettings(username, characterId);

    // 2. 判断是否满足触发条件
    int memoryTrigger = settings.AutoMemoryCount; // 默认20
    if (newTotalCount % memoryTrigger == 0 && newTotalCount > 0)
    {
        // 3. 加入待处理队列
        lock (_memoryLock)
        {
            _pendingMemoryRequests.Enqueue((characterId, newTotalCount));
        }

        // 4. 尝试开始处理队列
        ProcessMemoryQueueAsync();
    }
}

private async Task ProcessMemoryQueueAsync()
{
    lock (_memoryLock)
    {
        if (_isMemoryGenerating) return;  // 已有记忆生成任务进行中
        _isMemoryGenerating = true;
    }

    try
    {
        while (true)
        {
            (int characterId, int messageCount) request;
            lock (_memoryLock)
            {
                if (_pendingMemoryRequests.Count == 0) break;
                request = _pendingMemoryRequests.Dequeue();
            }

            // 1. 获取角色信息和历史消息
            // 2. 构建对话摘要（取最近 N 条消息）
            // 3. 调用 TextGenerator.GenerateMemoryWithQwenFormatAsync()
            // 4. 保存生成的记忆到数据库
            //    UserRepository.SaveAiMemory(new AiMemory {
            //        CharacterId = characterId,
            //        Content = EncryptionHelper.Encrypt(memoryText),
            //        CreateTime = DateTime.Now.ToString(...)
            //    });
        }
    }
    finally
    {
        lock (_memoryLock)
        {
            _isMemoryGenerating = false;
        }
    }
}
```

##### 无会话选中处理

```csharp
private void HandleNoSessionSelected()
{
    // 自动创建一个默认AI角色 "AI聊天助手"
    var defaultCharacter = new AiCharacter
    {
        Username = _currentUsername,
        Nickname = "AI聊天助手",
        Gender = "男",
        RoleDesc = "",
        TalkStyle = "",
        Habit = "",
        Opening = "你好！有什么可以帮助你的吗？"
    };

    int characterId = _userRepository.AddAiCharacter(defaultCharacter);

    // 刷新会话列表
    RefreshSessionList();

    // 自动选中该会话
    // 显示开场白
    chatMainControl1.AddAiMessage(defaultCharacter.Opening);
}
```

##### 消息回溯流程

```csharp
private void ChatMainControl_MessageRollbackRequested(object sender, int messageId)
{
    // 1. 删除指定消息ID之后的所有消息
    _userRepository.DeleteMessagesAfter(_currentCharacterId, messageId);

    // 2. 级联删除相关记忆
    _userRepository.DeleteMemoriesAfterMessage(_currentCharacterId, messageId);

    // 3. 重新加载消息列表并刷新UI
    ReloadAndRefreshMessages();
}
```

##### 重新生成回复流程

```csharp
private async void ChatMainControl_RegenerateResponseRequested(object sender, EventArgs e)
{
    // 1. 获取最后一条用户消息
    // 2. 删除最后一条AI回复
    // 3. 重新调用 AI 生成回复
    // 4. 保存新回复
    // 5. 更新UI
}
```

##### 注销流程

```csharp
private void tsmi_MainMenu_Logout_Click(object sender, EventArgs e)
{
    IsLogout = true;
    this.Close();  // 触发 FormClosing → Program 中的循环继续
}

private void FrmChatMain_FormClosing(object sender, FormClosingEventArgs e)
{
    // 如果不是注销操作，设置退出标记
    if (!IsLogout)
    {
        Program.IsExitApplication = true;
    }
}
```

### 8.3 FrmApiConfig — API配置窗体

```
文件: ChatAI/UI/Forms/FrmApiConfig.cs + FrmApiConfig.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "API配置窗口"
尺寸: 678 × 415
BorderStyle: FixedDialog
StartPosition: CenterParent
```

#### 控件清单

**云端API配置区域** (`groupBox_CloudApiConfig`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `cboProvider` | `ComboBox` | Size=230×25 | 服务商下拉选择 |
| `cboModel` | `ComboBox` | Size=265×25 | 模型选择 |
| `txtBaseUrl` | `TextBox` | Size=522×23 | 接口地址 |
| `txtApiKey` | `TextBox` | PasswordChar='*', Size=522×23 | API密钥（密文显示） |
| `btnLoadModels` | `Button` | Text="获取模型列表" | 从服务商拉取可用模型 |
| `btnTestConnect` | `Button` | Text="测试连接" | 测试API连通性 |
| `btnShowApiKey` | `Button` | Text="显示" | 切换API Key显示/隐藏 |
| `lblProvider` | `Label` | Text="接口服务商：" | — |
| `lblModel` | `Label` | Text="模型选择：" | — |
| `lblBaseUrl` | `Label` | Text="接口地址：" | — |
| `lblApiKey` | `Label` | Text="API Key：" | — |

**本地模型配置区域** (`groupBox_LocalModelConfig`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `chk_EnableLocalModel` | `CheckBox` | Text="启用本地离线大模型" | 本地/云端互斥切换 |
| `txt_LocalModelPath` | `TextBox` | ReadOnly=true, Size=181×23 | 模型文件路径 |
| `btn_BrowseModel` | `Button` | Text="⚙" | 浏览选择.gguf文件 |
| `cbo_GpuAccelMode` | `ComboBox` | Size=193×25 | GPU加速模式选择 |
| `numericUpDown1` | `NumericUpDown` | Size=173×23 | 显存/内存上限(MB) |
| `lbl_LocalModelPath` | `Label` | Text="模型文件路径：" | — |
| `lbl_GpuAccelMode` | `Label` | Text="硬件加速模式：" | — |
| `lbl_GpuMemLimit` | `Label` | Text="显存 / 内存上限 (MB)：" | — |

**底部按钮**:

| 控件变量名 | 类型 | 说明 |
|-----------|------|------|
| `btnSave` | `Button` | Text="保存配置" |

#### 11家服务商字典

```csharp
private static readonly Dictionary<string, (string Name, string BaseUrl)> Providers = new()
{
    // 实际的11家服务商配置
    // 每个条目: { key, (显示名称, 基础URL) }
};
```

#### 核心逻辑

```csharp
// 窗体加载
private void FrmApiConfig_Load(object sender, EventArgs e)
{
    // 1. 从数据库加载已有配置
    // 2. 填充 cboProvider 下拉选项
    // 3. 如果有保存的配置，回填所有字段
    // 4. 检测GPU信息，填充 cbo_GpuAccelMode
    //    - 注册表查询 NVIDIA
    //    - WMI 查询 AMD
}

// 服务商切换
private void cboProvider_SelectedIndexChanged(...)
{
    // 1. 更新 txtBaseUrl 为对应服务商的默认地址
    // 2. 清空 cboModel
}

// 本地/云端互斥
private void chk_EnableLocalModel_CheckedChanged(...)
{
    // checked=true:
    //   - groupBox_CloudApiConfig.Enabled = false
    //   - groupBox_LocalModelConfig.Enabled = true
    // checked=false:
    //   - groupBox_CloudApiConfig.Enabled = true
    //   - groupBox_LocalModelConfig.Enabled = false
}

// 浏览模型文件
private void btn_BrowseModel_Click(...)
{
    // OpenFileDialog，筛选 .gguf 文件
    // 选中后填充 txt_LocalModelPath
}

// 获取模型列表
private async void btnLoadModels_Click(...)
{
    // 1. 调用服务商 API 获取可用模型列表
    // 2. 填充 cboModel 下拉选项
}

// 测试连接
private async void btnTestConnect_Click(...)
{
    // 1. 构建测试请求
    // 2. POST 到 {BaseUrl}/chat/completions
    // 3. 检查响应状态码
    // 4. 显示成功/失败提示
}

// 保存配置
private void btnSave_Click(...)
{
    // 1. 构建 ApiConfig 对象
    // 2. UserRepository.SaveApiConfig(config)
    // 3. 提示保存成功
}
```

### 8.4 FrmSessionSettings — 会话设置窗体

```
文件: ChatAI/UI/Forms/FrmSessionSettings.cs + FrmSessionSettings.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "会话设置窗口"
尺寸: 708 × 365
BorderStyle: FixedDialog
StartPosition: CenterParent
字体: 微软雅黑 12pt
```

#### 控件清单

**基础设置区域** (`grpBasicSettings`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `lblContextLength` | `Label` | Text="上下文长度：" | — |
| `txtContextLength` | `TextBox` | Text="20", Size=55×29 | 上下文轮数 |
| `lblMaxTokens` | `Label` | Text="最大上行Tokens：" | — |
| `txtMaxTokens` | `TextBox` | Text="4096", Size=55×29 | 最大token数 |
| `lblTemperature` | `Label` | Text="温度值：" | — |
| `trkTemperature` | `TrackBar` | Maximum=20 | 温度滑块（值÷10=实际温度） |
| `lblTemperatureValue` | `Label` | Text="0.3" | 当前温度值显示 |
| `chkNoRole` | `CheckBox` | Text="纯净模式" | 禁用角色设定 |
| `lblNoRole` | `Label` | Text="(勾选纯净模式后，将会禁用所有用户和AI角色设定)" | 说明文字 |

**记忆设置区域** (`grpMemorySettings`):

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `lblAutoMemoryCount` | `Label` | Text="自动记忆触发条数：" | — |
| `txtAutoMemoryCount` | `TextBox` | Text="20", Size=55×29 | 触发条数 |
| `lblMemoryPrompt` | `Label` | Text="记忆生成提示词：" | — |
| `txtMemoryPrompt` | `TextBox` | Multiline, Size=340×185 | 记忆生成提示词 |

**底部按钮**:

| 控件变量名 | 类型 | 说明 |
|-----------|------|------|
| `btnSaveSessionSettings` | `Button` | Text="保存", BackColor=DeepSkyBlue |

#### 默认记忆提示词

```
根据人设、与用户对话历史，以AI第一人称"我"的角度总结与用户的重要交谈内容，
语言精炼无废话（3-5句话），不添加虚构内容，仅保留可长期复用的核心记忆点，
用于后续对话个性化参考。
```

#### 核心逻辑

```csharp
// 温度值换算: trkTemperature.Value(0-20) → 实际温度(0.0-2.0)
// temperature = trkTemperature.Value / 10.0

// 滑块实时显示
trkTemperature.Scroll += (s, e) =>
{
    lblTemperatureValue.Text = (trkTemperature.Value / 10.0).ToString("F1");
};

// Placeholder 处理
// txtContextLength、txtMaxTokens、txtAutoMemoryCount 支持 Placeholder
// 获得焦点时：清除灰色斜体占位文本，切换为黑色正体
// 失去焦点时：如为空则恢复占位文本

// 保存逻辑
private void btnSaveSessionSettings_Click(...)
{
    var settings = new SessionSettings
    {
        Username = _currentUsername,
        CharacterId = _currentCharacterId,
        ContextLength = int.Parse(txtContextLength.Text),
        MaxTokens = int.Parse(txtMaxTokens.Text),
        Temperature = trkTemperature.Value / 10.0,
        AutoMemoryCount = int.Parse(txtAutoMemoryCount.Text),
        MemoryPrompt = txtMemoryPrompt.Text,
        NoRole = chkNoRole.Checked
    };
    _userRepository.SaveSessionSettings(settings);
}
```

### 8.5 FrmMemoryManager — 记忆管理窗体

```
文件: ChatAI/UI/Forms/FrmMemoryManager.cs + FrmMemoryManager.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "记忆管理窗口"
尺寸: 703 × 682
BorderStyle: FixedDialog
StartPosition: CenterParent
```

#### 布局结构

```
┌──────────────────────────────────────────────────┐
│ splitContainer1 (垂直分割, SplitterDistance=175) │
│ ┌──────────┬─────────────────────────────────┐  │
│ │          │ splitContainer2 (水平分割)       │  │
│ │lbxRole   │ ┌─────────────────────────────┐│  │
│ │List      │ │ dgvMemories (DataGridView)  ││  │
│ │          │ │                             ││  │
│ │(会话选择) │ │                             ││  │
│ │          │ │                             ││  │
│ │          │ ├─────────────────────────────┤│  │
│ │          │ │ splitContainer3             ││  │
│ │          │ │ ┌──────────┬──────────────┐ ││  │
│ │          │ │ │btnDelete │  btnSave     │ ││  │
│ │          │ │ │ Memory   │  Memory      │ ││  │
│ │          │ │ └──────────┴──────────────┘ ││  │
│ │          │ └─────────────────────────────┘│  │
│ └──────────┴─────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

#### 控件清单

| 控件变量名 | 类型 | 说明 |
|-----------|------|------|
| `splitContainer1` | `SplitContainer` | 垂直分割（左：角色列表，右：记忆详情） |
| `lbxRoleList` | `ListBox` | 会话角色选择列表 |
| `splitContainer2` | `SplitContainer` | 水平分割（上：记忆表格，下：操作按钮） |
| `dgvMemories` | `DataGridView` | 记忆数据表格 |
| `splitContainer3` | `SplitContainer` | 水平分割（左：删除按钮，右：保存按钮） |
| `btnDeleteMemory` | `Button` | Text="删除记忆", BackColor=SandyBrown |
| `btnSaveMemory` | `Button` | Text="保存记忆", BackColor=DeepSkyBlue |

#### 核心逻辑

```csharp
// 内部数据结构
private Dictionary<int, string> _memoryMap;  // memoryId → content

// 窗体加载
private void FrmMemoryManager_Load(...)
{
    // 1. 加载所有AI角色，填充 lbxRoleList
    // 2. 配置 dgvMemories 手动列（不自动生成）：
    //    - Column 0: "ID", DataGridViewTextBoxColumn
    //    - Column 1: "记忆内容", DataGridViewTextBoxColumn
}

// 角色切换
private void lbxRoleList_SelectedIndexChanged(...)
{
    // 1. 获取选中角色的所有记忆
    // 2. 填充 dgvMemories
    // 3. 更新 _memoryMap
}

// 双击编辑记忆
private void dgvMemories_CellDoubleClick(...)
{
    // 打开 FrmMemoryEdit 内部窗体
    var editForm = new FrmMemoryEdit(memoryContent);
    if (editForm.ShowDialog() == DialogResult.OK)
    {
        // 更新记忆内容
        // 刷新 dgvMemories
    }
}

// 删除记忆
private void btnDeleteMemory_Click(...)
{
    // 1. 获取 dgvMemories 选中行
    // 2. 提取记忆ID
    // 3. UserRepository.DeleteMemory(memoryId)
    // 4. 刷新列表
}

// 保存记忆
private void btnSaveMemory_Click(...)
{
    // 1. 遍历 dgvMemories 所有行
    // 2. 比对 _memoryMap，找出修改过的记忆
    // 3. 更新数据库
}
```

#### 内部类 FrmMemoryEdit

```csharp
// 手动 InitializeComponent（不使用 .Designer.cs）
// 包含一个 Multiline TextBox 用于编辑记忆文本
// 正则处理换行符转换（\n → \r\n）
// 确认/取消按钮
```

### 8.6 FrmUserProfileSettings — 用户资料设置窗体

```
文件: ChatAI/UI/Forms/FrmUserProfileSettings.cs + FrmUserProfileSettings.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "用户配置窗口"
尺寸: 342 × 592
BorderStyle: FixedDialog
StartPosition: CenterParent
```

#### 控件清单

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `lblNickName` | `Label` | Text="昵    称：" | — |
| `txtNickName` | `TextBox` | Italic, ForeColor=LightGray, Placeholder="请输入用户昵称" | 昵称输入 |
| `lblGender` | `Label` | Text="性    别：" | — |
| `rdoMale` | `RadioButton` | Text="男♂", Checked=true | 男性 |
| `rdoFemale` | `RadioButton` | Text="女♀" | 女性 |
| `lblPersona` | `Label` | Text="人设设定：" | — |
| `txtPersona` | `TextBox` | Multiline, Italic, Placeholder="请输入人设设定（性格、背景等）" | 人设输入 |
| `lblSystemPrompt` | `Label` | Text="系统提示词：" | — |
| `txtSystemPrompt` | `TextBox` | Multiline, Italic, Placeholder="你是一个专业的演员，严格遵守以下人设全程扮演，绝不跳出角色：" | 系统提示词 |
| `btnSaveUserProfile` | `Button` | Text="保存", BackColor=RGB(0,122,204), ForeColor=White | 保存按钮 |
| `btnCancel` | `Button` | Text="取消", BackColor=RGB(220,220,220) | 取消按钮 |

#### 核心逻辑

```csharp
// Placeholder 处理（与 FrmSessionSettings 相同模式）
// txtNickName / txtPersona / txtSystemPrompt 支持 Placeholder
// Enter 事件：清除灰色斜体占位文本
// Leave 事件：如为空则恢复

// 圆角按钮
private void SetButtonRound(Button btn, int radius)
{
    // 通过 GraphicsPath 创建圆角矩形区域
    // btn.Region = new Region(graphicsPath);
    // radius=8
}

// 窗体加载
private void FrmUserProfileSettings_Load(...)
{
    // 1. 从数据库加载用户资料
    // 2. 回填所有字段
    // 3. 非 Placeholder 模式显示（黑色正体）
}

// 保存
private void btnSaveUserProfile_Click(...)
{
    // 1. 构建 UserProfile 对象
    // 2. UserRepository.SaveUserProfile(profile)
    // 3. 关闭窗体
}
```

### 8.7 CreateSessionForm — 创建/编辑AI角色窗体

```
文件: ChatAI/UI/Forms/CreateSessionForm.cs + CreateSessionForm.Designer.cs
命名空间: ChatAI.UI.Forms
窗体标题: "创建新会话💬"
尺寸: 423 × 680
BorderStyle: FixedDialog
MinimizeBox: false
MaximizeBox: false
StartPosition: CenterScreen
BackColor: RGB(245,245,245)
```

#### 控件清单

| 控件变量名 | 类型 | 关键属性 | 说明 |
|-----------|------|---------|------|
| `lblNickName` | `Label` | Text="昵    称：" | — |
| `txtNickName` | `TextBox` | Size=300×29, Placeholder="请输入角色昵称" | 角色昵称 |
| `lblGender` | `Label` | Text="性    别：" | — |
| `rdoMale` | `RadioButton` | Text="男♂", Checked=true | 男性 |
| `rdoFemale` | `RadioButton` | Text="女♀" | 女性 |
| `lblRoleDesc` | `Label` | Text="人设设定：" | — |
| `txtRoleDesc` | `TextBox` | Multiline, Size=300×94, Placeholder="请输入人设设定（性格、背景等）" | 人设描述 |
| `lblTalkStyle` | `Label` | Text="表达风格：" | — |
| `txtTalkStyle` | `TextBox` | Multiline, Size=300×74, Placeholder="请输入表达风格（语言特点等）" | 表达风格 |
| `lblHabit` | `Label` | Text="习惯用语：" | — |
| `txtHabit` | `TextBox` | Multiline, Size=300×74, Placeholder="请输入习惯用语（口头禅等）" | 习惯用语 |
| `lblOpening` | `Label` | Text="开场白：" | — |
| `txtOpening` | `TextBox` | Multiline, Size=300×80, Placeholder="请输入开场白（例如：好久不见！）" | 开场白 |
| `btnCreate` | `Button` | Text="创建", BackColor=RGB(0,122,204), ForeColor=White | 创建/更新按钮 |
| `btnCancel` | `Button` | Text="取消", BackColor=RGB(220,220,220) | 取消按钮 |

#### 核心逻辑

```csharp
// 属性
public bool IsEditMode { get; set; } = false;        // 编辑模式标记
public AiCharacter CreatedAiCharacter { get; set; }   // 输出属性（创建/编辑后的角色）
public int EditCharacterId { get; set; }              // 编辑时的角色ID

// Placeholder 统一处理
private void Txt_GotFocus(object sender, EventArgs e)
{
    var txt = (TextBox)sender;
    if (txt.ForeColor == Color.LightGray)
    {
        txt.Text = "";
        txt.ForeColor = Color.Black;
        txt.Font = new Font("微软雅黑", 12F, FontStyle.Regular);
    }
}

private void Txt_LostFocus(object sender, EventArgs e)
{
    var txt = (TextBox)sender;
    if (string.IsNullOrWhiteSpace(txt.Text))
    {
        // 恢复占位文本
        txt.ForeColor = Color.LightGray;
        txt.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
        // 根据控件名称设置对应占位文本
    }
}

// 创建/编辑
private void btnCreate_Click(...)
{
    // 1. 验证昵称非空且不超过10个字符
    // 2. 区分 Placeholder 和实际输入
    // 3. 构建或更新 AiCharacter 对象
    // 4. IsEditMode ?
    //    true → UserRepository.UpdateAiCharacter(character)
    //    false → id = UserRepository.AddAiCharacter(character)
    // 5. CreatedAiCharacter = character
    // 6. this.DialogResult = DialogResult.OK
}
```

---

## 9. UI表现层 — 自定义控件库

### 9.1 ChatMainControl — GDI+ 自绘气泡引擎

```
文件: ChatControl/Controls/ChatMainControl.cs + ChatMainControl.Designer.cs
命名空间: ChatControl.Controls
规模: 1392行（整个项目最大最复杂的单个控件）
```

#### 私有内部类 ChatMessage

```csharp
// 注意：这是控件内部的消息模型，不是 ChatAI.Data.Entities.ChatMessage
private class ChatMessage
{
    public int Id { get; set; }                // 消息ID
    public string Text { get; set; }           // 消息文本
    public bool IsSelf { get; set; }           // true=用户, false=AI
    public Rectangle DrawRect { get; set; }    // 气泡绘制区域
    public Rectangle AvatarRect { get; set; }  // 头像绘制区域
    public int TotalMessageCount { get; set; } // 总消息数（用于时间戳显示）
    public DateTime SendTime { get; set; }     // 发送时间
    public bool ShowTimestamp { get; set; }    // 是否显示时间戳
    public Size MeasuredSize { get; set; }     // 测量后的气泡尺寸
}
```

#### 样式配置常量

```csharp
// 气泡颜色
private readonly Color _selfColor = Color.FromArgb(181, 236, 208);       // 用户气泡（浅绿）
private readonly Color _maleAiColor = Color.LightSkyBlue;                 // AI男性气泡（浅蓝）
private readonly Color _femaleAiColor = Color.FromArgb(248, 205, 222);    // AI女性气泡（浅粉）

// 头像颜色（圆形占位头像）
private readonly Color _selfAvatarBg = Color.FromArgb(181, 236, 208);     // 用户头像背景
private readonly Color _maleAiAvatarBg = Color.LightSkyBlue;               // 男性AI头像背景
private readonly Color _femaleAiAvatarBg = Color.FromArgb(248, 205, 222);  // 女性AI头像背景
private readonly Color _avatarBorderColor = Color.White;                    // 头像白色边框（2px）

// 选中态气泡颜色
private readonly Color _selfSelectedBubbleBg = Color.FromArgb(255, 200, 200, 200);  // 用户选中
private readonly Color _aiSelectedBubbleBg = Color.FromArgb(255, 220, 220, 220);    // AI选中

// 布局参数
private readonly int _avatarSize = 40;                  // 头像直径（圆形）
private readonly int _avatarMargin = 8;                  // 头像与气泡间距
private readonly double _maxBubbleWidthRatio = 0.6;      // 气泡最大宽度占比（控件宽度的60%）
private readonly int _cornerRadius = 12;                 // 气泡圆角半径（像素）
private readonly int _bubblePadding = 8;                 // 气泡内边距
private readonly int _bubbleMargin = 10;                 // 两条消息间距
private readonly float _fontSize = 9.5f;                 // 消息正文字号
private readonly int _scrollBarWidth = 18;               // 滚动条宽度
private readonly int _timestampHeight = 25;              // 时间戳气泡高度
private readonly int _timestampMargin = 5;               // 时间戳与消息间距
private readonly Color _timestampBgColor = Color.FromArgb(240, 240, 240);  // 时间戳背景色
private readonly int _timeGroupInterval = 5;             // 时间分组间隔（分钟）
private readonly Color _timestampTextColor = Color.FromArgb(100, 100, 100); // 时间戳文字色
```

#### 控件组成（来自 .Designer.cs + 代码动态创建）

| 控件变量名 | 类型 | 说明 |
|-----------|------|------|
| `splitContainer1` | `SplitContainer` | 上下分割容器（上：聊天区域，下：输入区域） |
| `panelChat` | `Panel` | 聊天消息绘制面板（Dock=Fill，启用双缓冲） |
| `panelInput` | `Panel` | 输入区域面板（底部） |
| `txtInput` | `TextBox` | 消息输入框（Enter发送，Shift+Enter换行，字体：微软雅黑10.5pt） |
| `btnSend` | `Button` | 发送按钮 |
| `btnInsertBracket` | `Button` | 插入中文括号"（）"按钮 |
| `btnRetryReply` | `Button` | 重试AI回复按钮（仅最后一条为AI消息时可点击） |
| `lbl_CurrentChatName` | `Label` | 当前聊天名称标签（居中显示在聊天区域顶部） |
| `_vScrollBar` | `VScrollBar` | 代码动态创建的垂直滚动条（Dock=Right，Width=18px） |

#### 事件定义（使用自定义 EventArgs）

```csharp
// 事件参数类（定义在 ChatControl.Controls 命名空间）
public class MessageEventArgs : EventArgs
{
    public string Text { get; set; }       // 消息文本
    public bool IsSelf { get; set; }       // 是否用户消息
    public DateTime SendTime { get; set; } // 发送时间
}

public class AiResponseEventArgs : EventArgs
{
    public string UserMessage { get; set; } // 触发回复的用户消息
    public DateTime SendTime { get; set; }  // 发送时间
}

public class MessageRollbackEventArgs : EventArgs
{
    public int MessageId { get; set; }         // 回溯目标消息ID
    public int TotalMessageCount { get; set; }  // 该消息时的总数
    public bool IsUserMessage { get; set; }     // 是否用户消息
    public string MessageText { get; set; }     // 消息文本
    public DateTime SendTime { get; set; }      // 发送时间
}

public class MessageUpdateEventArgs : EventArgs
{
    public int MessageId { get; set; }         // 消息ID
    public int TotalMessageCount { get; set; }  // 消息总数
    public string MessageText { get; set; }     // 消息文本
    public bool IsSelf { get; set; }            // 是否用户消息
    public DateTime SendTime { get; set; }      // 发送时间
}

// 事件声明
public event EventHandler<MessageEventArgs>? MessageSent;              // 用户发送消息
public event EventHandler<AiResponseEventArgs>? AiResponseReceived;     // 收到AI回复/请求重新生成
public event EventHandler? RegenerateResponseRequested;                 // 请求重新生成AI回复
public event EventHandler<MessageRollbackEventArgs>? MessageRollbackRequested; // 消息回溯
public event EventHandler<MessageUpdateEventArgs>? MessageUpdated;       // 消息信息更新
```

#### 公开方法

```csharp
// 添加AI回复气泡
public void AddAiMessage(string message)
{
    // 1. 创建 ChatMessage 对象（IsSelf=false）
    // 2. 设置性别颜色（男性/女性）
    // 3. 添加到 _messages 列表
    // 4. 触发 AiResponseReceived 事件
    // 5. 调整滚动条
    // 6. Invalidate() 触发重绘
}

// 清空所有消息
public void ClearMessages()
{
    // 1. _messages.Clear()
    // 2. Invalidate()
}

// 设置AI角色信息（影响气泡颜色和头像）
public void SetAiInfo(string nickname, string gender)

// 加载历史消息
public void LoadMessages(List<Entities.ChatMessage> messages)
```

#### GDI+ 绘制流程

```csharp
protected override void OnPaint(PaintEventArgs e)
{
    base.OnPaint(e);
    Graphics g = e.Graphics;
    g.SmoothingMode = SmoothingMode.AntiAlias;  // 抗锯齿

    // 1. 遍历 _messages 列表
    // 2. 对每条消息：
    //    a. 绘制头像（圆形头像）
    //       - 用户头像：右侧
    //       - AI头像：左侧
    //       - 使用 UCAvatarBox 的绘制逻辑
    //    b. 绘制圆角气泡
    //       - 使用 GraphicsExtensions.DrawRoundedRectangle()
    //       - 根据 IsSelf 选择颜色
    //       - _selfColor（用户）/ _maleAiColor（男性AI）/ _femaleAiColor（女性AI）
    //    c. 绘制消息文本
    //       - 气泡内绘制，自动换行
    //       - 使用 Graphics.MeasureString() 计算布局
    //    d. 绘制时间戳
    //       - 如果 ShowTimestamp==true
    //       - 居中显示，灰色小字
    //       - 间隔超过 _timeGroupInterval 分钟才显示
}
```

#### 右键菜单

```csharp
// _copyMenu: ContextMenuStrip
// 三个菜单项：
// 1. "复制"     → 将选中气泡文本复制到剪贴板
// 2. "重新生成" → 触发 RegenerateResponseRequested 事件
// 3. "回溯"     → 触发 MessageRollbackRequested 事件，传入 messageId
```

#### 滚动管理

```csharp
// 手动管理 VScrollBar
// vScrollBar1.Maximum = totalContentHeight - clientHeight
// vScrollBar1.Value 变化时 → Invalidate() 重绘

// 防抖Resize
// _resizeTimer.Interval = 50ms
// Resize 时启动定时器，50ms后执行实际布局计算
// 避免拖拽窗口边缘时频繁重绘
```

#### 文本测量与气泡尺寸

```csharp
private Size MeasureBubbleSize(string text, int maxWidth)
{
    // 1. 使用 Graphics.MeasureString() 测量文本
    // 2. 考虑内边距（Padding）
    // 3. maxWidth = 控件宽度 × _maxBubbleWidthRatio
    // 4. 返回包含内边距的最终气泡尺寸
}
```

### 9.2 SessionListUC — 会话列表控件

```
文件: ChatControl/Controls/SessionListUC.cs + SessionListUC.Designer.cs
命名空间: ChatControl.Controls
规模: 260行
```

#### 控件组成

| 控件变量名 | 类型 | 说明 |
|-----------|------|------|
| `lstSession` | `ListBox` | DrawMode=OwnerDrawFixed, ItemHeight=32, Font=微软雅黑12pt | 自绘会话列表 |
| `btnCreateSession` | `Button` | 创建新会话按钮 |
| `contextMenuStrip1` | `ContextMenuStrip` | 右键菜单（删除会话/编辑会话/查看记忆） |

#### 美化参数

```csharp
private readonly int _leftPadding = 6;            // 左侧内边距
private readonly int _iconNameGap = 12;           // 图标与名称间距
private readonly int _charSpacing = 1;            // 字间距（逐字绘制时每个字符后添加）
private readonly Color _selectedBackColor = Color.FromArgb(220, 235, 255);  // 选中项背景色
private readonly Color _selectedForeColor = Color.Black;                     // 选中项文字色
private readonly Color _normalBackColor = Color.Gainsboro;                   // 普通项背景色
private readonly Color _normalForeColor = Color.Black;                     // 普通项文字色
```

#### 事件定义

```csharp
public event Action<SessionInfo> SessionSelected;      // 选中会话
public event Action OnCreateSession;                    // 创建会话
public event Action<int> OnDeleteSession;               // 删除会话（参数=角色ID）
public event Action<SessionInfo> OnEditSession;         // 编辑会话
public event Action<int> OnViewMemory;                   // 查看记忆（参数=角色ID）
```

#### 自绘逻辑

```csharp
// DrawMode = OwnerDrawFixed
private void lstSessions_DrawItem(object sender, DrawItemEventArgs e)
{
    // 1. 获取 SessionInfo 对象
    // 2. 绘制选中/未选中背景色
    // 3. 逐字绘制昵称（实现字间距效果）
    //    - 遍历每个字符
    //    - 每个字符后添加固定间距
    //    - 使用 Graphics.DrawString() 逐字符绘制
    // 4. 绘制性别图标
    //    - ♂ DodgerBlue
    //    - ♀ HotPink
    // 5. 绘制小三角（展开更多操作的指示器）
}
```

#### 公开方法

```csharp
// 加载会话列表
public void LoadSessions(List<SessionInfo> sessions)
{
    // 1. lstSessions.Items.Clear()
    // 2. foreach session → lstSessions.Items.Add(session)
}

// 添加单个会话
public void AddSession(SessionInfo session)

// 移除会话
public void RemoveSession(int characterId)
```

### 9.3 UCAvatarBox — 头像控件

```
文件: ChatControl/Controls/UCAvatarBox.cs + UCAvatarBox.Designer.cs
命名空间: ChatControl.Controls
```

#### 功能

- 显示圆形头像
- 根据性别显示不同默认头像
- 用于 ChatMainControl 中每条消息的头像绘制

#### 公开属性/方法

```csharp
// 设置头像信息
public void SetAvatarInfo(string nickname, string gender)

// 设置头像图片
public void SetImage(Image image)
```

### 9.4 GraphicsExtensions — GDI+ 扩展方法

```
文件: ChatControl/Utils/GraphicsExtensions.cs
命名空间: ChatControl.Utils
```

#### 核心方法

```csharp
// 绘制圆角矩形填充
public static void FillRoundedRectangle(
    this Graphics g,
    Brush brush,
    float x, float y, float width, float height,
    float radius
)

// 绘制圆形图片（用于头像显示）
public static void DrawCircleImage(
    this Graphics g,
    Image? image,
    Rectangle rect
)
// 处理空引用、资源释放，默认回退到系统图标

// 为控件开启双缓冲（解决ListBox闪烁）
public static void SetDoubleBuffered(
    this Control control,
    bool enable
)
```

---

## 10. 核心业务流程

### 10.1 用户登录流程

```
用户输入用户名和密码
        │
        ▼
FrmLogin.btnLogin_Click()
        │
        ▼
验证输入非空 ────否──→ 提示错误
        │是
        ▼
EncryptionHelper.Encrypt(password)
        │
        ▼
UserRepository.LoginUser(username, encryptedPassword)
        │
        ├── 成功 → LoggedInUsername = username
        │         chkRememberPwd ? → 保存凭据
        │         DialogResult = OK → 关闭窗体
        │
        └── 失败 → 提示"用户名或密码错误"
```

### 10.2 发送消息完整流程

```
用户在 ChatMainControl 输入消息并按发送
        │
        ▼
ChatMainControl.MessageSent 事件触发
        │
        ▼
FrmChatMain.ChatMainControl_MessageSent()
        │
        ├── _currentCharacterId == -1 ?
        │   │
        │   └── 是 → HandleNoSessionSelected()
        │           → 自动创建"AI聊天助手"角色
        │           → 显示开场白
        │           → return
        │
        ▼
SendUserMessageToAI(userMessage)
        │
        ├── 1. 保存用户消息（AES加密）到 chat_message 表
        │
        ├── 2. 加载历史消息（AES解密）
        │
        ├── 3. 获取 SessionSettings + AiCharacter
        │
        ├── 4. 构建系统提示词
        │   │
        │   ├── NoRole==true → systemPrompt=null（纯净模式）
        │   │
        │   └── NoRole==false → 拼接:
        │       - 用户人设 (user_profile.persona)
        │       - 用户系统提示词 (user_profile.system_prompt)
        │       - AI角色设定 (ai_character.role_desc + talk_style + habit)
        │       - AI长期记忆 (ai_memory 表中该角色的所有记忆)
        │
        ├── 5. 调用 AI 生成回复
        │   │
        │   ├── enable_local_model==true
        │   │   → LocalModelServiceSingleton.GenerateResponseAsync()
        │   │
        │   └── enable_local_model==false
        │       → 构建JSON → POST到云端API → 解析响应
        │
        ├── 6. 保存AI回复（AES加密）到 chat_message 表
        │
        ├── 7. ChatMainControl.AddAiMessage(response)
        │   → 触发 GDI+ 重绘新气泡
        │
        └── 8. CheckAndGenerateMemory(newTotalCount)
            │
            └── newTotalCount % autoMemoryCount == 0 ?
                │
                └── 是 → 加入 _pendingMemoryRequests 队列
                    → ProcessMemoryQueueAsync()
                    → AI总结对话 → 保存记忆到 ai_memory 表
```

### 10.3 消息回溯流程

```
用户右键点击AI消息 → 选择"回溯"
        │
        ▼
ChatMainControl.MessageRollbackRequested(messageId)
        │
        ▼
FrmChatMain.ChatMainControl_MessageRollbackRequested()
        │
        ├── 1. UserRepository.DeleteMessagesAfter(characterId, messageId)
        │       → DELETE FROM chat_message WHERE character_id=? AND id>?
        │
        ├── 2. UserRepository.DeleteMemoriesAfterMessage(characterId, messageId)
        │       → 删除该消息之后产生的记忆
        │
        └── 3. ReloadAndRefreshMessages()
                → 重新从数据库加载消息
                → ChatMainControl.LoadMessages()
                → GDI+ 重新绘制
```

### 10.4 自动记忆生成流程

```
每条消息发送完成后
        │
        ▼
CheckAndGenerateMemory(newTotalCount)
        │
        ├── newTotalCount % autoMemoryCount != 0 ?
        │   └── 不触发，直接返回
        │
        ▼
加入 _pendingMemoryRequests 队列
        │
        ▼
ProcessMemoryQueueAsync()
        │
        ├── _isMemoryGenerating == true ?
        │   └── 已有任务进行中，return（队列等待）
        │
        ▼
_isMemoryGenerating = true
        │
        ▼
循环处理队列：
        │
        ├── 1. Dequeue 获取 (characterId, messageCount)
        │
        ├── 2. 加载历史消息（最近 autoMemoryCount 条）
        │
        ├── 3. 构建 conversationSummary（对话摘要文本）
        │
        ├── 4. TextGenerator.GenerateMemoryWithQwenFormatAsync(
        │       conversationSummary, memoryPrompt, settings)
        │       → 调用AI生成3-5句精炼记忆
        │
        ├── 5. UserRepository.SaveAiMemory(new AiMemory {
        │       CharacterId = characterId,
        │       Content = EncryptionHelper.Encrypt(memoryText),
        │       CreateTime = DateTime.Now
        │   })
        │
        └── 6. 继续处理队列中的下一个请求
        │
        ▼
队列清空 → _isMemoryGenerating = false
```

### 10.5 记忆注入流程

记忆在发送消息时作为系统提示词的一部分被注入，而非在消息列表中直接展示。

```
构建 systemPrompt 时：
        │
        ▼
加载该角色的所有记忆：
UserRepository.GetMemoriesByCharacterId(characterId)
        │
        ▼
解密所有记忆内容
        │
        ▼
拼接格式：
"""
【长期记忆】
- 记忆1内容
- 记忆2内容
- 记忆3内容
...
"""
        │
        ▼
拼接到 systemPrompt 末尾
        │
        ▼
作为 messages[0] {"role": "system", "content": systemPrompt} 发送给AI
```

### 10.6 创建AI角色流程

```
用户点击 SessionListUC 的创建按钮
        │
        ▼
FrmChatMain.SessionListUC_OnCreateSession()
        │
        ▼
new CreateSessionForm() → ShowDialog()
        │
        ▼
用户填写表单：
- 昵称（限10字符）
- 性别（男/女单选）
- 人设设定（多行文本）
- 表达风格（多行文本）
- 习惯用语（多行文本）
- 开场白（多行文本）
        │
        ▼
btnCreate_Click()
        │
        ├── 验证昵称非空
        ├── 验证昵称≤10字符
        ├── 区分 Placeholder 和真实输入
        │
        ▼
构建 AiCharacter 对象
        │
        ▼
UserRepository.AddAiCharacter(character) → 返回 characterId
        │
        ▼
CreatedAiCharacter = character
DialogResult = OK
        │
        ▼
FrmChatMain 接收回调：
        │
        ├── 1. RefreshSessionList()
        │       → UserRepository.GetAiCharacters(username)
        │       → SessionListUC.LoadSessions()
        │
        ├── 2. 自动选中新创建的角色
        │
        └── 3. 如果有开场白 → ChatMainControl.AddAiMessage(opening)
```

---

## 11. 加密体系

### 11.1 总体架构

本项目采用**双层 AES-256 密钥架构**，在防止静态逆向分析和确保业务数据安全之间取得平衡：

```
┌────────────────────────────────────────────────────────────┐
│                     双层加密安全模型                          │
│                                                            │
│  代码二进制中的三段常量碎片（seg1/seg2/seg3）                  │
│         │ 交叉穿插 + XOR扰动 + 位旋转                        │
│         ▼                                                  │
│  外层密钥 K2（GetRuntimeSkey）                               │
│  ├── 不存储，每次运行时推导，结果固定                           │
│  └── 防止仅凭数据库文件还原明文                               │
│         │                                                  │
│         │ K2 加密 (AES-256-CBC, 随机IV)                     │
│         ▼                                                  │
│  [Sys_Secret.WrapRoot] = Base64(RandomIV + AES(K2, K1))   │
│         │                                                  │
│         │ K2 解密                                           │
│         ▼                                                  │
│  内层密钥 K1（_rootAesKey，32字节密码学安全随机）              │
│         │                                                  │
│         │ K1 加密 (AES-256-CBC, 确定性IV = SHA256[0..15])   │
│         ▼                                                  │
│  各业务表密文字段（密码/消息/API Key/记忆/人设等）              │
└────────────────────────────────────────────────────────────┘
```

### 11.2 密钥安全特性对比

| 特性 | 传统单层（硬编码密钥） | 本项目双层架构 |
|------|------------------|-------------|
| 密钥位置 | 代码中明文 / 可字符串搜索 | 无完整密钥存在，三段常量经运算推导 |
| 数据库泄露 | 直接可解密 | 无法解密（需要程序二进制才能推导K2） |
| 同用户不同安装 | 密钥相同 | K1随机生成，每个安装唯一 |
| 逆向难度 | 低（字符串搜索即可） | 中（需逆向推导算法 + K1 解密逻辑） |

### 11.3 加密初始化流程

```
程序启动
    │
    ▼
SqliteDbHelper.InitDatabaseAndTables()
    │
    ├── Data.db 不存在（新装）?
    │   │
    │   ├── 1. 创建数据库文件
    │   ├── 2. 执行建表 SQL（8张表）
    │   └── 3. EncryptionHelper.InitializeRootKey(isNewDatabase: true)
    │           │
    │           ├── RandomNumberGenerator 生成 32 字节 K1
    │           ├── WrapRootKey(K1)：用 K2 加密 K1 → Base64 密文
    │           └── SysSecretRepository.SaveWrappedRootKey(密文)
    │                   → INSERT INTO Sys_Secret (Id=1, WrapRoot=密文)
    │
    └── Data.db 已存在（已有安装）?
        └── EncryptionHelper.InitializeRootKey(isNewDatabase: false)
                │
                ├── SysSecretRepository.GetWrappedRootKey()
                │       → SELECT WrapRoot FROM Sys_Secret WHERE Id=1
                └── UnwrapRootKey(密文)：用 K2 解密 → 还原 K1
                        → _rootAesKey 载入内存，供后续加解密使用
```

### 11.4 业务数据加密流程

```
明文字符串（如 "my_password_123"）
        │
        ▼
GenerateDeterministicIV(plainText):
    SHA256("my_password_123") = [AB, 3F, 7C, ...]
    iv = 前16字节 = [AB, 3F, 7C, ..., xx]
        │
        ▼
AES-256-CBC(key=K1, iv=确定性IV).Encrypt(plainText)
        │
        ▼
Base64( iv[16字节] + cipherBytes )
        │
        ▼
"qIK7...xxx==" → 存入数据库字段
```

### 11.5 确定性IV的设计权衡

| 方面 | 说明 |
|------|------|
| **优点** | 相同明文始终产生相同密文，支持 `WHERE content = @encrypted` SQL等值查询 |
| **应用场景** | 消息回溯功能：通过加密后的消息文本在数据库定位精确消息 |
| **安全影响** | 失去**语义安全性**（IND-CPA）：攻击者可知两条密文是否来自相同明文 |
| **可接受原因** | 业务数据不存在统计规律分析价值；攻击者无法获取K1，对等值关系无法利用 |

### 11.6 加密范围汇总

| 数据类型 | 存储位置 | 加密字段 |
|---------|---------|---------|
| 用户密码 | `user_login.password` | `Encrypt(password)` |
| 用户昵称 | `user_profile.nickname` | `Encrypt(nickname)` |
| 用户人设 | `user_profile.persona` | `Encrypt(persona)` |
| 用户系统提示词 | `user_profile.system_prompt` | `Encrypt(system_prompt)` |
| API密钥 | `api_config.api_key` | `Encrypt(apiKey)` |
| 记忆生成提示词 | `session_settings.memory_prompt` | `Encrypt(memoryPrompt)` |
| AI角色设定 | `ai_character` 的 `role_desc`、`talk_style`、`habit`、`opening` | 各字段单独加密 |
| 聊天消息内容 | `chat_message.content` | `Encrypt(content)`，每条消息均加密 |
| AI记忆内容 | `ai_memory.content` | `Encrypt(content)` |
| 根密钥K1本身 | `Sys_Secret.WrapRoot` | `WrapRootKey(K1)`，以K2加密 |

---

## 12. 事件驱动架构

### 12.1 事件关系图

```
SessionListUC                    ChatMainControl                  FrmChatMain
┌──────────────┐              ┌──────────────────┐              ┌──────────────┐
│              │              │                  │              │              │
│ SessionSelected──────────────│                  │              │              │
│ OnCreateSession──────────────│                  │              │              │
│ OnDeleteSession──────────────│                  │              │              │
│ OnEditSession────────────────│                  │              │              │
│ OnViewMemory─────────────────│                  │              │              │
│              │              │ MessageSent─────────────────────→│              │
│              │              │ AiResponseReceived──────────────→│              │
│              │              │ RegenerateResponseRequested─────→│              │
│              │              │ MessageRollbackRequested────────→│              │
│              │              │ MessageUpdated──────────────────→│              │
└──────────────┘              └──────────────────┘              └──────────────┘
```

### 12.2 事件触发场景

| 事件 | 触发场景 |
|------|---------|
| `SessionSelected` | 用户在会话列表中点击某个角色 |
| `OnCreateSession` | 用户点击"创建新会话"按钮 |
| `OnDeleteSession` | 用户选择删除某个会话 |
| `OnEditSession` | 用户选择编辑某个AI角色 |
| `OnViewMemory` | 用户选择查看某个角色的记忆 |
| `MessageSent` | 用户在聊天输入框按回车发送消息 |
| `AiResponseReceived` | AI回复添加到聊天区域后 |
| `RegenerateResponseRequested` | 用户右键AI消息选择"重新生成" |
| `MessageRollbackRequested` | 用户右键消息选择"回溯" |
| `MessageUpdated` | 消息列表发生变化（增删改） |

---

## 13. 代码质量改进建议

### 13.1 高优先级

| 问题 | 当前状态 | 建议改进 |
|------|---------|---------|
| **UserRepository God Class** | 1400+行，承担所有数据访问 | 拆分为 `UserAuthRepository`、`CharacterRepository`、`MessageRepository`、`MemoryRepository`、`SettingsRepository` |
| **HttpClient 重复创建** | 每次API调用 `new HttpClient()` | 改为静态 `HttpClient` 实例或使用 `IHttpClientFactory` |
| **零单元测试** | 无任何测试覆盖 | 使用 xUnit/NUnit 编写核心逻辑测试 |

### 13.1.1 已完成改进

| 问题 | 原状态 | 改进方案 | 完成状态 |
|------|---------|---------|---------|
| **密钥硬编码** | 加密密钥硬编码在源码中 | 双层安全架构：外层动态计算SKey + 内层随机生成RootKey，密钥存储在数据库 | ✅ 已完成 |

### 13.2 中优先级

| 问题 | 当前状态 | 建议改进 |
|------|---------|---------|
| **日志系统** | `Console.WriteLine` | 引入 Serilog 结构化日志 |
| **异常处理** | 部分异常被静默吞掉 | 统一异常处理策略，关键操作添加重试机制 |
| **配置管理** | 硬编码常量散布各处 | 集中配置管理（appsettings.json） |
| **IUserRepository** | 仅登录窗体使用 | 扩展到所有窗体，统一依赖注入 |

### 13.3 低优先级（重构方向）

| 问题 | 当前状态 | 建议改进 |
|------|---------|---------|
| **控件库与主项目耦合** | `ChatMainControl` 内部 `ChatMessage` 类与 `Entities.ChatMessage` 重复 | 通过接口抽象解耦 |
| **Placeholder 逻辑重复** | 多个窗体重复实现相同的Placeholder模式 | 抽取为 `PlaceholderTextBox` 自定义控件 |
| **圆角按钮重复** | `FrmUserProfileSettings` 和 `CreateSessionForm` 都有 | 抽取为 `RoundedButton` 自定义控件 |
| **数据库连接无连接池** | 每次操作创建新连接 | 使用连接池或 `SQLiteConnectionPool` |

---

## 附录 A：文件索引

### ChatAI 项目

| 文件路径 | 行数（约） | 核心职责 |
|---------|----------|---------|
| `Program.cs` | ~30 | 程序入口，登录循环 |
| `Data/SqliteDbHelper.cs` | ~120 | 数据库初始化，建表（含 Sys_Secret） |
| `Data/EncryptionHelper.cs` | ~250 | 双层AES加密/解密（K2推导 + K1包裹 + 确定性IV） |
| `Data/SysSecretRepository.cs` | ~25 | 根密钥K1密文的读写（Sys_Secret表） |
| `Data/UserRepository.cs` | ~1400 | 全量数据访问（God Class） |
| `Data/Entities/UserLogin.cs` | ~15 | 用户登录实体 |
| `Data/Entities/UserProfile.cs` | ~20 | 用户资料实体 |
| `Data/Entities/ApiConfig.cs` | ~30 | API配置实体（含本地模型字段） |
| `Data/Entities/SessionSettings.cs` | ~35 | 会话设置实体 |
| `Data/Entities/AiCharacter.cs` | ~25 | AI角色实体 |
| `Data/Entities/ChatMessage.cs` | ~20 | 聊天消息实体 |
| `Data/Entities/AiMemory.cs` | ~15 | AI记忆实体 |
| `Services/TextGenerator.cs` | ~1700 | AI文本生成核心服务（云/本地路由、记忆生成、调试日志） |
| `Services/LocalModelService.cs` | ~350 | 本地LLamaSharp推理（GPU检测、分层计算、流式推理、自动重试） |
| `Services/ModelFormats/IModelFormat.cs` | ~15 | 模型格式接口（BuildChatPrompt/BuildMemoryPrompt/GetStopWords/CleanResponse） |
| `Services/ModelFormats/QwenModelFormat.cs` | ~60 | 千问 ChatML 格式实现 |
| `Services/ModelFormats/HermesModelFormat.cs` | ~50 | Hermes/Llama-3 格式实现 |
| `Services/ModelFormats/ModelFormatFactory.cs` | ~40 | 模型格式工厂（精确匹配+模糊匹配） |
| `UI/Forms/FrmLogin.cs` | ~203 | 登录/注册/改密 |
| `UI/Forms/FrmChatMain.cs` | ~1083 | 主聊天窗体 |
| `UI/Forms/FrmApiConfig.cs` | ~681 | API配置（云端11家服务商 + 本地模型文件选择 + GPU检测） |
| `UI/Forms/FrmSessionSettings.cs` | ~275 | 会话设置 |
| `UI/Forms/FrmMemoryManager.cs` | ~362 | 记忆管理 |
| `UI/Forms/FrmUserProfileSettings.cs` | ~235 | 用户资料设置 |
| `UI/Forms/CreateSessionForm.cs` | ~334 | 创建/编辑AI角色 |

### ChatControl 项目

| 文件路径 | 行数（约） | 核心职责 |
|---------|----------|---------|
| `Controls/ChatMainControl.cs` | ~1392 | GDI+自绘气泡引擎 |
| `Controls/SessionListUC.cs` | ~260 | 会话列表（自绘ListBox） |
| `Controls/UCAvatarBox.cs` | ~80 | 头像控件（支持裁剪上传） |
| `Models/SessionInfo.cs` | ~70 | 会话信息DTO（10个属性） |
| `Utils/GraphicsExtensions.cs` | ~80 | GDI+扩展方法（圆角矩形、圆形图片、双缓冲） |

---

## 附录 C：本地模型配置参考

### 支持的模型格式

| 格式 | 说明 |
|------|------|
| `.gguf` | GGML 量化格式，由 llama.cpp 原生支持，LLamaSharp 加载此格式 |

### 已验证的模型（ModelFormatFactory 内置支持）

| 模型文件名 | 格式策略 | 参数量 | 上下文窗口 | 每层约 VRAM |
|----------|---------|--------|----------|-----------|
| `Qwen-7B-Chat-Q4_K_M.gguf` | QwenModelFormat（ChatML） | 7B | 8192 tokens | ~100 MB |
| `Hermes-2-Pro-Llama-3-8B.Q4_K_M.gguf` | HermesModelFormat（Llama-3） | 8B | 4096 tokens | ~130 MB |

### GPU 加速模式选择指南

| 硬件条件 | 推荐模式 | 说明 |
|---------|---------|------|
| NVIDIA 显卡 + 8GB+ VRAM | `CUDA 12`（自动选择） | 最佳性能，所有层卸载到 GPU |
| AMD 显卡 + 8GB+ VRAM | `Vulkan` | 使用 Vulkan 后端加速 |
| 无独立显卡 / VRAM < 4GB | `CPU 模式` | 全部在 CPU 推理，速度慢但稳定 |
| VRAM 4-8GB | `CUDA/Vulkan`（手动设置内存限制） | 部分层卸载，其余走 CPU |

### VRAM 内存限制配置

设置显存/内存上限时的参考估算（以 Q4_K_M 量化为例）：

| 模型参数量 | 全 GPU（所有层） | 4GB VRAM | 8GB VRAM |
|----------|---------------|---------|---------|
| 7B 模型  | ~4-5 GB       | 部分层（约32层×100MB=3.2GB） | 全部层 |
| 8B 模型  | ~5-6 GB       | 部分层（约30层×130MB=3.9GB） | 全部层 |

> **提示**：VRAM 上限设置建议比实际可用显存少 500-1000 MB，为系统和其他程序预留空间。程序检测到 VRAM 超出时会弹出警告提示。

---

## 附录 B：API请求格式参考

### 云端API请求体

```json
{
    "model": "gpt-4o",
    "messages": [
        {
            "role": "system",
            "content": "【用户人设】...\n【AI角色设定】...\n【长期记忆】\n- 记忆1\n- 记忆2"
        },
        {
            "role": "user",
            "content": "你好"
        },
        {
            "role": "assistant",
            "content": "你好！有什么可以帮助你的吗？"
        }
    ],
    "temperature": 0.3,
    "max_tokens": 4096
}
```

### 请求头

```
POST {BaseUrl}/chat/completions
Content-Type: application/json
Authorization: Bearer {ApiKey}
```

### 响应格式

```json
{
    "id": "chatcmpl-xxx",
    "object": "chat.completion",
    "choices": [
        {
            "index": 0,
            "message": {
                "role": "assistant",
                "content": "AI的回复内容"
            },
            "finish_reason": "stop"
        }
    ],
    "usage": {
        "prompt_tokens": 100,
        "completion_tokens": 50,
        "total_tokens": 150
    }
}
```

---

> **文档结束**
> 本文档版本 v2.1，基于 ChatAI + ChatControl 项目源码完整分析撰写。
> 相较 v2.0，本版本新增内容：
> - **双层加密机制**完整实现细节（K2推导算法、确定性IV设计原理、Sys_Secret表）
> - **本地模型推理**完整实现（GPU检测、分层计算、流式推理、停词截断、自动恢复）
> - **IModelFormat接口**完整方法定义及两种格式（ChatML / Llama-3）的Prompt构建细节
> - **ApiConfig字段复用机制**（云端/本地模式下字段含义的切换映射）
> - **附录C**：本地模型配置参考（支持模型、GPU模式选择指南、VRAM估算表）
> - 数据库表数量从7张更正为8张（含 Sys_Secret）
