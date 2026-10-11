using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 加密工具：提供基于 AES-256-CBC 的字符串加密与解密功能。
/// 由 SaveService 内部使用，确保本地存档数据的机密性。
/// AES 密钥通过 SHA256 从主密钥 + 盐派生，每次加密生成随机 IV 并作为前缀写入。
/// </summary>
internal static class CryptoHelper
{
    /// <summary>
    /// 密钥派生主密钥（本地存档场景使用，生产环境应改为密钥管理方案）。
    /// </summary>
    private const string MasterSecret = "CardGame_Encrypt_Master_Secret_2026";

    /// <summary>
    /// 密钥派生盐，避免直接使用主密钥。
    /// </summary>
    private const string KeySalt = ":CardGame:Save:v1";

    /// <summary>
    /// AES 块大小（字节）。
    /// </summary>
    private const int IvSize = 16;

    /// <summary>
    /// SHA256 派生密钥：主密钥 + 盐 → 32 字节，对应 AES-256。
    /// </summary>
    private static readonly byte[] DerivedAesKey = DeriveAesKey();

    /// <summary>
    /// 旧版本固定密钥，用于兼容旧存档解密。
    /// </summary>
    private static readonly byte[] LegacyAesKey = Encoding.UTF8.GetBytes("CardGameEncryptKey2026!!ABCDEFGH");

    /// <summary>
    /// 使用 SHA256 派生 32 字节 AES 密钥。
    /// </summary>
    private static byte[] DeriveAesKey()
    {
        byte[] seed = Encoding.UTF8.GetBytes(MasterSecret + KeySalt);
        using var sha = SHA256.Create();
        return sha.ComputeHash(seed);
    }

    /// <summary>
    /// 将明文 UTF8 字符串加密为二进制数据。
    /// </summary>
    /// <param name="plainText">明文字符串</param>
    /// <returns>加密后的二进制数据（包含随机 IV 前缀）</returns>
    public static byte[] Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return null;

        using var aes = Aes.Create();
        aes.Key = DerivedAesKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] iv = new byte[IvSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        // 随机 IV 前缀写入密文头部，解密时从数据中读取
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// 将加密的二进制数据解密为 UTF8 字符串。
    /// </summary>
    /// <param name="cipherData">加密数据（包含 IV 前缀）</param>
    /// <returns>明文字符串；解密失败时返回 null</returns>
    public static string Decrypt(byte[] cipherData)
    {
        if (cipherData == null || cipherData.Length <= IvSize)
            return null;

        byte[] iv = new byte[IvSize];
        Array.Copy(cipherData, 0, iv, 0, IvSize);

        string result = DecryptWithKey(cipherData, iv, DerivedAesKey);
        if (result != null)
            return result;

        // 兼容旧版本固定密钥生成的存档
        if (!KeysEqual(DerivedAesKey, LegacyAesKey))
            result = DecryptWithKey(cipherData, iv, LegacyAesKey);

        if (result == null)
        {
            UnityEngine.Debug.LogWarning("[CryptoHelper] 解密失败：密钥不匹配或数据损坏");
        }

        return result;
    }

    /// <summary>
    /// 使用指定密钥解密（IV 从密文头部读取）。
    /// </summary>
    private static string DecryptWithKey(byte[] cipherData, byte[] iv, byte[] key)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherData);
            ms.Seek(IvSize, SeekOrigin.Begin);

            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static bool KeysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }
}
