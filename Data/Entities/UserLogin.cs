/// <summary>
/// 用户登录信息实体
/// </summary>
public class UserLogin
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 记住密码标记 0=不记住 1=记住
    /// </summary>
    public int RememberMe { get; set; }

    /// <summary>
    /// 用户登录账号（唯一）
    /// </summary>
    public string? Account { get; set; }

    /// <summary>
    /// 登录密码
    /// </summary>
    public string? Password { get; set; }
}