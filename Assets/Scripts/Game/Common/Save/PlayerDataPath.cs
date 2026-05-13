using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 玩家进度文件路径辅助类：将货币、背包、卡牌仓库与编队数据隔离到当前账号目录。
    /// </summary>
    public static class PlayerDataPath
    {
        #region Fields

        /// <summary>
        /// 本地数据根目录名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 按账号隔离玩家进度的子目录名。
        /// </summary>
        private const string PlayerDataFolderName = "Players";

        /// <summary>
        /// 没有当前账号时使用的临时目录名，避免把缓存误写回其他账号。
        /// </summary>
        private const string AnonymousPlayerKey = "_anonymous";

        /// <summary>
        /// 遗留全局存档迁移后的备份后缀。
        /// </summary>
        private const string LegacyBackupSuffix = ".legacy_migrated";

        #endregion

        #region Public API

        /// <summary>
        /// 获取当前账号对应的玩家进度文件路径；需要时会把旧版全局文件迁移到首个登录账号目录。
        /// </summary>
        public static string GetCurrentPlayerFilePath(string fileName, bool migrateLegacyFile = false)
        {
            var safeFileName = GetSafeFileName(fileName);
            var currentUser = AccountStore.GetCurrentUser();
            var hasLoggedInUser = !string.IsNullOrWhiteSpace(currentUser);
            var playerFolderPath = GetCurrentPlayerFolderPath(currentUser);
            var scopedPath = Path.Combine(playerFolderPath, safeFileName);

            if (migrateLegacyFile && hasLoggedInUser)
            {
                TryMigrateLegacyFile(safeFileName, scopedPath);
            }

            return scopedPath;
        }

        /// <summary>
        /// 获取旧版未按账号隔离时使用的全局文件路径。
        /// </summary>
        public static string GetLegacyFilePath(string fileName)
        {
            return Path.Combine(GetDataRootPath(), GetSafeFileName(fileName));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取当前账号的玩家进度目录路径。
        /// </summary>
        private static string GetCurrentPlayerFolderPath(string currentUser)
        {
            var playerKey = BuildPlayerKey(currentUser);
            return Path.Combine(GetDataRootPath(), PlayerDataFolderName, playerKey);
        }

        /// <summary>
        /// 获取本地数据根目录路径（游戏根目录/UserData）。
        /// </summary>
        private static string GetDataRootPath()
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
        /// 基于账号名生成稳定且可作为目录名使用的账号键。
        /// </summary>
        private static string BuildPlayerKey(string currentUser)
        {
            if (string.IsNullOrWhiteSpace(currentUser))
            {
                return AnonymousPlayerKey;
            }

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(currentUser.Trim());
            var hash = sha256.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 清理文件名参数，防止路径分隔符进入玩家目录拼接。
        /// </summary>
        private static string GetSafeFileName(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                throw new ArgumentException("玩家进度文件名不能为空。", nameof(fileName));
            }

            return safeFileName;
        }

        /// <summary>
        /// 将旧版全局玩家进度文件迁移到当前账号目录，避免第二个账号继续读取同一份进度。
        /// </summary>
        private static void TryMigrateLegacyFile(string safeFileName, string scopedPath)
        {
            var legacyPath = GetLegacyFilePath(safeFileName);
            if (File.Exists(scopedPath) || !File.Exists(legacyPath))
            {
                return;
            }

            try
            {
                var scopedFolder = Path.GetDirectoryName(scopedPath);
                if (!string.IsNullOrEmpty(scopedFolder) && !Directory.Exists(scopedFolder))
                {
                    Directory.CreateDirectory(scopedFolder);
                }

                File.Move(legacyPath, scopedPath);
                TryCreateLegacyBackup(scopedPath, legacyPath);
                Debug.Log($"[PlayerDataPath] 已迁移旧版玩家进度文件：{safeFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerDataPath] 迁移旧版玩家进度文件失败：{safeFileName}, {ex.Message}");
            }
        }

        /// <summary>
        /// 为已迁移文件保留一份只读排查备份；备份失败不影响主流程。
        /// </summary>
        private static void TryCreateLegacyBackup(string scopedPath, string legacyPath)
        {
            try
            {
                var backupPath = legacyPath + LegacyBackupSuffix;
                if (File.Exists(backupPath))
                {
                    backupPath = $"{legacyPath}.{DateTime.UtcNow:yyyyMMddHHmmss}{LegacyBackupSuffix}";
                }

                File.Copy(scopedPath, backupPath, false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerDataPath] 创建旧版玩家进度备份失败：{ex.Message}");
            }
        }

        #endregion
    }
}
