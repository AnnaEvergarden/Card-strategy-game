using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 玩家进度路径工具：根据当前登录账号生成隔离的本地数据目录。
    /// </summary>
    public static class PlayerDataPath
    {
        #region Fields

        /// <summary>
        /// 本地数据根目录名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 按账号隔离的玩家进度目录名。
        /// </summary>
        private const string PlayerFolderName = "Players";

        #endregion

        #region Public API

        /// <summary>
        /// 尝试获取当前登录账号对应的稳定目录键。
        /// </summary>
        public static bool TryGetCurrentUserKey(out string userKey)
        {
            var currentUser = AccountStore.GetCurrentUser();
            if (string.IsNullOrWhiteSpace(currentUser))
            {
                userKey = string.Empty;
                return false;
            }

            userKey = BuildUserFolderName(currentUser.Trim());
            return true;
        }

        /// <summary>
        /// 尝试获取当前登录账号下指定玩家进度文件的完整路径。
        /// </summary>
        public static bool TryGetCurrentUserFilePath(string dataFileName, out string filePath, out string userKey)
        {
            filePath = string.Empty;
            userKey = string.Empty;
            if (string.IsNullOrWhiteSpace(dataFileName) || !TryGetCurrentUserKey(out userKey))
            {
                return false;
            }

            filePath = Path.Combine(GetPlayerFolderPath(userKey), dataFileName.Trim());
            return true;
        }

        /// <summary>
        /// 确保当前登录账号的玩家进度目录已创建。
        /// </summary>
        public static bool TryEnsureCurrentUserFolder(out string folderPath, out string userKey)
        {
            folderPath = string.Empty;
            if (!TryGetCurrentUserKey(out userKey))
            {
                return false;
            }

            folderPath = GetPlayerFolderPath(userKey);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取指定账号键对应的玩家进度目录。
        /// </summary>
        private static string GetPlayerFolderPath(string userKey)
        {
            return Path.Combine(GetDataRootPath(), PlayerFolderName, userKey);
        }

        /// <summary>
        /// 获取本地数据根目录（游戏根目录/UserData）。
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
        /// 用可读前缀加账号哈希生成目录名，避免非法字符和不同账号碰撞。
        /// </summary>
        private static string BuildUserFolderName(string userName)
        {
            var safeName = BuildSafeName(userName);
            return $"{safeName}_{HashUserName(userName)}";
        }

        /// <summary>
        /// 将账号名转换为适合路径片段的可读前缀。
        /// </summary>
        private static string BuildSafeName(string userName)
        {
            var builder = new StringBuilder();
            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < userName.Length && builder.Length < 48; i++)
            {
                var ch = userName[i];
                if (char.IsControl(ch) ||
                    Array.IndexOf(invalidChars, ch) >= 0 ||
                    "<>:\"/\\|?*".IndexOf(ch) >= 0)
                {
                    builder.Append('_');
                    continue;
                }

                builder.Append(ch);
            }

            return builder.Length > 0 ? builder.ToString() : "user";
        }

        /// <summary>
        /// 计算账号名哈希后缀，用于区分清理后名称相同的账号。
        /// </summary>
        private static string HashUserName(string userName)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(userName));
            var builder = new StringBuilder(16);
            for (var i = 0; i < 8 && i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        #endregion
    }
}
