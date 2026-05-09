using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAI.Data;
using System.IO;
using System.Linq;

namespace ChatAI.UI.Forms
{
    public partial class FrmApiConfig : Form
    {
        // 用来防止窗体加载时自动触发请求
        private bool _formLoaded = false;

        // 所有主流服务商 → 自动填接口地址
        private readonly Dictionary<string, string> _providerBaseUrls = new Dictionary<string, string>
        {
            { "豆包（火山方舟）", "https://ark.cn-beijing.volces.com/api/v3" },
            { "通义千问（阿里）", "https://dashscope.aliyuncs.com/compatible-mode/v1" },
            { "DeepSeek", "https://api.deepseek.com/v1" },
            { "硅基流动", "https://api.siliconflow.cn/v1" },
            { "智谱GLM", "https://open.bigmodel.cn/api/paas/v4" },
            { "Moonshot（Kimi）", "https://api.moonshot.cn/v1" },
            { "零一万物", "https://api.01.ai/v1" },
            { "百度千帆", "https://qianfan.baidubce.com/v2" },
            { "腾讯混元", "https://hunyuan.cloud.tencent.com/v1" },
            { "讯飞星火", "https://spark-api.xf-yun.com/v1" },
            { "百川智能", "https://api.baichuan-ai.com/v1" }
        };
        private readonly string _loginUsername;
        private readonly UserRepository _userRepo = new UserRepository();

        public FrmApiConfig(string loginUsername)
        {
            InitializeComponent();
            _loginUsername = loginUsername;
        }
        private void FrmApiConfig_Load(object? sender, EventArgs e)
        {
            // 加载服务商列表
            cboProvider.Items.Clear();
            foreach (var provider in _providerBaseUrls.Keys)
            {
                cboProvider.Items.Add(provider);
            }

            // API Key 默认星号
            txtApiKey.PasswordChar = '*';
            btnShowApiKey.Text = "显示密钥";

            // 初始化本地模型配置
            InitializeLocalModelConfig();

            // 加载用户的API配置
            LoadUserApiConfig();

            _formLoaded = true;
        }

        /// <summary>
        /// 初始化本地模型配置
        /// </summary>
        private void InitializeLocalModelConfig()
        {
            // 复选框默认为不选中
            chk_EnableLocalModel.Checked = false;

            // 禁用本地模型配置区域的控件
            SetGroupBoxEnabled(groupBox_LocalModelConfig, false);

            // 启用云端API配置区域的控件
            SetGroupBoxEnabled(groupBox_CloudApiConfig, true);

            // 填充硬件加速模式下拉框
            cbo_GpuAccelMode.Items.Clear();
            cbo_GpuAccelMode.Items.AddRange(new string[] {
                "自动选择（推荐）",
                "CPU 模式",
                "CUDA 12",
                "Vulkan"
            });
            cbo_GpuAccelMode.SelectedIndex = 0;

            // 设置显存/内存上限控件的步长为1000
            numericUpDown1.Increment = 1000;
            numericUpDown1.Minimum = 0;
            numericUpDown1.Maximum = GetMaxMemoryLimit();
        }

        /// <summary>
        /// 设置GroupBox内所有控件的启用状态
        /// </summary>
        private void SetGroupBoxEnabled(GroupBox groupBox, bool enabled)
        {
            foreach (Control control in groupBox.Controls)
            {
                control.Enabled = enabled;
            }
        }

        /// <summary>
        /// 获取最大内存限制（根据系统内存）
        /// </summary>
        private decimal GetMaxMemoryLimit()
        {
            // 获取系统物理内存（以MB为单位）
            ulong totalMemoryBytes = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            int totalMemoryMB = (int)(totalMemoryBytes / (1024 * 1024));
            
            // 返回90%作为上限
            return (decimal)(totalMemoryMB * 0.9);
        }

        /// <summary>
        /// 获取系统显存大小（MB）
        /// 使用注册表快速查询，避免耗时的WMI查询
        /// </summary>
        /// <returns>显存大小（MB），如果无法检测返回16384（16GB）</returns>
        private long GetGpuMemoryMB()
        {
            // 优先从注册表查询NVIDIA显卡显存
            long nvidiaVram = GetNvidiaGpuMemory();
            if (nvidiaVram >= 1024)
                return nvidiaVram;
            
            // 查询AMD显卡显存
            long amdVram = GetAmdGpuMemory();
            if (amdVram >= 1024)
                return amdVram;
            
            // 默认返回16GB（现代显卡常见配置）
            // 避免使用耗时的WMI查询，直接返回合理默认值
            return 16384;
        }

        /// <summary>
        /// 从注册表获取NVIDIA显卡显存
        /// </summary>
        private long GetNvidiaGpuMemory()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\Device0"))
                {
                    if (key != null)
                    {
                        // 尝试获取不同的显存相关值
                        object? regValue = key.GetValue("HardwareInformation.QwordMemorySize");
                        if (regValue != null)
                        {
                            if (regValue is ulong size)
                            {
                                return (long)(size / (1024 * 1024));
                            }
                        }
                        
                        // 尝试另一个键
                        regValue = key.GetValue("MemorySize");
                        if (regValue != null)
                        {
                            if (regValue is int intSize)
                                return (long)(intSize / (1024 * 1024));
                            if (regValue is ulong ulongSize)
                                return (long)(ulongSize / (1024 * 1024));
                        }
                    }
                }
            }
            catch { }
            
            return 0;
        }

        /// <summary>
        /// 从注册表获取AMD显卡显存
        /// </summary>
        private long GetAmdGpuMemory()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\amdkmdag\Device0"))
                {
                    if (key != null)
                    {
                        object? regValue = key.GetValue("HardwareInformation.QwordMemorySize");
                        if (regValue != null)
                        {
                            if (regValue is ulong size)
                            {
                                return (long)(size / (1024 * 1024));
                            }
                        }
                        
                        regValue = key.GetValue("MemorySize");
                        if (regValue != null)
                        {
                            if (regValue is int intSize)
                                return (long)(intSize / (1024 * 1024));
                            if (regValue is ulong ulongSize)
                                return (long)(ulongSize / (1024 * 1024));
                        }
                    }
                }
            }
            catch { }
            
            return 0;
        }

        /// <summary>
        /// 加载用户的API配置
        /// </summary>
        private void LoadUserApiConfig()
        {
            try
            {
                var apiConfig = _userRepo.LoadApiConfig(_loginUsername);

                if (apiConfig != null)
                {
                    // 设置本地模型启用状态
                    chk_EnableLocalModel.Checked = apiConfig.EnableLocalModel ?? false;

                    if (apiConfig.EnableLocalModel ?? false)
                    {
                        // 加载本地模型配置
                        txt_LocalModelPath.Text = apiConfig.Model ?? string.Empty;
                        txt_LocalModelPath.Tag = apiConfig.Endpoint;
                        cbo_GpuAccelMode.Text = apiConfig.ApiKey ?? "自动选择（推荐）";
                        numericUpDown1.Value = apiConfig.LocalModelMemoryLimit ?? 0;

                        // 更新控件状态
                        SetGroupBoxEnabled(groupBox_CloudApiConfig, false);
                        SetGroupBoxEnabled(groupBox_LocalModelConfig, true);
                    }
                    else
                    {
                        // 加载云端API配置
                        cboProvider.Text = apiConfig.Provider ?? string.Empty;
                        txtBaseUrl.Text = apiConfig.Endpoint ?? string.Empty;
                        txtApiKey.Text = apiConfig.ApiKey ?? string.Empty;
                        cboModel.Text = apiConfig.Model ?? string.Empty;

                        // 更新控件状态
                        SetGroupBoxEnabled(groupBox_CloudApiConfig, true);
                        SetGroupBoxEnabled(groupBox_LocalModelConfig, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载API配置失败：{ex.Message}");
            }
        }

        // 选择服务商 → 自动填地址
        private void cboProvider_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_formLoaded) return;
            if (cboProvider.SelectedItem == null) return;

            string selected = cboProvider.SelectedItem?.ToString() ?? "";
            if (!string.IsNullOrEmpty(selected))
            {
                if (_providerBaseUrls.TryGetValue(selected, out string baseUrl))
                {
                    txtBaseUrl.Text = baseUrl;
                }
                else
                {
                    txtBaseUrl.Clear();
                }
            }
            else
            {
                txtBaseUrl.Clear();
            }

            // 清空模型
            cboModel.Items.Clear();
            cboModel.Text = "";
        }

        // 核心：获取模型列表
        private async System.Threading.Tasks.Task LoadModelListAsync()
        {
            try
            {
                string baseUrl = txtBaseUrl.Text.TrimEnd('/');
                string apiKey = txtApiKey.Text.Trim();

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                string json = await client.GetStringAsync($"{baseUrl}/models");
                var root = JsonNode.Parse(json);
                var data = root?["data"]?.AsArray();

                cboModel.Items.Clear();
                if (data != null && data.Count > 0)
                {
                    foreach (var item in data)
                    {
                        string? modelId = item?["id"]?.ToString();
                        if (!string.IsNullOrEmpty(modelId))
                            cboModel.Items.Add(modelId);
                    }

                    if (cboModel.Items.Count > 0)
                    {
                        cboModel.SelectedIndex = 0;
                        return;
                    }
                }
                MessageBox.Show("该服务商暂未返回模型列表，请稍后重试或联系服务商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取模型列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 启用/禁用本地模型配置
        private void chk_EnableLocalModel_CheckedChanged(object? sender, EventArgs e)
        {
            if (chk_EnableLocalModel.Checked)
            {
                // 禁用云端API配置区域
                SetGroupBoxEnabled(groupBox_CloudApiConfig, false);
                // 启用本地模型配置区域
                SetGroupBoxEnabled(groupBox_LocalModelConfig, true);
                // 默认选中第一项
                cbo_GpuAccelMode.SelectedIndex = 0;
            }
            else
            {
                // 启用云端API配置区域
                SetGroupBoxEnabled(groupBox_CloudApiConfig, true);
                // 禁用本地模型配置区域
                SetGroupBoxEnabled(groupBox_LocalModelConfig, false);
            }
            // 更新显存上限
            UpdateMemoryLimit();
        }

        /// <summary>
        /// 更新显存/内存上限
        /// </summary>
        private void UpdateMemoryLimit()
        {
            if (chk_EnableLocalModel.Checked && cbo_GpuAccelMode.SelectedItem != null)
            {
                string selectedMode = cbo_GpuAccelMode.SelectedItem.ToString();
                numericUpDown1.Maximum = GetMemoryLimitForMode(selectedMode);
            }
            else
            {
                numericUpDown1.Maximum = GetMaxMemoryLimit();
            }
        }

        /// <summary>
        /// 根据加速模式获取内存限制
        /// </summary>
        private decimal GetMemoryLimitForMode(string mode)
        {
            switch (mode)
            {
                case "CPU 模式":
                    // CPU模式使用系统内存
                    return GetMaxMemoryLimit();
                case "CUDA 12":
                    // CUDA模式使用GPU显存（这里简化处理，使用系统内存的80%作为上限）
                    return GetMaxMemoryLimit() * 0.8m;
                case "Vulkan":
                    // Vulkan模式使用GPU显存
                    return GetMaxMemoryLimit() * 0.8m;
                default: // 自动选择
                    return GetMaxMemoryLimit();
            }
        }

        // 浏览模型文件
        private void btn_BrowseModel_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "LLaMA模型文件 (*.gguf)|*.gguf|所有文件 (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 只显示文件名
                    txt_LocalModelPath.Text = Path.GetFileName(openFileDialog.FileName);
                    // 存储完整路径到Tag属性供后续使用
                    txt_LocalModelPath.Tag = openFileDialog.FileName;
                }
            }
        }

        // 硬件加速模式变化时更新内存限制
        private void cbo_GpuAccelMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateMemoryLimit();
        }

        

        // 显示/隐藏密钥
        private void btnShowApiKey_Click(object? sender, EventArgs e)
        {
            if (txtApiKey.PasswordChar == '*')
            {
                txtApiKey.PasswordChar = '\0';
                btnShowApiKey.Text = "隐藏密钥";
            }
            else
            {
                txtApiKey.PasswordChar = '*';
                btnShowApiKey.Text = "显示密钥";
            }
        }

        // 测试连接
        private async void btnTestConnect_Click(object? sender, EventArgs e)
        {
            // 显示加载状态
            btnTestConnect.Enabled = false;
            btnTestConnect.Text = "测试中...";
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                string baseUrl = txtBaseUrl.Text.TrimEnd('/');
                string apiKey = txtApiKey.Text.Trim();
                string? model = cboModel.Text?.Trim();

                if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
                {
                    MessageBox.Show("请填写 API Key 并选择模型", "提示");
                    return;
                }

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var requestBody = new
                {
                    model = model,
                    messages = new[] { new { role = "user", content = "hi" } },
                    max_tokens = 5
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{baseUrl}/chat/completions", content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("✅ 连接成功！API配置有效", "成功");
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"❌ 连接失败：{response.StatusCode}\n{err}", "错误");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"异常：{ex.Message}", "错误");
            }
            finally
            {
                // 恢复正常状态
                btnTestConnect.Enabled = true;
                btnTestConnect.Text = "测试连接";
                Cursor.Current = Cursors.Default;
            }
        }

        // 保存按钮：保存API配置到数据库
        private void btnSave_Click(object? sender, EventArgs e)
        {
            // 显示加载状态
            btnSave.Enabled = false;
            btnSave.Text = "保存中...";
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                if (chk_EnableLocalModel.Checked)
                {
                    // 保存本地模型配置
                    SaveLocalModelConfig();
                }
                else
                {
                    // 保存云端API配置
                    SaveCloudApiConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存异常：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复正常状态
                btnSave.Enabled = true;
                btnSave.Text = "保存";
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// 保存云端API配置
        /// </summary>
        private void SaveCloudApiConfig()
        {
            string? provider = cboProvider.Text?.Trim();
            string? model = cboModel.Text?.Trim();
            string? endpoint = txtBaseUrl.Text?.Trim();
            string? apiKey = txtApiKey.Text?.Trim();

            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("请填写完整的API配置信息", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建API配置实体
            var apiConfig = new ChatAI.Data.Entities.ApiConfig
            {
                UserAccount = _loginUsername,
                Provider = provider,
                Model = model,
                Endpoint = endpoint,
                ApiKey = apiKey,
                EnableLocalModel = false
            };

            // 保存到数据库
            bool success = _userRepo.SaveApiConfig(apiConfig, out string errorMessage);

            if (success)
            {
                MessageBox.Show("API配置保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show($"保存失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存本地模型配置
        /// </summary>
        private void SaveLocalModelConfig()
        {
            string? modelPath = txt_LocalModelPath.Tag?.ToString();
            string? gpuMode = cbo_GpuAccelMode.SelectedItem?.ToString();
            int memoryLimit = (int)numericUpDown1.Value;

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                MessageBox.Show("请选择本地模型文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(modelPath))
            {
                MessageBox.Show("选择的模型文件不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 检查显存溢出（仅对非CPU模式进行检查）
            if (!string.IsNullOrWhiteSpace(gpuMode) && gpuMode != "CPU 模式" && gpuMode != "CPU")
            {
                long detectedVram = GetGpuMemoryMB();
                if (detectedVram > 0 && memoryLimit > detectedVram)
                {
                    DialogResult result = MessageBox.Show(
                        $"警告：您设置的显存上限 ({memoryLimit} MB) 超过了检测到的系统显存 ({detectedVram} MB)！\n\n" +
                        "继续保存可能导致模型加载失败或显存溢出。\n\n" +
                        "是否继续保存？", 
                        "显存溢出警告", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }
            }

            // 创建API配置实体（本地模型模式）
            var apiConfig = new ChatAI.Data.Entities.ApiConfig
            {
                UserAccount = _loginUsername,
                Provider = "本地模型",
                Model = Path.GetFileName(modelPath),
                Endpoint = modelPath,  // 存储完整路径
                ApiKey = gpuMode,     // 存储加速模式
                EnableLocalModel = true,
                LocalModelMemoryLimit = memoryLimit
            };

            // 保存到数据库
            bool success = _userRepo.SaveApiConfig(apiConfig, out string errorMessage);

            if (success)
            {
                MessageBox.Show("本地模型配置保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show($"保存失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 加载模型列表
        private async void btnLoadModels_Click(object? sender, EventArgs e)
        {
            if (!_formLoaded) return;

            // 检查是否选择了服务商
            if (cboProvider.SelectedItem == null || string.IsNullOrWhiteSpace(cboProvider.Text))
            {
                MessageBox.Show("请先选择服务商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 检查是否有API Key
            if (string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                MessageBox.Show("请先输入 API Key", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 检查是否有接口地址
            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show("请先选择服务商，系统将自动填充接口地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 显示加载状态
            btnLoadModels.Enabled = false;
            btnLoadModels.Text = "加载中...";
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                await LoadModelListAsync();
            }
            finally
            {
                // 恢复正常状态
                btnLoadModels.Enabled = true;
                btnLoadModels.Text = "加载模型";
                Cursor.Current = Cursors.Default;
            }
        }
    }
}
