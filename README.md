# ChatAI
ChatAI
基于 C# .NET 10.0、WinForms、SQLite、LLamaSharp（.gguf） 开发的 Windows 桌面 AI 聊天客户端，支持云端 API 与本地离线模型双模式运行。
功能特性
云端 + 本地双推理：支持 OpenAI 兼容云端 API + 本地 .gguf 模型运行
多会话 / 多角色管理：可创建多个 AI 角色，记忆相互独立
自动长期记忆：自动生成核心记忆并注入对话上下文
AES-256 双层加密：敏感数据全程加密存储，安全可靠
GDI+ 自定义聊天气泡：无第三方 UI 依赖，界面流畅美观
GPU 硬件加速：支持 NVIDIA CUDA / AMD Vulkan 加速
技术栈
开发语言：C#
框架：.NET 10.0
UI 框架：WinForms
数据库：SQLite（System.Data.SQLite）
本地模型：LLamaSharp（.gguf 格式）
架构：三层架构 + 事件驱动 + 策略模式
加密：AES-256-CBC
运行环境
操作系统：Windows 10 / 11
运行时：.NET 10.0 SDK / 运行时
开发工具：Visual Studio 2026（推荐）/ Visual Studio 2022
本地模型：建议 GPU 显存 ≥ 4GB
快速开始
克隆或下载本项目
使用 Visual Studio 打开 ChatAI.sln
还原 NuGet 程序包
直接编译并运行
注册账号即可开始使用
重要说明
请勿删除 Data.db 或 Sys_Secret 表，它们存储加密密钥与用户数据
本地模型仅支持 .gguf 格式文件
所有敏感数据（API Key、密码、聊天记录）均已 AES-256 加密
bin/、obj/、.vs/ 已自动排除，不会提交到 Git
许可证
本项目仅限个人学习与开发使用。