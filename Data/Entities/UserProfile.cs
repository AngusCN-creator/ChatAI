/// <summary>
/// 用户信息实体
/// </summary>
public class UserProfile
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
    /// 用户昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 用户性别
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 用户人设
    /// </summary>
    public string? Persona { get; set; }

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string? SystemPrompt { get; set; }
}