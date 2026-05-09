using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace ChatAI.Data
{
    public static class SysSecretRepository
    {
        /// <summary>
        /// 从数据库获取加密后的根密钥
        /// </summary>
        public static string GetWrappedRootKey()
        {
            string sql = "SELECT WrapRoot FROM Sys_Secret WHERE Id = 1";
            // 用我们新加的 ExecuteScalar<T>
            return SqliteDbHelper.ExecuteScalar<string>(sql);
        }

        /// <summary>
        /// 保存加密后的根密钥（新库第一次用）
        /// </summary>
        public static void SaveWrappedRootKey(string wrappedRoot)
        {
            string sql = "INSERT OR REPLACE INTO Sys_Secret (Id, WrapRoot) VALUES (1, @WrapRoot)";
            // 用你已有的 ExecuteNonQuery
            SqliteDbHelper.ExecuteNonQuery(sql, new { WrapRoot = wrappedRoot });
        }
    }
}
