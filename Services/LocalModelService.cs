using System;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace ChatAI.Services
{
    /// <summary>
    /// 本地模型服务类（LocalModelService）
    /// 
    /// 该类负责管理本地大语言模型的加载、推理和资源释放。
    /// 支持NVIDIA CUDA和AMD Vulkan两种GPU加速模式，以及纯CPU模式。
    /// 
    /// 主要功能：
    /// 1. GPU类型检测和显存检测（带WMI缓存优化）
    /// 2. 模型初始化和参数配置
    /// 3. GPU层数计算和上下文窗口设置
    /// 4. 响应生成和流式输出
    /// 5. 资源释放和错误处理
    /// 
    /// 依赖库：LLamaSharp（用于本地模型推理）
    /// </summary>
    public class LocalModelService
    {
        /// <summary>
        /// 模型权重对象，包含加载的模型参数
        /// </summary>
        private LLamaWeights? _model;

        /// <summary>
        /// 推理上下文对象，用于管理模型推理状态
        /// </summary>
        private LLamaContext? _context;

        /// <summary>
        /// 交互式执行器，用于生成响应
        /// </summary>
        private InteractiveExecutor? _executor;

        /// <summary>
        /// 模型是否已初始化的标志
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// 线程同步锁，保护初始化和资源释放操作
        /// </summary>
        private readonly object _lock = new object();
        
        #region WMI缓存优化

        /// <summary>
        /// 缓存的GPU类型
        /// </summary>
        private static string? _cachedGpuType;

        /// <summary>
        /// 缓存的总显存大小（MB）
        /// </summary>
        private static long _cachedTotalVram = -1;

        /// <summary>
        /// 上次缓存GPU类型的时间
        /// </summary>
        private static DateTime _lastGpuTypeCacheTime = DateTime.MinValue;

        /// <summary>
        /// 上次缓存显存大小的时间
        /// </summary>
        private static DateTime _lastVramCacheTime = DateTime.MinValue;

        /// <summary>
        /// 缓存有效期（秒），设置为5分钟
        /// </summary>
        private const int CacheDurationSeconds = 300;
        
        /// <summary>
        /// 检查缓存是否有效
        /// </summary>
        /// <param name="lastCacheTime">上次缓存时间</param>
        /// <returns>缓存是否有效</returns>
        private static bool IsCacheValid(DateTime lastCacheTime)
        {
            return (DateTime.Now - lastCacheTime).TotalSeconds < CacheDurationSeconds;
        }
        
        #endregion

        /// <summary>
        /// 检测系统中的显卡类型
        /// 
        /// 检测优先级: NVIDIA > AMD > CPU
        /// 首先通过WMI查询Win32_VideoController获取显卡信息，
        /// 如果WMI查询失败，尝试通过环境变量检测。
        /// 
        /// 支持缓存机制，避免重复查询WMI。
        /// </summary>
        /// <returns>检测到的显卡类型（NVIDIA/AMD/CPU）</returns>
        public static string DetectGpuType()
        {
            // 先检查缓存是否有效
            lock (typeof(LocalModelService))
            {
                if (IsCacheValid(_lastGpuTypeCacheTime) && _cachedGpuType != null)
                {
                    Console.WriteLine($"使用缓存的GPU类型: {_cachedGpuType}");
                    return _cachedGpuType;
                }
            }
            
            string result = "CPU";
            
            try
            {
                // 使用WMI查询显卡信息
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                
                foreach (var obj in searcher.Get())
                {
                    string adapterName = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                    Console.WriteLine($"检测到显卡: {adapterName}");
                    
                    // 优先检测NVIDIA
                    if (adapterName.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine("检测到NVIDIA显卡，优先使用CUDA加速");
                        result = "NVIDIA";
                        break;
                    }
                    
                    // 其次检测AMD
                    if (adapterName.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        adapterName.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine("检测到AMD显卡，使用Vulkan加速");
                        result = "AMD";
                        break;
                    }
                }
            }
            catch (ManagementException ex)
            {
                // WMI查询失败时尝试环境变量检测
                Console.WriteLine($"WMI查询失败（Win32_VideoController）: {ex.Message}");
                Console.WriteLine("尝试使用环境变量检测显卡类型...");
                
                string gpuEnv = Environment.GetEnvironmentVariable("NVIDIA_VISIBLE_DEVICES");
                if (!string.IsNullOrWhiteSpace(gpuEnv))
                {
                    Console.WriteLine("通过环境变量检测到NVIDIA显卡");
                    result = "NVIDIA";
                }
                else
                {
                    gpuEnv = Environment.GetEnvironmentVariable("AMD_VISIBLE_DEVICES");
                    if (!string.IsNullOrWhiteSpace(gpuEnv))
                    {
                        Console.WriteLine("通过环境变量检测到AMD显卡");
                        result = "AMD";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测显卡类型失败: {ex.Message}");
            }
            
            // 更新缓存
            lock (typeof(LocalModelService))
            {
                _cachedGpuType = result;
                _lastGpuTypeCacheTime = DateTime.Now;
            }
            
            Console.WriteLine($"检测到GPU类型: {result}");
            return result;
        }

        /// <summary>
        /// 获取系统显存大小（MB）
        /// 
        /// 同步包装方法，调用异步版本并阻塞等待结果。
        /// 使用Task.Run避免死锁。
        /// </summary>
        /// <returns>显存大小（MB），如果无法检测返回0</returns>
        public static long GetGpuMemoryMB()
        {
            return Task.Run(async () => await GetGpuMemoryMBAsync()).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步获取显存大小（MB），带超时机制和缓存
        /// 
        /// 通过WMI查询Win32_VideoController获取显卡的AdapterRAM属性，
        /// 转换为MB单位。支持5分钟缓存，避免重复查询。
        /// 
        /// 设置2秒超时，避免WMI查询长时间阻塞。
        /// </summary>
        /// <returns>显存大小（MB），如果无法检测返回0</returns>
        public static async Task<long> GetGpuMemoryMBAsync()
        {
            // 检查缓存
            lock (typeof(LocalModelService))
            {
                if (IsCacheValid(_lastVramCacheTime) && _cachedTotalVram >= 0)
                {
                    Console.WriteLine($"使用缓存的显存大小: {_cachedTotalVram} MB");
                    return _cachedTotalVram;
                }
            }
            
            long result = 0;
            
            try
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                        
                        foreach (var obj in searcher.Get())
                        {
                            string adapterName = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                            
                            // 只考虑NVIDIA和AMD显卡
                            if (adapterName.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                adapterName.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                adapterName.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                object? vramObj = obj["AdapterRAM"];
                                if (vramObj != null && long.TryParse(vramObj.ToString(), out long vramBytes))
                                {
                                    long vramMB = vramBytes / (1024 * 1024);
                                    Console.WriteLine($"检测到显存: {vramMB} MB");
                                    return vramMB;
                                }
                            }
                        }
                    }
                    catch (ManagementException ex)
                    {
                        Console.WriteLine($"WMI查询失败（Win32_VideoController）: {ex.Message}");
                        Console.WriteLine("WMI查询失败，将使用用户设置的内存限制");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"检测显存大小失败: {ex.Message}");
                    }
                    return 0L;
                });

                // 设置2秒超时
                if (await Task.WhenAny(task, Task.Delay(2000)) == task)
                {
                    result = task.Result;
                }
                else
                {
                    Console.WriteLine("WMI查询超时（2秒），跳过显存检测");
                    result = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"异步检测显存失败: {ex.Message}");
                result = 0;
            }
            
            // 更新缓存（仅当检测成功时）
            lock (typeof(LocalModelService))
            {
                if (result > 0)
                {
                    _cachedTotalVram = result;
                    _lastVramCacheTime = DateTime.Now;
                }
            }
            
            return result;
        }

        /// <summary>
        /// 获取当前可用的GPU显存（MB）
        /// 
        /// 同步包装方法，调用异步版本并阻塞等待结果。
        /// </summary>
        /// <returns>可用显存大小（MB）</returns>
        public static long GetAvailableGpuMemoryMB()
        {
            return Task.Run(async () => await GetAvailableGpuMemoryMBAsync()).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步获取当前可用的GPU显存（MB），简化版本避免不可靠的WMI查询
        /// 
        /// 由于Win32_PerfFormattedData_Counters_GPUPerformanceCounters等WMI类在很多系统上不可用，
        /// 本方法使用简化策略：返回总显存的85%作为可用显存估计值。
        /// 
        /// 如果无法检测总显存，返回long.MaxValue表示无法确定。
        /// </summary>
        /// <returns>可用显存大小（MB）</returns>
        public static async Task<long> GetAvailableGpuMemoryMBAsync()
        {
            try
            {
                long totalVram = await GetGpuMemoryMBAsync();
                if (totalVram == 0)
                {
                    Console.WriteLine("未检测到显存，使用用户设置的内存限制");
                    return long.MaxValue;
                }

                // 使用总显存的85%作为可用显存估计
                long estimatedAvailable = (long)(totalVram * 0.85);
                Console.WriteLine($"估计可用显存: {estimatedAvailable} MB (总显存的85%)");
                return estimatedAvailable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测可用显存失败: {ex.Message}");
                return long.MaxValue;
            }
        }

        /// <summary>
        /// 获取模型是否已初始化的状态
        /// </summary>
        public bool IsInitialized 
        { 
            get 
            { 
                bool result = _isInitialized;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] IsInitialized属性被访问，返回: {result}");
                return result;
            } 
        }

        /// <summary>
        /// 初始化本地模型
        /// 
        /// 执行以下步骤：
        /// 1. 如果已初始化，先释放资源
        /// 2. 设置GPU后端环境变量（Vulkan模式）
        /// 3. 检测显存并计算GPU层数
        /// 4. 创建ModelParams并加载模型权重
        /// 5. 创建推理上下文和执行器
        /// 
        /// 支持NVIDIA CUDA和AMD Vulkan加速，以及纯CPU模式。
        /// </summary>
        /// <param name="modelPath">模型文件路径（.gguf格式）</param>
        /// <param name="gpuMode">GPU模式（NVIDIA/AMD/CPU）</param>
        /// <param name="memoryLimitMB">显存限制（MB）</param>
        /// <returns>是否初始化成功</returns>
        public bool Initialize(string modelPath, string gpuMode, int memoryLimitMB)
        {
            lock (_lock)
            {
                // 如果已初始化，先释放资源
                if (_isInitialized)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 模型已初始化，先释放资源...");
                    Dispose();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 资源释放完成");
                }

                try
                {
                    Console.WriteLine($"=== 本地模型初始化开始 ===");
                    Console.WriteLine($"时间: {DateTime.Now}");
                    Console.WriteLine($"模型路径: {modelPath}");
                    Console.WriteLine($"GPU模式: {gpuMode}");
                    Console.WriteLine($"内存限制: {memoryLimitMB} MB");

                    // 设置Vulkan后端环境变量
                    if (gpuMode == "Vulkan")
                    {
                        Console.WriteLine("设置环境变量强制使用Vulkan后端...");
                        Environment.SetEnvironmentVariable("LLAMA_BACKEND", "Vulkan");
                        Environment.SetEnvironmentVariable("LLAMA_VULKAN", "1");
                    }
                    
                    // 初始化Vulkan后端（非CPU模式）
                    if (gpuMode != "CPU 模式" && gpuMode != "CPU")
                    {
                        Console.WriteLine("尝试初始化Vulkan后端...");
                        InitializeVulkanBackend();
                    }

                    // 提取模型名称用于调整参数
                    string modelName = string.Empty;
                    if (!string.IsNullOrWhiteSpace(modelPath))
                    {
                        try
                        {
                            modelName = System.IO.Path.GetFileName(modelPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"获取模型文件名失败: {ex.Message}");
                            modelName = string.Empty;
                        }
                    }
                    
                    bool forceCpuMode = false;
                    long detectedVram = 0;
                    
                    // 检测显存（非CPU模式）
                    if ((gpuMode != "CPU 模式" && gpuMode != "CPU"))
                    {
                        detectedVram = GetGpuMemoryMB();
                        Console.WriteLine($"检测到显存: {detectedVram} MB");
                    }
                    
                    // 根据模型类型计算GPU层数
                    int gpuLayers = CalculateGpuLayers(gpuMode, memoryLimitMB, modelName, detectedVram);
                    Console.WriteLine($"计算的GPU层数: {gpuLayers}");

                    // 自动调整GPU层数
                    if ((gpuMode != "CPU 模式" && gpuMode != "CPU") && gpuLayers == 0)
                    {
                        gpuLayers = Math.Max(1, memoryLimitMB / 200);
                        Console.WriteLine($"自动调整GPU层数为: {gpuLayers}");
                    }
                    
                    // 根据模型类型设置上下文大小
                    int contextSize = 8192; // 默认8192（Qwen-7B）
                    
                    // Llama-3/Hermes-2模型建议上下文长度为4096
                    if (modelName.Contains("llama-3", StringComparison.OrdinalIgnoreCase) || 
                        modelName.Contains("hermes", StringComparison.OrdinalIgnoreCase))
                    {
                        contextSize = 4096;
                        Console.WriteLine($"检测到Llama-3/Hermes模型，设置上下文大小为: {contextSize}");
                    }
                    
                    // 检查可用显存并调整参数（非CPU模式）
                    if ((gpuMode != "CPU 模式" && gpuMode != "CPU"))
                    {
                        long availableVram = GetAvailableGpuMemoryMB();
                        Console.WriteLine($"当前可用显存: {availableVram} MB");
                        
                        // 只有当可用显存检测有效时才进行检查
                        if (availableVram != long.MaxValue)
                        {
                            if (availableVram < 512)
                            {
                                Console.WriteLine("警告：显存严重不足，强制切换到CPU模式");
                                forceCpuMode = true;
                                gpuLayers = 0;
                            }
                            else if (availableVram < 2048)
                            {
                                Console.WriteLine("警告：可用显存不足，减少GPU层数");
                                gpuLayers = Math.Max(0, gpuLayers - 4);
                                Console.WriteLine($"调整后GPU层数: {gpuLayers}");
                                
                                if (availableVram < 1024)
                                {
                                    Console.WriteLine("警告：显存不足，减少上下文窗口");
                                    contextSize = Math.Max(2048, contextSize / 2);
                                    Console.WriteLine($"调整后ContextSize: {contextSize}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("无法检测可用显存，跳过显存检查");
                        }
                    }
                    
                    if (forceCpuMode)
                    {
                        Console.WriteLine($"已强制切换到CPU模式");
                    }

                    Console.WriteLine($"创建ModelParams - ContextSize: {contextSize}, GpuLayerCount: {gpuLayers}");
                    
                    // 确保参数有效
                    uint contextSizeUint = (uint)Math.Max(1024, contextSize);
                    int gpuLayersInt = Math.Max(0, gpuLayers);
                    
                    // 创建模型参数
                    var parameters = new ModelParams(modelPath)
                    {
                        ContextSize = contextSizeUint,
                        GpuLayerCount = gpuLayersInt,
                    };
                    
                    Console.WriteLine($"ModelParams创建成功 - ContextSize: {parameters.ContextSize}, GpuLayerCount: {parameters.GpuLayerCount}");
                    Console.WriteLine($"用户设置的显存限制: {memoryLimitMB} MB");

                    Console.WriteLine($"LLamaSharp版本: {typeof(LLamaWeights).Assembly.GetName().Version}");
                    Console.WriteLine($"ContextSize: {parameters.ContextSize}");
                    Console.WriteLine($"GpuLayerCount: {parameters.GpuLayerCount}");
                    Console.WriteLine($"开始加载模型权重...");

                    try
                    {
                        // 加载模型权重
                        DateTime startTime = DateTime.Now;
                        _model = LLamaWeights.LoadFromFile(parameters);
                        TimeSpan loadTime = DateTime.Now - startTime;
                        Console.WriteLine($"模型权重加载完成，耗时: {loadTime.TotalSeconds:F2}秒");
                        
                        // 检查GPU使用情况
                        CheckGpuUsage();
                    }
                    catch (LLama.Exceptions.LoadWeightsFailedException ex)
                    {
                        // 模型加载失败，输出详细错误信息
                        Console.WriteLine($"=== 模型加载失败 ===");
                        Console.WriteLine($"错误类型: LoadWeightsFailedException");
                        Console.WriteLine($"错误消息: {ex.Message}");
                        Console.WriteLine($"可能的原因:");
                        Console.WriteLine($"  1. 模型文件路径不正确");
                        Console.WriteLine($"  2. 模型文件格式不正确（必须是 .gguf 格式）");
                        Console.WriteLine($"  3. 模型文件损坏或不完整");
                        Console.WriteLine($"  4. 缺少必要的原生库（如 CUDA/Vulkan）");
                        Console.WriteLine($"====================");
                        throw;
                    }

                    // 创建推理上下文
                    _context = _model.CreateContext(parameters);
                    Console.WriteLine("上下文创建完成");

                    // 创建执行器
                    _executor = new InteractiveExecutor(_context);
                    Console.WriteLine("执行器创建完成");

                    // 标记初始化完成
                    _isInitialized = true;
                    Console.WriteLine("本地模型初始化成功");
                    Console.WriteLine($"GPU层数量: {gpuLayers} (0=纯CPU模式)");
                    Console.WriteLine($"是否使用GPU加速: {gpuLayers > 0}");
                    Console.WriteLine($"====================");

                    return true;
                }
                catch (Exception ex)
                {
                    // 初始化失败，输出详细错误信息并释放资源
                    Console.WriteLine($"=== 本地模型初始化失败 ===");
                    Console.WriteLine($"错误类型: {ex.GetType().Name}");
                    Console.WriteLine($"错误消息: {ex.Message}");
                    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                    Console.WriteLine($"====================");
                    Dispose();
                    return false;
                }
            }
        }

        /// <summary>
        /// 初始化Vulkan后端
        /// 
        /// 尝试查找并初始化LLamaSharp.Backend.Vulkan程序集，
        /// 设置Vulkan作为推理后端。如果初始化失败，LLamaSharp会自动回退到CPU模式。
        /// </summary>
        private void InitializeVulkanBackend()
        {
            try
            {
                Console.WriteLine("=== Vulkan后端初始化 ===");
                
                Console.WriteLine("已加载的LLamaSharp程序集:");
                bool foundLlamaSharp = false;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name = assembly.GetName().Name ?? "未知";
                    if (name.Contains("LLama"))
                    {
                        Console.WriteLine($"  - {name} ({assembly.GetName().Version})");
                        if (name.Contains("Backend.Vulkan"))
                        {
                            foundLlamaSharp = true;
                        }
                    }
                }
                
                if (!foundLlamaSharp)
                {
                    Console.WriteLine("警告：未找到LLamaSharp.Backend.Vulkan程序集");
                    Console.WriteLine("LLamaSharp会自动回退到CPU模式");
                    Console.WriteLine($"====================");
                    return;
                }
                
                // 查找Vulkan后端程序集
                var vulkanAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name?.Contains("LLamaSharp.Backend.Vulkan") ?? false);
                
                if (vulkanAssembly != null)
                {
                    Console.WriteLine($"找到Vulkan后端程序集: {vulkanAssembly.GetName().Name}, 版本: {vulkanAssembly.GetName().Version}");
                    
                    // 尝试多种可能的类型名称
                    var backendType = vulkanAssembly.GetType("LLama.Backend.Vulkan.Backend");
                    if (backendType == null)
                    {
                        backendType = vulkanAssembly.GetType("LLamaSharp.Backend.Vulkan.Backend");
                    }
                    
                    if (backendType != null)
                    {
                        Console.WriteLine($"找到后端类型: {backendType.FullName}");
                        
                        // 尝试查找Initialize方法
                        var initializeMethod = backendType.GetMethod("Initialize", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (initializeMethod == null)
                        {
                            initializeMethod = backendType.GetMethod("Initialize", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        }
                        
                        if (initializeMethod != null)
                        {
                            Console.WriteLine("找到Initialize方法，尝试调用...");
                            try
                            {
                                initializeMethod.Invoke(null, null);
                                Console.WriteLine("Vulkan后端初始化成功");
                            }
                            catch (Exception invokeEx)
                            {
                                Console.WriteLine($"调用Initialize方法失败: {invokeEx.Message}");
                                Console.WriteLine("注意：LLamaSharp通常会自动选择后端，无需手动初始化");
                            }
                        }
                        else
                        {
                            Console.WriteLine("未找到Initialize方法");
                            Console.WriteLine("注意：LLamaSharp可能不需要手动初始化后端");
                        }
                    }
                    else
                    {
                        Console.WriteLine("未找到Vulkan后端类型");
                        Console.WriteLine("尝试查找Vulkan相关类型...");
                        var vulkanTypes = vulkanAssembly.GetTypes().Where(t => 
                            t.FullName != null && t.FullName.Contains("Vulkan"));
                        foreach (var type in vulkanTypes.Take(5))
                        {
                            Console.WriteLine($"  - {type.FullName}");
                        }
                        Console.WriteLine("LLamaSharp会自动选择可用的后端");
                    }
                }
                else
                {
                    Console.WriteLine("未找到Vulkan后端程序集");
                }
                
                Console.WriteLine($"====================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化Vulkan后端失败: {ex.Message}");
                Console.WriteLine($"异常类型: {ex.GetType().Name}");
                Console.WriteLine("注意：LLamaSharp会自动回退到CPU模式，程序可以继续运行");
            }
        }

        /// <summary>
        /// 检查GPU使用情况
        /// 
        /// 尝试检测CUDA和Vulkan后端是否可用，输出调试信息。
        /// </summary>
        private void CheckGpuUsage()
        {
            try
            {
                Console.WriteLine("=== GPU使用情况检查 ===");
                
                var backendTypes = new[] { "LLama.Backend.Cuda12.Backend", "LLama.Backend.Vulkan.Backend" };
                
                foreach (var typeName in backendTypes)
                {
                    try
                    {
                        var backendType = Type.GetType(typeName);
                        if (backendType != null)
                        {
                            var isAvailableMethod = backendType.GetMethod("IsAvailable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (isAvailableMethod != null)
                            {
                                var result = isAvailableMethod.Invoke(null, null);
                                Console.WriteLine($"{typeName} 可用: {result}");
                            }
                        }
                    }
                    catch { }
                }
                
                Console.WriteLine($"GPU层数配置: {(_model != null ? "已加载" : "未加载")}");
                Console.WriteLine($"====================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查GPU使用情况失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据显存和模型类型计算GPU层数
        /// 
        /// GPU层数决定了有多少层模型参数加载到GPU显存中，
        /// 剩余层将在CPU内存中运行。更多的GPU层可以提高推理速度，
        /// 但需要更多显存。
        /// 
        /// 计算逻辑：
        /// 1. CPU模式返回0
        /// 2. 根据模型类型确定每层显存系数（Llama-3/Hermes为130MB，其他为100MB）
        /// 3. 使用显存限制除以每层系数得到层数
        /// 4. 根据模型大小限制最大层数
        /// </summary>
        /// <param name="gpuMode">GPU模式</param>
        /// <param name="memoryLimitMB">显存限制（MB）</param>
        /// <param name="modelName">模型名称（用于识别模型类型）</param>
        /// <param name="preDetectedVram">预检测的显存值（可选）</param>
        /// <returns>计算的GPU层数</returns>
        private int CalculateGpuLayers(string gpuMode, int memoryLimitMB, string modelName = "", long? preDetectedVram = null)
        {
            // CPU模式直接返回0
            if (gpuMode == "CPU 模式" || gpuMode == "CPU")
            {
                Console.WriteLine("CPU模式: 禁用GPU加速");
                return 0;
            }

            // 使用预检测的显存值或重新检测
            long detectedVram = preDetectedVram ?? GetGpuMemoryMB();
            Console.WriteLine($"检测到显存: {detectedVram} MB");
            
            const int minReliableVram = 4096;
            
            // 处理显存检测结果
            if (detectedVram > minReliableVram && memoryLimitMB > detectedVram)
            {
                Console.WriteLine($"警告: 用户设置的内存限制 ({memoryLimitMB} MB) 超过检测到的显存 ({detectedVram} MB)");
                Console.WriteLine($"将使用显存值作为内存限制");
                memoryLimitMB = (int)detectedVram;
            }
            else if (detectedVram > 0 && detectedVram <= minReliableVram)
            {
                Console.WriteLine($"检测到的显存 ({detectedVram} MB) 可能不准确（常见于WMI限制）");
                Console.WriteLine($"继续使用用户设置的内存限制 ({memoryLimitMB} MB)");
            }
            else if (detectedVram == 0)
            {
                Console.WriteLine("无法检测到显存大小，使用用户设置的内存限制");
            }
            
            long usableMemory = memoryLimitMB;
            Console.WriteLine($"=== 显存分配详细日志 ===");
            Console.WriteLine($"用户设置的内存限制: {memoryLimitMB} MB");
            Console.WriteLine($"可用内存: {usableMemory} MB");
            
            // 根据模型类型设置每层显存系数
            int memoryPerLayer = 100;
            
            if (!string.IsNullOrEmpty(modelName) && 
                (modelName.Contains("llama-3", StringComparison.OrdinalIgnoreCase) || 
                 modelName.Contains("hermes", StringComparison.OrdinalIgnoreCase)))
            {
                memoryPerLayer = 130;
                Console.WriteLine($"检测到Llama-3/Hermes模型，调整每层显存系数为: {memoryPerLayer} MB");
            }
            else
            {
                Console.WriteLine($"使用默认每层显存系数: {memoryPerLayer} MB");
            }
            
            // 计算GPU层数
            int layers = (int)(usableMemory / memoryPerLayer);
            Console.WriteLine($"初步计算GPU层数: {usableMemory} / {memoryPerLayer} = {layers}");
            
            // 根据模型大小限制最大层数
            int maxLayers = GetMaxLayersForModel(modelName);
            if (layers > maxLayers)
            {
                Console.WriteLine($"计算的层数 ({layers}) 超过模型最大层数 ({maxLayers})，限制为最大层数");
                layers = maxLayers;
            }
            
            // 确保至少有一层（如果有足够显存）
            if (usableMemory >= memoryPerLayer && layers == 0)
            {
                layers = 1;
                Console.WriteLine("强制设置GPU层数为1");
            }
            
            Console.WriteLine($"最终GPU层数: {layers}");
            Console.WriteLine($"预估显存使用: {layers * memoryPerLayer} MB");
            Console.WriteLine($"===========================");
            return layers;
        }
        
        /// <summary>
        /// 根据模型名称获取最大层数
        /// 
        /// 7B模型通常有32层，13B模型有40层。
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <returns>最大层数</returns>
        private int GetMaxLayersForModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return 64;
            
            if (modelName.Contains("7b", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("7B", StringComparison.OrdinalIgnoreCase))
            {
                return 32;
            }
            else if (modelName.Contains("13b", StringComparison.OrdinalIgnoreCase) ||
                     modelName.Contains("13B", StringComparison.OrdinalIgnoreCase))
            {
                return 40;
            }
            else if (modelName.Contains("llama-3", StringComparison.OrdinalIgnoreCase) ||
                     modelName.Contains("hermes", StringComparison.OrdinalIgnoreCase))
            {
                return 32;
            }
            
            return 64;
        }

        /// <summary>
        /// 生成响应（简化版本）
        /// </summary>
        /// <param name="prompt">输入的Prompt</param>
        /// <param name="maxTokens">最大生成token数</param>
        /// <param name="temperature">温度参数</param>
        /// <returns>生成的响应</returns>
        public async Task<string?> GenerateResponseAsync(string prompt, int maxTokens = 512, float temperature = 0.7f)
        {
            return await GenerateResponseAsync(prompt, maxTokens, temperature, TimeSpan.FromMinutes(5), null);
        }

        /// <summary>
        /// 生成响应（完整版本）
        /// 
        /// 使用交互式执行器生成响应，支持流式输出和停止词检测。
        /// 推理完成后自动释放显存资源。
        /// 
        /// 主要功能：
        /// 1. 参数验证和默认值设置
        /// 2. 重新创建执行器以清空KV缓存
        /// 3. 流式推理并检测重复内容
        /// 4. 检测停止词并提前终止
        /// 5. 处理超时和错误
        /// 6. 推理完成后释放资源
        /// </summary>
        /// <param name="prompt">输入的Prompt</param>
        /// <param name="maxTokens">最大生成token数</param>
        /// <param name="temperature">温度参数（未使用，保留接口兼容性）</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="stopWords">停止词数组</param>
        /// <param name="attempt">重试次数（内部使用）</param>
        /// <returns>生成的响应</returns>
        public async Task<string?> GenerateResponseAsync(string prompt, int maxTokens, float temperature, TimeSpan timeout, string[]? stopWords = null, int attempt = 1)
        {
            // 检查模型是否已初始化
            if (!_isInitialized || _executor == null || _context == null)
            {
                Console.WriteLine("本地模型尚未初始化或已失效");
                throw new InvalidOperationException("本地模型尚未初始化");
            }

            // 验证Prompt
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.WriteLine("警告：Prompt为空或空白！");
                throw new InvalidOperationException("Prompt不能为空");
            }

            // 参数验证
            if (maxTokens <= 0)
            {
                Console.WriteLine($"警告：MaxTokens值无效 ({maxTokens})，使用默认值512");
                maxTokens = 512;
            }

            if (timeout <= TimeSpan.Zero)
            {
                Console.WriteLine($"警告：超时时间无效，使用默认值5分钟");
                timeout = TimeSpan.FromMinutes(5);
            }

            try
            {
                Console.WriteLine($"开始生成响应，Prompt长度: {prompt.Length}");
                Console.WriteLine($"超时时间: {timeout.TotalMinutes} 分钟, MaxTokens: {maxTokens}");

                // 使用锁保护执行器创建
                InteractiveExecutor currentExecutor;
                lock (_lock)
                {
                    Console.WriteLine("重新创建执行器以清空KV缓存...");
                    currentExecutor = new InteractiveExecutor(_context);
                    _executor = currentExecutor;
                    Console.WriteLine("执行器创建成功");
                }

                // 使用传入的停止词
                string[] effectiveStopWords = stopWords ?? Array.Empty<string>();
                if (effectiveStopWords.Length == 0)
                {
                    Console.WriteLine("警告：停止词数组为空");
                }

                // 创建推理参数
                var inferenceParams = new InferenceParams
                {
                    MaxTokens = maxTokens,
                    AntiPrompts = effectiveStopWords
                };

                string response = string.Empty;
                var cts = new System.Threading.CancellationTokenSource(timeout);
                int consecutiveRepeatCount = 0;
                string lastChunk = string.Empty;

                try
                {
                    // 流式推理
                    await foreach (var text in currentExecutor.InferAsync(prompt, inferenceParams).WithCancellation(cts.Token))
                    {
                        string currentChunk = text;
                        
                        // 检测连续重复的chunk
                        if (currentChunk == lastChunk)
                        {
                            consecutiveRepeatCount++;
                            if (consecutiveRepeatCount >= 5)
                            {
                                Console.WriteLine("检测到连续重复的chunk，提前终止生成");
                                break;
                            }
                        }
                        else
                        {
                            consecutiveRepeatCount = 0;
                            lastChunk = currentChunk;
                        }
                        
                        response += currentChunk;
                        
                        // 检查是否遇到停止词
                        foreach (var pattern in effectiveStopWords)
                        {
                            if (response.Contains(pattern))
                            {
                                Console.WriteLine($"检测到停止词 '{pattern}'，提前终止");
                                // 截断到停止词之前
                                int idx = response.IndexOf(pattern);
                                if (idx > 0)
                                {
                                    response = response.Substring(0, idx);
                                }
                                goto endGeneration;
                            }
                        }
                    }
                endGeneration:
                    Console.WriteLine("响应生成循环结束");
                }
                catch (System.OperationCanceledException)
                {
                    Console.WriteLine($"响应生成超时（超过 {timeout.TotalMinutes} 分钟）");
                    return response.Length > 0 ? response.Trim() : null;
                }

                Console.WriteLine($"响应生成完成，长度: {response.Length}");
                
                // 处理空响应（重试机制）
                if (string.IsNullOrWhiteSpace(response))
                {
                    Console.WriteLine($"响应为空检测：attempt={attempt}");
                    
                    if (attempt < 3)
                    {
                        Console.WriteLine($"警告：响应为空（尝试{attempt}），等待后重试...");
                        await Task.Delay(200);
                        return await GenerateResponseAsync(prompt, maxTokens, temperature, timeout, stopWords, attempt + 1);
                    }
                    else
                    {
                        Console.WriteLine($"警告：响应为空，已重试{attempt}次，放弃");
                    }
                }
                
                string result = response.Trim();
                
                // 推理完成后释放显存资源
                Console.WriteLine("=== 推理完成，释放显存资源 ===");
                Dispose();
                Console.WriteLine("显存资源已释放");
                
                return result;
            }
            catch (LLama.Exceptions.LLamaDecodeError ex)
            {
                // 解码错误通常是显存不足或模型损坏
                Console.WriteLine($"=== LLama解码错误 ===");
                Console.WriteLine($"错误消息: {ex.Message}");
                Console.WriteLine($"这通常是由于显存不足、上下文溢出或模型损坏导致的");
                Console.WriteLine("触发模型重新初始化...");
                Console.WriteLine($"====================");
                
                // 清理资源并标记未初始化
                lock (_lock)
                {
                    Dispose();
                    _isInitialized = false;
                }
                
                // 抛出异常让调用者处理重新初始化
                throw new InvalidOperationException("本地模型解码失败，需要重新初始化", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== 响应生成失败 ===");
                Console.WriteLine($"错误类型: {ex.GetType().Name}");
                Console.WriteLine($"错误消息: {ex.Message}");
                Console.WriteLine($"====================");
                lock (_lock)
                {
                    Dispose();
                }
                throw new InvalidOperationException("本地模型连接已断开，请重新初始化", ex);
            }
        }

        /// <summary>
        /// 释放所有资源
        /// 
        /// 按顺序释放：执行器 -> 上下文 -> 模型权重
        /// 并标记为未初始化状态。
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_executor != null)
                {
                    _executor = null;
                    Console.WriteLine("执行器已释放");
                }
                
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                    Console.WriteLine("上下文已释放");
                }
                
                if (_model != null)
                {
                    _model.Dispose();
                    _model = null;
                    Console.WriteLine("模型权重已释放");
                }
                
                _isInitialized = false;
                Console.WriteLine("本地模型服务已完全释放");
            }
        }

        /// <summary>
        /// 验证模型文件是否有效
        /// 
        /// 检查文件是否存在且扩展名为.gguf。
        /// </summary>
        /// <param name="modelPath">模型文件路径</param>
        /// <returns>是否有效</returns>
        public static bool ValidateModelFile(string modelPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelPath))
                    return false;
                
                if (!System.IO.File.Exists(modelPath))
                    return false;
                
                string extension = System.IO.Path.GetExtension(modelPath).ToLower();
                return extension == ".gguf";
            }
            catch
            {
                return false;
            }
        }
    }
}
