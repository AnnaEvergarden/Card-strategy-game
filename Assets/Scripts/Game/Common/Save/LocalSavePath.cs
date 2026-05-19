using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

/// <summary>
/// 本地存档路径工具：统一使用 Unity 持久化目录，并为游戏进度提供按账号隔离的目录。
/// </summary>
public static class LocalSavePath
{
    #region Fields

    /// <summary>
    /// 本地数据根目录名。
    /// </summary>
    private const string DataFolderName = "UserData";

    /// <summary>
    /// 按账号隔离的游戏进度目录名。
    /// </summary>
    private const string AccountDataFolderName = "Accounts";

    /// <summary>
    /// 空账号路径占位，避免未登录时构造出可写入的真实玩家目录。
    /// </summary>
    private const string EmptyAccountKey = string.Empty;

    #endregion

    #region Public API

    /// <summary>
    /// 获取全局本地数据目录，用于账号库等不归属某个账号的数据。
    /// </summary>
    public static string GetGlobalDataFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, DataFolderName);
    }

    /// <summary>
    /// 获取全局本地数据文件路径。
    /// </summary>
    /// <param name="fileName">文件名。</param>
    public static string GetGlobalDataFilePath(string fileName)
    {
        return Path.Combine(GetGlobalDataFolderPath(), fileName ?? string.Empty);
    }

    /// <summary>
    /// 尝试获取当前登录账号的游戏进度目录；未登录时返回 false，调用方应跳过保存。
    /// </summary>
    /// <param name="folderPath">当前账号的游戏进度目录。</param>
    public static bool TryGetCurrentAccountDataFolderPath(out string folderPath)
    {
        var currentUser = AccountStore.GetCurrentUser();
        var accountKey = SanitizeAccountKey(currentUser);
        if (string.IsNullOrEmpty(accountKey))
        {
            folderPath = string.Empty;
            return false;
        }

        folderPath = Path.Combine(GetGlobalDataFolderPath(), AccountDataFolderName, accountKey);
        return true;
    }

    /// <summary>
    /// 尝试获取当前登录账号的游戏进度文件路径；未登录时返回 false，调用方应跳过读写。
    /// </summary>
    /// <param name="fileName">文件名。</param>
    /// <param name="filePath">当前账号的游戏进度文件路径。</param>
    public static bool TryGetCurrentAccountDataFilePath(string fileName, out string filePath)
    {
        if (!TryGetCurrentAccountDataFolderPath(out var folderPath))
        {
            filePath = string.Empty;
            return false;
        }

        filePath = Path.Combine(folderPath, fileName ?? string.Empty);
        return true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 将账号名转换为安全的单级目录名，防止路径穿越或跨平台非法字符。
    /// </summary>
    /// <param name="accountName">账号名。</param>
    private static string SanitizeAccountKey(string accountName)
    {
        accountName = (accountName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(accountName))
        {
            return EmptyAccountKey;
        }

        var chars = accountName.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '@')
            {
                continue;
            }

            chars[i] = '_';
        }

        var sanitized = new string(chars).Trim('.');
        if (string.IsNullOrEmpty(sanitized) ||
            string.Equals(sanitized, ".", StringComparison.Ordinal) ||
            string.Equals(sanitized, "..", StringComparison.Ordinal))
        {
            sanitized = "account";
        }

        return $"{sanitized}_{BuildStableSuffix(accountName)}";
    }

    /// <summary>
    /// 为账号目录追加稳定短哈希，避免大小写或非法字符归一化后的目录名冲突。
    /// </summary>
    /// <param name="accountName">原始账号名。</param>
    private static string BuildStableSuffix(string accountName)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(accountName));
        var builder = new StringBuilder(16);
        for (var i = 0; i < 8 && i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }

    #endregion
}
