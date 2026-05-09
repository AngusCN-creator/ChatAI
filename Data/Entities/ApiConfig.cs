namespace ChatAI.Data.Entities
{
    /// <summary>
    /// API配置实体
    /// </summary>
    public class ApiConfig
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 绑定的用户名
    /// </summary>
    public string UserAccount { get; set; } = string.Empty;

    /// <summary>
    /// 模型服务商
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 接口地址
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// API密钥
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// 是否启用本地模型
    /// </summary>
    public bool? EnableLocalModel { get; set; }

    /// <summary>
    /// 本地模型内存限制（MB）
    /// </summary>
    public int? LocalModelMemoryLimit { get; set; }
    }
}