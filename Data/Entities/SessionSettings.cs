﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;

namespace ChatAI.Data.Entities
{
    /// <summary>
    /// 会话设置实体类
    /// 对应数据库中的session_settings表
    /// </summary>
    public class SessionSettings
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户账号
        /// </summary>
        public string? UserAccount { get; set; }

        /// <summary>
        /// 是否禁用角色信息
        /// </summary>
        public bool NoRole { get; set; }

        /// <summary>
        /// 历史消息数量
        /// </summary>
        public int HistoryCount { get; set; }

        /// <summary>
        /// 最大提示词令牌数
        /// </summary>
        public int MaxPromptTokens { get; set; }

        /// <summary>
        /// 对话温度
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// 记忆触发阈值
        /// </summary>
        public int MemoryTrigger { get; set; }

        /// <summary>
        /// 记忆提示词
        /// </summary>
        public string? MemoryPrompt { get; set; }
    }
}