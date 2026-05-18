using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using Game.Common.Security;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 本地用户数据路径工具：账号库使用共享路径，玩家进度按当前登录账号隔离。
    /// </summary>
    public static class LocalUserDataPaths
    {
        #region Fields

        /// <summary>
        /// 本地存档根目录名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 按账号隔离的子目录名。
        /// </summary>
        private const string UsersFolderName = "Users";

        /// <summary>
        /// 旧共享存档迁移标记文件前缀，避免多个账号重复继承同一份旧进度。
        /// </summary>
        private const string MigrationMarkerPrefix = ".migrated_";

        #endregion

        #region Public API

        /// <summary>
        /// 获取共享数据文件路径，适用于账号库等不属于单个账号进度的数据。
        /// </summary>
        /// <param name="fileName">数据文件名。</param>
        /// <returns>位于持久化目录下的共享文件完整路径。</returns>
        public static string GetSharedDataFilePath(string fileName)
        {
            var safeFileName = SanitizeFileName(fileName);
            var targetPath = Path.Combine(GetSharedDataFolderPath(), safeFileName);
            TryCopyDecryptableLegacyFile(safeFileName, targetPath);
            return targetPath;
        }

        /// <summary>
        /// 尝试获取当前登录账号的数据文件路径；未登录时返回 false。
        /// </summary>
        /// <param name="fileName">数据文件名。</param>
        /// <param name="ownerKey">当前账号的稳定目录键。</param>
        /// <param name="filePath">账号隔离后的数据文件完整路径。</param>
        /// <returns>当前存在登录账号时为 true。</returns>
        public static bool TryGetCurrentUserDataFilePath(string fileName, out string ownerKey, out string filePath)
        {
            ownerKey = string.Empty;
            filePath = string.Empty;

            var currentUser = AccountStore.GetCurrentUser();
            if (string.IsNullOrWhiteSpace(currentUser))
            {
                return false;
            }

            var safeFileName = SanitizeFileName(fileName);
            ownerKey = BuildUserFolderName(currentUser);
            filePath = Path.Combine(GetUserDataFolderPath(ownerKey), safeFileName);
            TryMigrateLegacyProgressFile(safeFileName, filePath);
            return true;
        }

        /// <summary>
        /// 确保指定文件路径的父目录存在。
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
        /// 获取持久化数据共享目录。
        /// </summary>
        private static string GetSharedDataFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, DataFolderName);
        }

        /// <summary>
        /// 获取指定账号的隔离数据目录。
        /// </summary>
        private static string GetUserDataFolderPath(string ownerKey)
        {
            return Path.Combine(GetSharedDataFolderPath(), UsersFolderName, ownerKey);
        }

        /// <summary>
        /// 获取旧版本使用的游戏根目录 UserData 路径。
        /// </summary>
        private static string GetLegacySharedDataFolderPath()
        {
            var dataPath = Application.dataPath;
            var gameRoot = Directory.GetParent(dataPath)?.FullName;
            if (string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = dataPath;
            }

            return Path.Combine(gameRoot, DataFolderName);
        }

        /// <summary>
        /// 首次按账号读进度时，将旧共享文件迁移给第一个登录账号。
        /// </summary>
        private static void TryMigrateLegacyProgressFile(string fileName, string targetPath)
        {
            var markerPath = Path.Combine(GetSharedDataFolderPath(), $"{MigrationMarkerPrefix}{fileName}.txt");
            if (File.Exists(markerPath))
            {
                return;
            }

            if (File.Exists(targetPath))
            {
                WriteMigrationMarker(markerPath, "target-exists");
                return;
            }

            if (TryCopyDecryptableLegacyFile(fileName, targetPath))
            {
                WriteMigrationMarker(markerPath, "copied");
            }
        }

        /// <summary>
        /// 若旧共享文件可由当前设备解密，则复制到目标路径。
        /// </summary>
        private static bool TryCopyDecryptableLegacyFile(string fileName, string targetPath)
        {
            try
            {
                var legacyPath = Path.Combine(GetLegacySharedDataFolderPath(), fileName);
                if (!File.Exists(legacyPath) || File.Exists(targetPath) || SamePath(legacyPath, targetPath))
                {
                    return false;
                }

                if (!CanDecryptLegacyFile(legacyPath))
                {
                    return false;
                }

                EnsureParentDirectory(targetPath);
                File.Copy(legacyPath, targetPath, false);
                Debug.Log($"[LocalUserDataPaths] 已迁移旧存档 {fileName} => {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalUserDataPaths] 迁移旧存档失败 {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查旧文件是否能用当前设备密钥解密出有效 JSON 文本。
        /// </summary>
        private static bool CanDecryptLegacyFile(string filePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                if (bytes == null || bytes.Length <= 16)
                {
                    return false;
                }

                var json = LocalDataCrypto.DecryptToUtf8(bytes);
                return !string.IsNullOrWhiteSpace(json);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 写入旧共享存档迁移标记。
        /// </summary>
        private static void WriteMigrationMarker(string markerPath, string reason)
        {
            try
            {
                EnsureParentDirectory(markerPath);
                File.WriteAllText(markerPath, reason ?? string.Empty, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalUserDataPaths] 写入迁移标记失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 基于账号名生成不泄露明文且可作为目录名的稳定键。
        /// </summary>
        private static string BuildUserFolderName(string userName)
        {
            var normalized = (userName ?? string.Empty).Trim();
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var builder = new StringBuilder("user_", 69);
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 只保留文件名部分，避免调用方传入路径导致越界写入。
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            return Path.GetFileName(fileName ?? string.Empty);
        }

        /// <summary>
        /// 比较两个路径是否指向同一规范化位置。
        /// </summary>
        private static bool SamePath(string left, string right)
        {
            var leftFull = Path.GetFullPath(left);
            var rightFull = Path.GetFullPath(right);
            return string.Equals(leftFull, rightFull, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
