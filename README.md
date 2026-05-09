# ChatAI

ChatAI AI聊天客户端｜云端 API + 本地离线大模型双模式｜.NET 10 + WinForms + SQLite

一款基于 C# .NET 10.0、WinForms、SQLite、LLamaSharp 开发的 Windows 桌面 AI 聊天工具，支持云端 API 与本地 .gguf 模型双模式运行，内置双层 AES-256 加密、自动长期记忆、多角色会话管理等完整能力。

## 项目特性
- 云端 + 本地双推理引擎：支持 OpenAI 兼容协议 + 本地 .gguf 模型加载
- 多会话 / 多角色管理：每个人设独立记忆、独立对话历史
- 自动长期记忆系统：对话达到阈值自动总结生成核心记忆
- 双层 AES-256 加密体系：所有敏感数据全程加密存储
- 无第三方 UI 依赖：GDI+ 自绘聊天气泡，界面流畅轻量
- GPU 硬件加速：支持 NVIDIA CUDA / AMD Vulkan 加速
- 事件驱动架构：UI、服务、数据层完全解耦
- 策略模式适配多模型格式：Qwen ChatML / Llama-3 Hermes 自动识别

## 技术栈
- 开发语言：C#
- 运行框架：.NET 10.0
- UI 框架：WinForms（GDI+ 自绘）
- 数据库：SQLite（System.Data.SQLite）
- 本地模型：LLamaSharp（.gguf 格式）
- 架构模式：三层架构 + 事件驱动 + 策略模式
- 加密方式：AES-256-CBC 双层加密

## 项目结构
- ChatAI/
- ├── Data/ # 数据访问层 + 实体 + 加密
- ├── Services/ # 服务层：AI 推理、记忆、格式策略
- ├── UI/Forms/ # 所有窗体：登录、聊天、配置等
- └── Program.cs # 程序入口

- ChatControl/ # 自定义控件库
- ├── Controls/ # 气泡聊天、会话列表、头像
- ├── Models/ # DTO 模型
- └── Utils/ # GDI+ 扩展工具

## 核心功能模块
1. 用户体系：注册 / 登录 / 修改密码、用户资料、人设、系统提示词管理，所有敏感字段 AES 加密存储
2. AI 角色与会话：创建、编辑、删除 AI 角色，独立人设、语气、习惯、开场白，会话参数：上下文长度、温度值、最大 Tokens
3. 云端 API 模式：内置 11 家主流 AI 服务商配置，支持自定义 BaseUrl、Apikey、模型名称，自动获取模型列表、连接测试
4. 本地模型模式：直接加载 .gguf 模型文件，自动 GPU 类型检测（NVIDIA / AMD），GPU 分层卸载、显存限制，流式推理 + 停词截断 + 异常自动重试
5. 聊天系统：GDI+ 自绘聊天气泡，消息发送、重新生成、回溯删除，右键菜单：复制、重新生成、消息回溯，时间分组显示
6. 自动长期记忆：按对话条数自动触发记忆生成，记忆存入数据库并自动注入上下文，记忆查看、编辑、删除
7. 双层加密体系：外层密钥 K2：运行时动态推导，永不落盘；内层密钥 K1：随机生成并加密存储；确定性 IV 设计，支持密文等值查询；覆盖密码、消息、API Key、人设、记忆等全部敏感字段

## 快速启动
- 使用 Visual Studio 2022/2026 打开 ChatAI.sln，还原 NuGet 包，直接编译运行，注册账号即可开始使用。
- 首次运行会自动创建 Data.db 数据库并初始化 8 张表与密钥体系。

## 重要说明
- 请勿删除 Data.db 或 Sys_Secret 表，否则无法解密数据
- 本地模型仅支持 .gguf 格式
- 所有聊天记录、API Key、密码均已加密
- bin/、obj/、.vs/ 已自动排除，不会提交 Git

## 许可证
本项目仅供学习、研究、非商用使用。

## 开发者信息
- 作者：AngusCN-creator
- 项目：https://github.com/AngusCN-creator/ChatAI