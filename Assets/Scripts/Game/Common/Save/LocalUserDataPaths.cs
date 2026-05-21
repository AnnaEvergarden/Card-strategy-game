using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 本地用户数据路径工具：账号库共享保存，玩家进度按当前账号隔离保存。
    /// </summary>
    public static class LocalUserDataPaths
    {
        #region Fields

        /// <summary>
        /// 本地用户数据根目录名。
        /// </summary>
        private const string UserDataFolderName = "UserData";

        /// <summary>
        /// 单账号玩家进度目录名。
        /// </summary>
        private const string PlayerProgressFolderName = "Players";

        #endregion

        #region Public API

        /// <summary>
        /// 当前版本的用户数据根目录，位于 Unity 持久化数据目录下。
        /// </summary>
        public static string UserDataRootPath => Path.Combine(Application.persistentDataPath, UserDataFolderName);

        /// <summary>
        /// 获取共享账号库文件路径。
        /// </summary>
        /// <param name="fileName">账号库文件名。</param>
        /// <returns>持久化目录下的共享账号库路径。</returns>
        public static string GetSharedFilePath(string fileName)
        {
            return Path.Combine(UserDataRootPath, SanitizeFileName(fileName));
        }

        /// <summary>
        /// 获取当前登录账号的进度文件路径；未登录时返回 false。
        /// </summary>
        /// <param name="fileName">进度文件名。</param>
        /// <param name="filePath">当前账号专属进度文件路径。</param>
        /// <param name="ownerKey">当前账号的稳定目录键。</param>
        /// <returns>是否存在当前登录账号。</returns>
        public static bool TryGetCurrentUserProgressFilePath(string fileName, out string filePath, out string ownerKey)
        {
            filePath = string.Empty;
            ownerKey = GetCurrentUserKey();
            if (string.IsNullOrEmpty(ownerKey))
            {
                return false;
            }

            filePath = Path.Combine(UserDataRootPath, PlayerProgressFolderName, ownerKey, SanitizeFileName(fileName));
            return true;
        }

        /// <summary>
        /// 判断指定缓存归属键是否仍是当前登录账号。
        /// </summary>
        /// <param name="ownerKey">缓存记录的账号目录键。</param>
        /// <returns>缓存归属与当前账号一致时返回 true。</returns>
        public static bool IsCurrentUserKey(string ownerKey)
        {
            return !string.IsNullOrEmpty(ownerKey) &&
                   string.Equals(ownerKey, GetCurrentUserKey(), System.StringComparison.Ordinal);
        }

        /// <summary>
        /// 获取旧版本共享数据文件路径，用于首次迁移既有本地数据。
        /// </summary>
        /// <param name="fileName">旧文件名。</param>
        /// <param name="filePath">旧版本游戏根目录 UserData 下的路径。</param>
        /// <returns>是否能解析旧路径。</returns>
        public static bool TryGetLegacySharedFilePath(string fileName, out string filePath)
        {
            var dataPath = Application.dataPath;
            var gameRoot = Directory.GetParent(dataPath)?.FullName;
            if (string.IsNullOrEmpty(gameRoot))
            {
                filePath = string.Empty;
                return false;
            }

            filePath = Path.Combine(gameRoot, UserDataFolderName, SanitizeFileName(fileName));
            return true;
        }

        /// <summary>
        /// 确保目标文件的父目录存在。
        /// </summary>
        /// <param name="filePath">目标文件完整路径。</param>
        public static void EnsureParentDirectory(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 根据当前登录账号生成稳定目录键。
        /// </summary>
        private static string GetCurrentUserKey()
        {
            var currentUser = AccountStore.GetCurrentUser();
            return string.IsNullOrWhiteSpace(currentUser) ? string.Empty : BuildStableUserKey(currentUser.Trim());
        }

        /// <summary>
        /// 将账号名哈希为文件系统安全的短目录名，避免明文账号进入路径。
        /// </summary>
        private static string BuildStableUserKey(string user)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(user));
            var builder = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 仅保留文件名部分，避免调用方传入路径穿越片段。
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            return Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "data.dat" : fileName.Trim());
        }

        #endregion
    }
}
