using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Game.Common.Security
{
    /// <summary>
    /// 本地存档 AES 加解密：与账号库共用同一密钥派生方式，所有 UserData 明文 JSON 经此落盘。
    /// </summary>
    public static class LocalDataCrypto
    {
        #region Fields

        /// <summary>
        /// 密钥派生种子（项目常量，勿随意变更以免旧档无法解密）。
        /// </summary>
        private const string KeySeed = "CardGame.LocalAuth.EncryptionSeed.v1";

        #endregion

        #region Public API

        /// <summary>
        /// 将 UTF-8 明文加密为字节（前 16 字节为随机 IV，与密文拼接后一并返回）。
        /// </summary>
        /// <param name="plainText">待加密的明文；为 null 时按空字符串处理。</param>
        /// <returns>长度为 16 + 密文长度 的字节数组；AES-CBC、PKCS7。</returns>
        public static byte[] EncryptUtf8(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
            var key = BuildAesKey();

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return result;
        }

        /// <summary>
        /// 将加密字节解密为 UTF-8 明文（假定前 16 字节为 IV，其后为密文）。
        /// </summary>
        /// <param name="encryptedBytes">由 <see cref="EncryptUtf8"/> 生成的字节流；无效或过短时返回空字符串。</param>
        /// <returns>解密后的 UTF-8 文本；失败或数据不合法时返回 <see cref="string.Empty"/>。</returns>
        public static string DecryptToUtf8(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length <= 16)
            {
                return string.Empty;
            }

            var key = BuildAesKey();
            var iv = new byte[16];
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);

            var cipherLength = encryptedBytes.Length - iv.Length;
            var cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(encryptedBytes, iv.Length, cipherBytes, 0, cipherLength);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 基于项目种子与 <see cref="UnityEngine.SystemInfo.deviceUniqueIdentifier"/> 生成 256 位 AES 密钥（SHA256）。
        /// </summary>
        /// <returns>32 字节密钥。</returns>
        private static byte[] BuildAesKey()
        {
            var deviceId = SystemInfo.deviceUniqueIdentifier ?? "unknown-device";
            var raw = Encoding.UTF8.GetBytes($"{KeySeed}:{deviceId}");
            using var sha = SHA256.Create();
            return sha.ComputeHash(raw);
        }

        #endregion
    }
}
