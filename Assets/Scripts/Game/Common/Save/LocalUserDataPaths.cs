using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 本地用户数据路径工具：账号库使用共享路径，游戏进度按当前登录账号隔离到独立目录。
    /// </summary>
    public static class LocalUserDataPaths
    {
        #region Fields

        /// <summary>
        /// 本地数据根目录名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 账号进度子目录名。
        /// </summary>
        private const string UserProgressFolderName = "Users";

        /// <summary>
        /// 用户进度目录前缀，后接账号名哈希。
        /// </summary>
        private const string UserFolderPrefix = "user_";

        #endregion

        #region Public API

        /// <summary>
        /// 获取共享数据文件路径；用于账号库等不属于单个用户进度的文件。
        /// </summary>
        public static string GetSharedDataFilePath(string dataFileName)
        {
            return Path.Combine(GetRootDataFolderPath(), SanitizeDataFileName(dataFileName));
        }

        /// <summary>
        /// 获取当前登录账号的数据文件路径；未登录时返回 false。
        /// </summary>
        public static bool TryGetCurrentUserDataFilePath(string dataFileName, out string filePath, out string currentUser)
        {
            currentUser = AccountStore.GetCurrentUser();
            return TryGetUserDataFilePath(currentUser, dataFileName, out filePath);
        }

        /// <summary>
        /// 获取指定账号的数据文件路径；账号为空时返回 false。
        /// </summary>
        public static bool TryGetUserDataFilePath(string user, string dataFileName, out string filePath)
        {
            filePath = string.Empty;
            user = (user ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(user))
            {
                return false;
            }

            filePath = Path.Combine(GetUserDataFolderPath(user), SanitizeDataFileName(dataFileName));
            return true;
        }

        /// <summary>
        /// 获取当前账号可读取的数据文件路径；若新路径不存在但旧共享文件存在，则返回旧共享文件用于迁移读取。
        /// </summary>
        public static bool TryResolveCurrentUserReadableFilePath(
            string dataFileName,
            out string readFilePath,
            out string userFilePath,
            out string currentUser,
            out bool isLegacySharedFile)
        {
            readFilePath = string.Empty;
            userFilePath = string.Empty;
            isLegacySharedFile = false;

            if (!TryGetCurrentUserDataFilePath(dataFileName, out userFilePath, out currentUser))
            {
                return false;
            }

            if (File.Exists(userFilePath))
            {
                readFilePath = userFilePath;
                return true;
            }

            var legacyFilePath = GetSharedDataFilePath(dataFileName);
            if (File.Exists(legacyFilePath))
            {
                readFilePath = legacyFilePath;
                isLegacySharedFile = true;
                return true;
            }

            readFilePath = userFilePath;
            return true;
        }

        /// <summary>
        /// 获取本地数据根目录路径（游戏根目录/UserData）。
        /// </summary>
        public static string GetRootDataFolderPath()
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
        /// 将已成功读取的旧共享进度文件复制到当前账号目录，保留旧文件以便其他旧账号首次迁移。
        /// </summary>
        public static void CopyLegacySharedFileIfNeeded(string legacyFilePath, string userFilePath)
        {
            if (string.IsNullOrEmpty(legacyFilePath) ||
                string.IsNullOrEmpty(userFilePath) ||
                !File.Exists(legacyFilePath) ||
                File.Exists(userFilePath))
            {
                return;
            }

            var folderPath = Path.GetDirectoryName(userFilePath);
            if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            File.Copy(legacyFilePath, userFilePath, false);
            Debug.Log($"[LocalUserDataPaths] 已迁移旧共享进度文件 => {userFilePath}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取指定账号的进度目录路径。
        /// </summary>
        private static string GetUserDataFolderPath(string user)
        {
            return Path.Combine(GetRootDataFolderPath(), UserProgressFolderName, BuildUserFolderName(user));
        }

        /// <summary>
        /// 以账号名哈希构建安全目录名，避免账号文本中的路径字符影响文件系统。
        /// </summary>
        private static string BuildUserFolderName(string user)
        {
            var normalizedUser = (user ?? string.Empty).Trim();
            var userBytes = Encoding.UTF8.GetBytes(normalizedUser);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(userBytes);

            var builder = new StringBuilder(UserFolderPrefix.Length + 32);
            builder.Append(UserFolderPrefix);
            for (var i = 0; i < 16 && i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 只保留文件名部分，防止调用方传入路径片段。
        /// </summary>
        private static string SanitizeDataFileName(string dataFileName)
        {
            var safeName = Path.GetFileName(dataFileName ?? string.Empty);
            return string.IsNullOrWhiteSpace(safeName) ? "data.dat" : safeName;
        }

        #endregion
    }
}
