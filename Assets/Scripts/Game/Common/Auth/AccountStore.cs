using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Common.Security;
using UnityEngine;

namespace Game.Common.Auth
{
    /// <summary>
    /// 账号存储服务：负责本地注册、登录、记住密码、当前账号状态与玩家数据目录隔离。
    /// </summary>
    public static class AccountStore
    {
        #region Keys

        /// <summary>
        /// 本地数据文件夹名。
        /// </summary>
        private const string DataFolderName = "UserData";

        /// <summary>
        /// 账号数据文件名（加密二进制）。
        /// </summary>
        private const string DataFileName = "account.dat";

        /// <summary>
        /// 多账号玩家数据的父目录名。
        /// </summary>
        private const string ProfilesFolderName = "Profiles";

        /// <summary>
        /// 未登录或账号库不可读时使用的游客数据目录名。
        /// </summary>
        private const string GuestProfileFolderName = "guest";

        /// <summary>
        /// 账号库文件读写锁。
        /// </summary>
        private static readonly object FileLock = new();

        #endregion

        #region Nested Models

        [Serializable]
        private sealed class AccountEntry
        {
            /// <summary>
            /// 账号名。
            /// </summary>
            public string user;

            /// <summary>
            /// 密码。
            /// </summary>
            public string pass;
        }

        [Serializable]
        private sealed class AccountDb
        {
            /// <summary>
            /// 账号列表。
            /// </summary>
            public List<AccountEntry> accounts = new();

            /// <summary>
            /// 当前登录账号。
            /// </summary>
            public string currentUser = string.Empty;

            /// <summary>
            /// 是否启用记住密码（默认启用）。
            /// </summary>
            public bool rememberEnabled = true;

            /// <summary>
            /// 记住的用户名。
            /// </summary>
            public string rememberUser = string.Empty;

            /// <summary>
            /// 记住的密码。
            /// </summary>
            public string rememberPass = string.Empty;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 注册账号：账号不存在时写入本地数据库。
        /// </summary>
        public static bool TryRegister(string user, string pass, out string error)
        {
            error = string.Empty;
            user = (user ?? string.Empty).Trim();
            pass = pass ?? string.Empty;

            if (string.IsNullOrEmpty(user))
            {
                error = "账号不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(pass))
            {
                error = "密码不能为空";
                return false;
            }

            if (!TryLoadDb(out var db, out var loadError))
            {
                error = $"账号数据读取失败，无法注册：{loadError}";
                return false;
            }

            if (Find(db, user) != null)
            {
                error = "账号已存在";
                return false;
            }

            db.accounts.Add(new AccountEntry { user = user, pass = pass });
            SaveDb(db);
            return true;
        }

        /// <summary>
        /// 登录账号：校验成功后写入当前登录账号。
        /// </summary>
        public static bool TryLogin(string user, string pass, out string error)
        {
            error = string.Empty;
            user = (user ?? string.Empty).Trim();
            pass = pass ?? string.Empty;

            if (!TryLoadDb(out var db, out var loadError))
            {
                error = $"账号数据读取失败，无法登录：{loadError}";
                return false;
            }

            var entry = Find(db, user);
            if (entry == null)
            {
                error = "账号不存在";
                return false;
            }

            if (!string.Equals(entry.pass, pass, StringComparison.Ordinal))
            {
                error = "密码错误";
                return false;
            }

            db.currentUser = user;
            SaveDb(db);
            return true;
        }

        /// <summary>
        /// 退出当前账号登录状态。
        /// </summary>
        public static void Logout()
        {
            if (!TryLoadDb(out var db, out var loadError))
            {
                Debug.LogWarning($"Logout skipped because account db failed to load: {loadError}");
                return;
            }

            db.currentUser = string.Empty;
            SaveDb(db);
        }

        /// <summary>
        /// 获取当前登录账号名。
        /// </summary>
        public static string GetCurrentUser()
        {
            var db = LoadDbOrDefault();
            return db.currentUser ?? string.Empty;
        }

        /// <summary>
        /// 获取当前账号对应的安全存储键，用于判断静态缓存是否属于同一账号。
        /// </summary>
        public static string GetCurrentUserStorageKey()
        {
            return BuildStorageKey(GetCurrentUser());
        }

        /// <summary>
        /// 获取当前账号的玩家数据目录，账号库不可读时落到游客目录，避免写入其他账号目录。
        /// </summary>
        public static string GetCurrentUserDataFolderPath()
        {
            return Path.Combine(GetDataFolderPath(), ProfilesFolderName, GetCurrentUserStorageKey());
        }

        /// <summary>
        /// 判断是否允许将旧版共享玩家数据迁移到当前账号目录。
        /// </summary>
        public static bool CanMigrateLegacyUserData()
        {
            if (!TryLoadDb(out var db, out _))
            {
                return false;
            }

            var currentUser = db.currentUser ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentUser) || db.accounts == null)
            {
                return false;
            }

            var validAccountCount = 0;
            var onlyAccountMatchesCurrent = false;
            for (var i = 0; i < db.accounts.Count; i++)
            {
                var entry = db.accounts[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.user))
                {
                    continue;
                }

                validAccountCount++;
                onlyAccountMatchesCurrent = string.Equals(entry.user, currentUser, StringComparison.Ordinal);
                if (validAccountCount > 1)
                {
                    return false;
                }
            }

            return validAccountCount == 1 && onlyAccountMatchesCurrent;
        }

        /// <summary>
        /// 设置是否启用记住密码。
        /// </summary>
        public static void SetRememberEnabled(bool enabled)
        {
            if (!TryLoadDb(out var db, out var loadError))
            {
                Debug.LogWarning($"Set remember enabled skipped because account db failed to load: {loadError}");
                return;
            }

            db.rememberEnabled = enabled;
            if (!enabled)
            {
                db.rememberUser = string.Empty;
                db.rememberPass = string.Empty;
            }
            SaveDb(db);
        }

        /// <summary>
        /// 获取是否启用记住密码（默认启用）。
        /// </summary>
        public static bool GetRememberEnabled()
        {
            var db = LoadDbOrDefault();
            return db.rememberEnabled;
        }

        /// <summary>
        /// 保存要记住的账号密码。
        /// </summary>
        public static void SaveRememberedCredentials(string user, string pass)
        {
            if (!TryLoadDb(out var db, out var loadError))
            {
                Debug.LogWarning($"Save remembered credentials skipped because account db failed to load: {loadError}");
                return;
            }

            if (!db.rememberEnabled) return;

            db.rememberUser = (user ?? string.Empty).Trim();
            db.rememberPass = pass ?? string.Empty;
            SaveDb(db);
        }

        /// <summary>
        /// 读取记住的账号密码。
        /// </summary>
        public static (string user, string pass) LoadRememberedCredentials()
        {
            var db = LoadDbOrDefault();
            if (!db.rememberEnabled) return (string.Empty, string.Empty);
            return (db.rememberUser ?? string.Empty, db.rememberPass ?? string.Empty);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 从本地读取账号数据库，读取失败时返回空数据库仅供只读查询使用。
        /// </summary>
        private static AccountDb LoadDbOrDefault()
        {
            return TryLoadDb(out var db, out _) ? db : new AccountDb();
        }

        /// <summary>
        /// 尝试从本地读取账号数据库；文件存在但损坏时返回 false，调用方不得写回默认库。
        /// </summary>
        private static bool TryLoadDb(out AccountDb db, out string error)
        {
            lock (FileLock)
            {
                db = new AccountDb();
                error = string.Empty;
                var filePath = GetDataFilePath();
                if (!File.Exists(filePath))
                {
                    return true;
                }

                try
                {
                    var encryptedBytes = File.ReadAllBytes(filePath);
                    if (encryptedBytes == null || encryptedBytes.Length <= 16)
                    {
                        throw new InvalidDataException("账号数据文件为空或长度无效");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(encryptedBytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException("账号数据解密结果为空");
                    }

                    db = JsonUtility.FromJson<AccountDb>(json) ?? new AccountDb();
                    NormalizeDb(db);
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    Debug.LogWarning($"Load account db failed: {ex.Message}");
                    db = new AccountDb();
                    return false;
                }
            }
        }

        /// <summary>
        /// 将账号数据库写入本地。
        /// </summary>
        private static void SaveDb(AccountDb db)
        {
            lock (FileLock)
            {
                try
                {
                    var folderPath = GetDataFolderPath();
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    var json = JsonUtility.ToJson(db);
                    var encryptedBytes = LocalDataCrypto.EncryptUtf8(json);
                    File.WriteAllBytes(GetDataFilePath(), encryptedBytes);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Save account db failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 规范化账号数据库字段，避免旧档缺字段时后续写入空引用。
        /// </summary>
        private static void NormalizeDb(AccountDb db)
        {
            db.accounts ??= new List<AccountEntry>();
            db.currentUser ??= string.Empty;
            db.rememberUser ??= string.Empty;
            db.rememberPass ??= string.Empty;
        }

        /// <summary>
        /// 在数据库中查找指定账号。
        /// </summary>
        private static AccountEntry Find(AccountDb db, string user)
        {
            if (db?.accounts == null) return null;
            for (var i = 0; i < db.accounts.Count; i++)
            {
                var e = db.accounts[i];
                if (e == null) continue;
                if (string.Equals(e.user, user, StringComparison.Ordinal)) return e;
            }
            return null;
        }

        /// <summary>
        /// 根据账号名生成文件夹安全的存储键，追加短哈希避免特殊字符归一化后撞名。
        /// </summary>
        private static string BuildStorageKey(string user)
        {
            user = (user ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(user))
            {
                return GuestProfileFolderName;
            }

            var safeNameBuilder = new StringBuilder(user.Length);
            for (var i = 0; i < user.Length; i++)
            {
                var c = user[i];
                safeNameBuilder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            }

            var safeName = safeNameBuilder.ToString().Trim('.');
            if (string.IsNullOrEmpty(safeName))
            {
                safeName = "user";
            }

            if (safeName.Length > 48)
            {
                safeName = safeName.Substring(0, 48);
            }

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(user));
            var hashBuilder = new StringBuilder(16);
            for (var i = 0; i < 8; i++)
            {
                hashBuilder.Append(hashBytes[i].ToString("x2"));
            }

            return $"{safeName}_{hashBuilder}";
        }

        /// <summary>
        /// 获取本地数据文件夹路径（游戏根目录/UserData）。
        /// </summary>
        private static string GetDataFolderPath()
        {
            var dataPath = Application.dataPath;
            var gameRootPath = Directory.GetParent(dataPath)?.FullName;
            if (string.IsNullOrEmpty(gameRootPath)) gameRootPath = dataPath;
            return Path.Combine(gameRootPath, DataFolderName);
        }

        /// <summary>
        /// 获取账号数据文件完整路径。
        /// </summary>
        private static string GetDataFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

        #endregion
    }
}

