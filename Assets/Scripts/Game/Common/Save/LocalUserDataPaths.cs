using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Auth;
using UnityEngine;

namespace Game.Common.Save
{
    /// <summary>
    /// 本地用户数据路径工具：负责共享账号库路径与当前账号进度目录的统一生成。
    /// </summary>
    public static class LocalUserDataPaths
    {
        /// <summary>
        /// 本地用户数据根目录名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 账号目录名前缀，避免目录名直接暴露原始账号。
        /// </summary>
        private const string UserFolderPrefix = "user_";

        /// <summary>
        /// 获取所有本地用户数据的共享根目录。
        /// </summary>
        public static string SharedDataFolderPath => Path.Combine(Application.persistentDataPath, DataFolderName);

        /// <summary>
        /// 获取共享数据文件路径，适用于账号库等不归属单一玩家进度的数据。
        /// </summary>
        /// <param name="fileName">共享数据文件名。</param>
        public static string GetSharedDataFilePath(string fileName)
        {
            return Path.Combine(SharedDataFolderPath, fileName ?? string.Empty);
        }

        /// <summary>
        /// 获取重制版早期共享数据文件路径，用于迁移旧进度文件。
        /// </summary>
        /// <param name="fileName">旧共享数据文件名。</param>
        public static string GetLegacySharedDataFilePath(string fileName)
        {
            return Path.Combine(LegacyGameRootDataFolderPath, fileName ?? string.Empty);
        }

        /// <summary>
        /// 尝试获取当前登录账号的进度目录；未登录时返回 false。
        /// </summary>
        /// <param name="folderPath">当前账号进度目录。</param>
        /// <param name="ownerKey">当前账号对应的稳定目录键。</param>
        public static bool TryGetCurrentUserDataFolderPath(out string folderPath, out string ownerKey)
        {
            var user = AccountStore.GetCurrentUser();
            if (string.IsNullOrWhiteSpace(user))
            {
                folderPath = string.Empty;
                ownerKey = string.Empty;
                return false;
            }

            ownerKey = BuildOwnerKey(user);
            folderPath = Path.Combine(SharedDataFolderPath, ownerKey);
            return true;
        }

        /// <summary>
        /// 基于账号名生成稳定目录键，避免特殊字符破坏路径结构。
        /// </summary>
        /// <param name="user">账号名。</param>
        private static string BuildOwnerKey(string user)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes((user ?? string.Empty).Trim()));
            var builder = new StringBuilder(UserFolderPrefix, UserFolderPrefix.Length + bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 获取重制版早期使用的游戏根目录 UserData 路径，用于一次性读取旧共享账号库。
        /// </summary>
        public static string LegacyGameRootDataFolderPath
        {
            get
            {
                var dataPath = Application.dataPath;
                var gameRootPath = Directory.GetParent(dataPath)?.FullName;
                if (string.IsNullOrEmpty(gameRootPath))
                {
                    gameRootPath = dataPath;
                }

                return Path.Combine(gameRootPath, DataFolderName);
            }
        }
    }
}
