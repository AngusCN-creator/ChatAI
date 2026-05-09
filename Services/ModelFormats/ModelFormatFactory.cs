using System;
using System.Collections.Generic;

namespace ChatAI.Services.ModelFormats
{
    /// <summary>
    /// 模型格式工厂类（ModelFormatFactory）
    /// 
    /// 该类负责根据模型名称创建对应的模型格式实现实例，采用简单工厂模式。
    /// 支持的模型格式包括Qwen系列和Hermes系列。
    /// 
    /// 设计特点：
    /// 1. 使用延迟初始化和双重检查锁定（Double-Checked Locking）保证线程安全
    /// 2. 支持按精确名称匹配和关键字模糊匹配
    /// 3. 提供默认格式（Qwen）作为降级方案
    /// </summary>
    public static class ModelFormatFactory
    {
        /// <summary>
        /// 模型格式类型字典，键为模型名称，值为对应的类型
        /// </summary>
        private static Dictionary<string, Type>? _modelFormats;

        /// <summary>
        /// 线程同步锁，用于保护字典初始化
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 初始化标志，防止并发初始化时重复创建
        /// </summary>
        private static bool _isInitializing = false;

        /// <summary>
        /// 获取模型格式类型字典（延迟初始化）
        /// 
        /// 使用双重检查锁定模式确保线程安全的延迟初始化：
        /// 1. 第一次检查：无锁检查，提高性能
        /// 2. 加锁
        /// 3. 第二次检查：确保在等待锁的期间没有被其他线程初始化
        /// </summary>
        private static Dictionary<string, Type> ModelFormats
        {
            get
            {
                // 第一次检查：快速路径，无锁
                if (_modelFormats == null)
                {
                    // 加锁确保线程安全
                    lock (_lock)
                    {
                        // 第二次检查：确保在等待锁期间没有被初始化
                        if (_modelFormats == null && !_isInitializing)
                        {
                            // 设置初始化标志，防止其他线程重复初始化
                            _isInitializing = true;
                            try
                            {
                                // 创建并初始化模型格式字典
                                var formats = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                                {
                                    // Qwen系列模型
                                    { "Qwen", typeof(QwenModelFormat) },
                                    { "Qwen-7B", typeof(QwenModelFormat) },
                                    { "Qwen-14B", typeof(QwenModelFormat) },
                                    { "Qwen-7B-Chat", typeof(QwenModelFormat) },
                                    { "Qwen-7B-Chat-Q4_K_M", typeof(QwenModelFormat) },
                                    
                                    // Hermes系列模型（基于Llama-3）
                                    { "Hermes", typeof(HermesModelFormat) },
                                    { "Hermes-2-Pro", typeof(HermesModelFormat) },
                                    { "Hermes-2-Pro-Llama-3-8B", typeof(HermesModelFormat) },
                                    { "Hermes-2-Pro-Llama-3-8B.Q4_K_M", typeof(HermesModelFormat) },
                                    { "Hermes-2-Pro-Llama-3-8B.Q4_K_M.gguf", typeof(HermesModelFormat) },
                                };
                                _modelFormats = formats;
                                Console.WriteLine("ModelFormatFactory 模型格式字典初始化成功");
                            }
                            catch (Exception ex)
                            {
                                // 初始化失败时清空字典，抛出异常
                                _modelFormats = null;
                                Console.WriteLine($"ModelFormatFactory 初始化失败: {ex.Message}");
                                throw;
                            }
                            finally
                            {
                                // 重置初始化标志
                                _isInitializing = false;
                            }
                        }
                    }
                }
                return _modelFormats;
            }
        }

        /// <summary>
        /// 根据模型名称获取对应的模型格式实现
        /// 
        /// 匹配策略：
        /// 1. 如果模型名称为空，返回默认的Qwen格式
        /// 2. 首先尝试精确匹配字典中的模型名称
        /// 3. 如果精确匹配失败，尝试关键字模糊匹配（检查模型名称是否包含字典中的关键字）
        /// 4. 如果都匹配失败，返回默认的Qwen格式
        /// </summary>
        /// <param name="modelName">模型名称，如 "Qwen-7B-Chat-Q4_K_M" 或 "Hermes-2-Pro-Llama-3-8B"</param>
        /// <returns>对应的模型格式实现实例</returns>
        public static IModelFormat GetFormat(string modelName)
        {
            // 如果模型名称为空，返回默认格式
            if (string.IsNullOrWhiteSpace(modelName))
            {
                Console.WriteLine("模型名称为空，使用默认Qwen格式");
                return new QwenModelFormat();
            }

            // 尝试精确匹配
            if (ModelFormats.TryGetValue(modelName, out var formatType))
            {
                Console.WriteLine($"找到精确匹配的模型格式: {modelName} -> {formatType.Name}");
                return (IModelFormat)Activator.CreateInstance(formatType);
            }

            // 尝试关键字模糊匹配
            foreach (var (key, type) in ModelFormats)
            {
                if (modelName.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"找到模糊匹配的模型格式: {modelName} 包含 {key} -> {type.Name}");
                    return (IModelFormat)Activator.CreateInstance(type);
                }
            }

            // 都匹配失败，返回默认格式
            Console.WriteLine($"未找到匹配的模型格式: {modelName}，使用默认Qwen格式");
            return new QwenModelFormat();
        }

        /// <summary>
        /// 获取所有支持的模型格式名称列表
        /// </summary>
        /// <returns>支持的模型名称列表</returns>
        public static List<string> GetSupportedModels()
        {
            return new List<string>(ModelFormats.Keys);
        }
    }
}
