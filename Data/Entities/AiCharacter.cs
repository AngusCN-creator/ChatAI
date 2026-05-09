/// <summary>
/// AI人物信息实体
/// </summary>
public class AiCharacter
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所属用户账号
    /// </summary>
    public string? UserAccount { get; set; }

    /// <summary>
    /// AI角色名称
    /// </summary>
    public string? AiName { get; set; }

    /// <summary>
    /// AI性别
    /// </summary>
    public string? AiGender { get; set; }

    /// <summary>
    /// AI人设
    /// </summary>
    public string? AiPersona { get; set; }

    /// <summary>
    /// 表达风格
    /// </summary>
    public string? AiStyle { get; set; }

    /// <summary>
    /// 习惯用语
    /// </summary>
    public string? AiHabit { get; set; }

    /// <summary>
    /// 开场白
    /// </summary>
    public string? AiOpening { get; set; }

    /// <summary>
    /// 创建时间（自动生成）
    /// </summary>
    public DateTime CreateTime { get; set; }
}