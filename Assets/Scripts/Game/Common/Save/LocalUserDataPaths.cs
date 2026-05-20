using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 本地用户数据路径工具：账号库使用共享目录，玩家进度使用当前账号的稳定哈希目录隔离。
    /// </summary>
    public static class LocalUserDataPaths
    {
        #region Fields

        /// <summary>
        /// 本地数据根文件夹名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 按账号隔离的进度子目录名。
        /// </summary>
        private const string AccountProgressFolderName = "Accounts";

        #endregion

        #region Public API

        /// <summary>
        /// 获取共享数据目录，用于保存账号库等不属于单个账号进度的数据。
        /// </summary>
        public static string GetSharedDataFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, DataFolderName);
        }

        /// <summary>
        /// 获取共享数据文件完整路径。
        /// </summary>
        public static string GetSharedDataFilePath(string fileName)
        {
            return Path.Combine(GetSharedDataFolderPath(), fileName);
        }

        /// <summary>
        /// 尝试获取指定账号的进度文件完整路径；账号为空时返回 false。
        /// </summary>
        public static bool TryGetUserDataFilePath(string user, string fileName, out string filePath)
        {
            filePath = string.Empty;
            user = (user ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            filePath = Path.Combine(GetUserDataFolderPath(user), fileName);
            return true;
        }

        /// <summary>
        /// 获取重制版旧数据目录路径；仅用于首次迁移旧的共享进度文件。
        /// </summary>
        public static string GetLegacySharedDataFolderPath()
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
        /// 获取重制版旧共享数据文件完整路径。
        /// </summary>
        public static string GetLegacySharedDataFilePath(string fileName)
        {
            return Path.Combine(GetLegacySharedDataFolderPath(), fileName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取指定账号的进度目录。
        /// </summary>
        private static string GetUserDataFolderPath(string user)
        {
            return Path.Combine(GetSharedDataFolderPath(), AccountProgressFolderName, BuildStableUserKey(user));
        }

        /// <summary>
        /// 基于账号名生成稳定且不泄露明文账号的目录名。
        /// </summary>
        private static string BuildStableUserKey(string user)
        {
            var raw = Encoding.UTF8.GetBytes(user.Trim());
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(raw);
            var builder = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }

        #endregion
    }
}
