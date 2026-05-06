using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Security;
using UnityEngine;

namespace Game.Common.Auth
{
    /// <summary>
    /// 账号存储服务：负责本地注册、登录、记住密码和当前账号状态。
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

            var db = LoadDb();
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

            var db = LoadDb();
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
            var db = LoadDb();
            db.currentUser = string.Empty;
            SaveDb(db);
        }

        /// <summary>
        /// 获取当前登录账号名。
        /// </summary>
        public static string GetCurrentUser()
        {
            var db = LoadDb();
            return db.currentUser ?? string.Empty;
        }

        /// <summary>
        /// 设置是否启用记住密码。
        /// </summary>
        public static void SetRememberEnabled(bool enabled)
        {
            var db = LoadDb();
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
            var db = LoadDb();
            return db.rememberEnabled;
        }

        /// <summary>
        /// 保存要记住的账号密码。
        /// </summary>
        public static void SaveRememberedCredentials(string user, string pass)
        {
            var db = LoadDb();
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
            var db = LoadDb();
            if (!db.rememberEnabled) return (string.Empty, string.Empty);
            return (db.rememberUser ?? string.Empty, db.rememberPass ?? string.Empty);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 从本地读取账号数据库。
        /// </summary>
        private static AccountDb LoadDb()
        {
            lock (FileLock)
            {
                var filePath = GetDataFilePath();
                if (!File.Exists(filePath)) return new AccountDb();

                try
                {
                    var encryptedBytes = File.ReadAllBytes(filePath);
                    if (encryptedBytes == null || encryptedBytes.Length <= 16) return new AccountDb();

                    var json = LocalDataCrypto.DecryptToUtf8(encryptedBytes);
                    var db = JsonUtility.FromJson<AccountDb>(json);
                    return db ?? new AccountDb();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Load account db failed: {ex.Message}");
                    return new AccountDb();
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

