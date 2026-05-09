using System;

namespace ChatControl.Models
{
    /// <summary>
    /// 会话列表实体（对应AI角色信息）
    /// 作用：用于左侧会话列表绑定、显示、选中后获取AI角色信息
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// AI角色主键ID（数据库唯一标识）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 所属用户账号
        /// </summary>
        public string? UserAccount { get; set; }

        /// <summary>
        /// AI角色名称（会话列表显示名称）
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
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}