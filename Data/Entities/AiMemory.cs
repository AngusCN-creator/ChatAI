﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;

namespace ChatAI.Data.Entities
{
    /// <summary>
    /// AI记忆实体类
    /// 对应数据库中的ai_memory表
    /// 存储用户长期记忆信息
    /// </summary>
    public class AiMemory
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户账号
        /// </summary>
        public string UserAccount { get; set; }

        /// <summary>
        /// 记忆内容（由AI生成的用户长期记忆文本）
        /// </summary>
        public string MemoryContent { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}