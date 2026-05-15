using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

/// <summary>
/// 用户数据路径服务：根据当前登录账号生成玩家进度文件路径，并保留旧版共享路径迁移入口。
/// </summary>
public static class UserDataPathService
{
    #region Fields

    /// <summary>
    /// 本地运行时数据根目录名。
    /// </summary>
    private const string DataFolderName = "UserData";

    /// <summary>
    /// 按账号隔离的玩家进度子目录名。
    /// </summary>
    private const string UsersFolderName = "Users";

    #endregion

    #region Public API

    /// <summary>
    /// 获取当前登录账号名；未登录时返回空字符串。
    /// </summary>
    public static string GetCurrentUser()
    {
        return (AccountStore.GetCurrentUser() ?? string.Empty).Trim();
    }

    /// <summary>
    /// 当前是否已有登录账号。
    /// </summary>
    public static bool HasCurrentUser()
    {
        return !string.IsNullOrWhiteSpace(GetCurrentUser());
    }

    /// <summary>
    /// 获取运行时数据根目录（游戏根目录/UserData）。
    /// </summary>
    public static string GetDataFolderPath()
    {
        var dataPath = Application.dataPath;
        var gameRootPath = Directory.GetParent(dataPath)?.FullName;
        if (string.IsNullOrEmpty(gameRootPath))
        {
            gameRootPath = dataPath;
        }

        return Path.Combine(gameRootPath, DataFolderName);
    }

    /// <summary>
    /// 尝试获取当前账号的进度目录；未登录时返回 false。
    /// </summary>
    /// <param name="folderPath">当前账号进度目录。</param>
    public static bool TryGetCurrentUserDataFolderPath(out string folderPath)
    {
        folderPath = string.Empty;
        var currentUser = GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            return false;
        }

        folderPath = Path.Combine(GetDataFolderPath(), UsersFolderName, BuildUserFolderName(currentUser));
        return true;
    }

    /// <summary>
    /// 尝试获取当前账号下指定数据文件路径；未登录或文件名为空时返回 false。
    /// </summary>
    /// <param name="fileName">数据文件名。</param>
    /// <param name="filePath">当前账号下的数据文件完整路径。</param>
    public static bool TryGetCurrentUserDataFilePath(string fileName, out string filePath)
    {
        filePath = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (!TryGetCurrentUserDataFolderPath(out var folderPath))
        {
            return false;
        }

        filePath = Path.Combine(folderPath, fileName.Trim());
        return true;
    }

    /// <summary>
    /// 获取旧版共享数据文件路径，用于首次读取时向账号目录迁移。
    /// </summary>
    /// <param name="fileName">旧版数据文件名。</param>
    public static string GetLegacySharedDataFilePath(string fileName)
    {
        return Path.Combine(GetDataFolderPath(), fileName ?? string.Empty);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 将账号名转换为稳定且不暴露原文的目录名，避免路径非法字符和目录穿越。
    /// </summary>
    /// <param name="userName">当前账号名。</param>
    private static string BuildUserFolderName(string userName)
    {
        var bytes = Encoding.UTF8.GetBytes(userName ?? string.Empty);
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(bytes);
        var builder = new StringBuilder(hashBytes.Length * 2);
        for (var i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        return builder.ToString();
    }

    #endregion
}
