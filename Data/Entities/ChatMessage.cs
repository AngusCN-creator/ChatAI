﻿/// <summary>
/// 聊天消息实体
/// </summary>
public class ChatMessage
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
    /// 所属AI角色名称
    /// </summary>
    public string? AiName { get; set; }

    /// <summary>
    /// 所属AI角色ID
    /// </summary>
    public int AiCharacterId { get; set; }

    /// <summary>
    /// 发送者：user/ai/system
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 用户与AI对话消息总数（用于记忆生成触发计数）
    /// </summary>
    public int TotalMessageCount { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}