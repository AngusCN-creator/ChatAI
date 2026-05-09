using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ChatAI.Data
{
    /// <summary>
    /// 加密工具类，用于加密和解密敏感数据如API Key
    /// 双层安全架构：
    /// 外层 K2 = GetRuntimeSkey() 固定混淆外壳（永不改变）
    /// 内层 K1 = 首次运行随机生成 32位 AES 根密钥（终身不变）
    /// </summary>
    public static class EncryptionHelper
    {
        // 【底层埋藏原生二进制碎片 纯byte 无任何明文】
        private static readonly byte[] seg1 = { 0x17, 0x32, 0xAF, 0x7B, 0x29, 0xD1, 0x55, 0xC3 };
        private static readonly byte[] seg2 = { 0xE2, 0x08, 0x4D, 0xBB, 0x77, 0x19, 0x6F, 0x24 };
        private static readonly byte[] seg3 = { 0x9C, 0x41, 0x86, 0x30, 0xD8, 0x5A, 0x11, 0x70 };

        // 缓存：运行时最终推导出来的固定外层Skey(K2)
        private static byte[]? _cachedRuntimeSkey;

        /// <summary>
        /// 正版安全推导：交叉穿插 + 异或畸变打乱 非简单拼接
        /// 输出严格 32字节 AES256 外层外壳密钥
        /// </summary>
        public static byte[] GetRuntimeSkey()
        {
            if (_cachedRuntimeSkey != null)
                return _cachedRuntimeSkey;

            var pool = seg1.Concat(seg2).Concat(seg3).ToArray();
            byte[] temp = new byte[24];
            int idx = 0;

            for (int i = 0; i < pool.Length && idx < temp.Length; i += 3)
                temp[idx++] = pool[i];
            for (int i = 1; i < pool.Length && idx < temp.Length; i += 3)
                temp[idx++] = pool[i];

            byte distortSalt = 0x5F;
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = (byte)(temp[i] ^ distortSalt ^ (byte)i);
                byte low = (byte)(temp[i] & 0x0F);
                byte high = (byte)(temp[i] & 0xF0);
                temp[i] = (byte)(high | ((low >> 1) | (low << 3)));
            }

            _cachedRuntimeSkey = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                _cachedRuntimeSkey[i] = temp[i % temp.Length];
                _cachedRuntimeSkey[i] ^= (byte)(0x2D + i);
            }

            return _cachedRuntimeSkey;
        }

        // ==============================================
        // ✅ 【核心改造】全局唯一、终身不变、32字节 AES 根密钥
        // ==============================================
        // 初始化前为空数组，确保非null；InitializeRootKey会在程序启动时调用
        private static byte[] _rootAesKey = Array.Empty<byte>();

        /// <summary>
        /// 初始化全局根密钥（首次建库时调用一次）
        /// </summary>
        public static void InitializeRootKey(bool isNewDatabase)
        {
            // 检查是否已经初始化过（防止重复初始化导致密钥变化）
            if (_rootAesKey != null && _rootAesKey.Length == 32)
            {
                return; // 已经初始化，直接返回
            }

            if (isNewDatabase)
            {
                // 新库：生成真随机 32字节 终身密钥
                _rootAesKey = GenerateSecureRandom32ByteKey();

                // 用外层K2加密
                string wrapped = WrapRootKey(_rootAesKey);

                // ✅ 调用仓储保存
                SysSecretRepository.SaveWrappedRootKey(wrapped);
            }
            else
            {
                // ✅ 从仓储读取加密密钥
                string wrappedRoot = SysSecretRepository.GetWrappedRootKey();
                
                if (string.IsNullOrEmpty(wrappedRoot))
                {
                    // 数据库存在但无密钥记录：可能是新建数据库但初始化失败，重新生成并保存
                    _rootAesKey = GenerateSecureRandom32ByteKey();
                    string wrapped = WrapRootKey(_rootAesKey);
                    SysSecretRepository.SaveWrappedRootKey(wrapped);
                }
                else
                {
                    // 用外层K2解密，还原根密钥
                    _rootAesKey = UnwrapRootKey(wrappedRoot);
                }
            }
        }

        /// <summary>
        /// 生成 密码学安全随机 32字节 AES-256 密钥
        /// 【终身只生成一次】
        /// </summary>
        private static byte[] GenerateSecureRandom32ByteKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// 用外层K2加密 根密钥K1（存入数据库）
        /// </summary>
        public static string WrapRootKey(byte[] rootKey)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = GetRuntimeSkey();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(rootKey, 0, rootKey.Length);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// 用外层K2解密 根密钥K1（从数据库读取）
        /// </summary>
        public static byte[] UnwrapRootKey(string encryptedBase64)
        {
            byte[] allBytes = Convert.FromBase64String(encryptedBase64);
            using (var aes = Aes.Create())
            {
                aes.Key = GetRuntimeSkey();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] iv = new byte[16];
                Array.Copy(allBytes, 0, iv, 0, 16);
                aes.IV = iv;

                byte[] cipher = new byte[allBytes.Length - 16];
                Array.Copy(allBytes, 16, cipher, 0, cipher.Length);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipher, 0, cipher.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// 获取全局根密钥（真正用来加密业务数据）
        /// </summary>
        public static byte[] RootAesKey => _rootAesKey;

        // ==============================================
        // 原有加密解密方法 → 现在全部使用 RootAesKey
        // ==============================================
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            
            // 检查密钥是否已初始化
            if (_rootAesKey == null || _rootAesKey.Length != 32)
            {
                throw new InvalidOperationException("加密密钥未初始化，请先调用 InitializeRootKey()");
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = _rootAesKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                // 使用基于明文内容的确定性IV，确保相同明文产生相同密文
                // 这样可以直接在数据库中进行密文比对
                byte[] iv = GenerateDeterministicIV(plainText);
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(inputBytes, 0, inputBytes.Length);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// 根据明文内容生成确定性的IV（用于密码等需要比对的场景）
        /// 使用SHA256哈希的前16字节作为IV
        /// </summary>
        private static byte[] GenerateDeterministicIV(string plainText)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(inputBytes);
                
                // 取前16字节作为IV
                byte[] iv = new byte[16];
                Array.Copy(hash, 0, iv, 0, 16);
                
                return iv;
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            byte[] allBytes = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = _rootAesKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] iv = new byte[16];
                Array.Copy(allBytes, 0, iv, 0, 16);
                aes.IV = iv;

                byte[] cipher = new byte[allBytes.Length - 16];
                Array.Copy(allBytes, 16, cipher, 0, cipher.Length);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipher, 0, cipher.Length);
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}