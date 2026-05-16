using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 本地用户数据路径工具：统一提供可写根目录、账号共享文件路径与当前账号进度文件路径。
    /// </summary>
    public static class LocalUserDataPaths
    {
        #region Fields

        /// <summary>
        /// 本地数据根文件夹名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 按账号隔离进度数据的子目录名。
        /// </summary>
        private const string AccountProgressFolderName = "Accounts";

        #endregion

        #region Public API

        /// <summary>
        /// 获取账号库等共享数据目录（Application.persistentDataPath/UserData）。
        /// </summary>
        public static string GetSharedDataFolderPath()
        {
            return Path.Combine(GetWritableRootPath(), DataFolderName);
        }

        /// <summary>
        /// 获取账号库等共享数据文件路径。
        /// </summary>
        /// <param name="fileName">共享数据文件名。</param>
        /// <returns>共享数据文件完整路径。</returns>
        public static string GetSharedDataFilePath(string fileName)
        {
            return Path.Combine(GetSharedDataFolderPath(), fileName);
        }

        /// <summary>
        /// 尝试获取当前登录账号的进度目录；未登录时返回 false，避免写入无归属存档。
        /// </summary>
        /// <param name="folderPath">当前账号进度目录。</param>
        /// <param name="createFolder">是否在成功获取后创建目录。</param>
        /// <returns>是否存在当前登录账号。</returns>
        public static bool TryGetCurrentAccountDataFolderPath(out string folderPath, bool createFolder = false)
        {
            folderPath = string.Empty;
            var currentUser = AccountStore.GetCurrentUser();
            if (string.IsNullOrWhiteSpace(currentUser))
            {
                return false;
            }

            folderPath = Path.Combine(
                GetSharedDataFolderPath(),
                AccountProgressFolderName,
                ToStableAccountFolderName(currentUser));

            if (createFolder && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return true;
        }

        /// <summary>
        /// 尝试获取当前登录账号下的指定进度文件路径；未登录时返回 false。
        /// </summary>
        /// <param name="fileName">进度文件名。</param>
        /// <param name="filePath">当前账号下的进度文件完整路径。</param>
        /// <param name="createFolder">是否创建当前账号进度目录。</param>
        /// <returns>是否存在当前登录账号。</returns>
        public static bool TryGetCurrentAccountDataFilePath(
            string fileName,
            out string filePath,
            bool createFolder = false)
        {
            filePath = string.Empty;
            if (!TryGetCurrentAccountDataFolderPath(out var folderPath, createFolder))
            {
                return false;
            }

            filePath = Path.Combine(folderPath, fileName);
            return true;
        }

        /// <summary>
        /// 若旧版本共享文件仍在游戏根目录/UserData，则迁移到 persistentDataPath/UserData。
        /// </summary>
        /// <param name="fileName">共享文件名。</param>
        public static void TryCopyLegacySharedFileToPersistent(string fileName)
        {
            var targetPath = GetSharedDataFilePath(fileName);
            if (File.Exists(targetPath))
            {
                return;
            }

            var legacyPath = Path.Combine(GetLegacySharedDataFolderPath(), fileName);
            if (!File.Exists(legacyPath) || string.Equals(legacyPath, targetPath, StringComparison.Ordinal))
            {
                return;
            }

            var targetFolder = GetSharedDataFolderPath();
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            File.Copy(legacyPath, targetPath);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取跨平台可写根目录；persistentDataPath 为空时回退到旧版游戏根目录。
        /// </summary>
        private static string GetWritableRootPath()
        {
            var persistentPath = Application.persistentDataPath;
            if (!string.IsNullOrWhiteSpace(persistentPath))
            {
                return persistentPath;
            }

            return GetLegacyGameRootPath();
        }

        /// <summary>
        /// 获取旧版存档目录使用的游戏根目录（Application.dataPath 的父目录）。
        /// </summary>
        private static string GetLegacyGameRootPath()
        {
            var dataPath = Application.dataPath;
            var gameRootPath = Directory.GetParent(dataPath)?.FullName;
            return string.IsNullOrEmpty(gameRootPath) ? dataPath : gameRootPath;
        }

        /// <summary>
        /// 获取旧版共享数据目录，用于账号库从旧位置迁移到 persistentDataPath。
        /// </summary>
        private static string GetLegacySharedDataFolderPath()
        {
            return Path.Combine(GetLegacyGameRootPath(), DataFolderName);
        }

        /// <summary>
        /// 将账号名转换成稳定且文件系统安全的目录名。
        /// </summary>
        private static string ToStableAccountFolderName(string accountName)
        {
            var normalized = (accountName ?? string.Empty).Trim();
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var builder = new StringBuilder("user_", 37);
            for (var i = 0; i < 16 && i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        #endregion
    }
}
