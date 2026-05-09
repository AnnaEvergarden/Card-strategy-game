using System;
using System.IO;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 资源存储服务：持久化玩家金币、钻石、船票，并提供安全的增减接口（加密存储）。
/// </summary>
public static class CurrencyStore
{
    #region Fields

    /// <summary>
    /// 资源数据文件夹名。
    /// </summary>
    private const string DataFolderName = "UserData";

    /// <summary>
    /// 加密后的资源数据文件名。
    /// </summary>
    private const string DataFileName = "currency.dat";

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private static CurrencyData _cached = new();

    /// <summary>
    /// 是否已经从磁盘加载过或通过保存建立过可信缓存。
    /// </summary>
    private static bool _hasCachedData;

    /// <summary>
    /// 最近一次加载是否失败；失败后禁止自动保存覆盖原文件。
    /// </summary>
    private static bool _saveBlockedByLoadFailure;

    /// <summary>
    /// 文件读写锁。
    /// </summary>
    private static readonly object FileLock = new();

    #endregion

    #region Nested Models

    /// <summary>
    /// 资源快照数据。
    /// </summary>
    [Serializable]
    public sealed class CurrencyData
    {
        /// <summary>
        /// 金币数量。
        /// </summary>
        public int gold;

        /// <summary>
        /// 钻石数量。
        /// </summary>
        public int diamond;

        /// <summary>
        /// 船票数量。
        /// </summary>
        public int shipTicket;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 读取资源数据；不存在时返回默认初始值。
    /// </summary>
    public static CurrencyData Load()
    {
        lock (FileLock)
        {
            try
            {
                var path = GetEncryptedFilePath();
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _cached = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
                            Normalize(_cached);
                            MarkCacheReadyForSave();
                            return _cached;
                        }
                    }

                    Debug.LogWarning($"Load currency failed: encrypted file is invalid => {path}");
                    _cached = CreateDefault();
                    MarkLoadFailure();
                    return _cached;
                }

                _cached = CreateDefault();
                MarkCacheReadyForSave();
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                _cached = CreateDefault();
                MarkLoadFailure();
                return _cached;
            }
        }
    }

    /// <summary>
    /// 保存资源数据。
    /// </summary>
    public static void Save(CurrencyData data)
    {
        lock (FileLock)
        {
            try
            {
                if (_saveBlockedByLoadFailure)
                {
                    Debug.LogError("Save currency blocked: last load failed, refusing to overwrite existing data.");
                    return;
                }

                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? CreateDefault();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(GetEncryptedFilePath(), encrypted);
                MarkCacheReadyForSave();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save currency failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存当前缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        lock (FileLock)
        {
            if (!_hasCachedData)
            {
                // 自动保存前先加载磁盘数据，避免用静态初始空对象覆盖真实存档。
                Load();
            }

            if (_saveBlockedByLoadFailure)
            {
                Debug.LogError("Save current currency skipped: cached data came from a failed load.");
                return;
            }
        }

        Save(_cached ?? CreateDefault());
    }

    /// <summary>
    /// 尝试一次性消耗多种资源；任意资源不足则失败并且不扣减。
    /// </summary>
    public static bool TryConsume(int gold, int diamond, int shipTicket)
    {
        if (gold < 0 || diamond < 0 || shipTicket < 0)
        {
            return false;
        }

        lock (FileLock)
        {
            var data = Load();
            if (_saveBlockedByLoadFailure)
            {
                Debug.LogError("Consume currency blocked: current data was not loaded safely.");
                return false;
            }

            if (data.gold < gold || data.diamond < diamond || data.shipTicket < shipTicket)
            {
                return false;
            }

            data.gold -= gold;
            data.diamond -= diamond;
            data.shipTicket -= shipTicket;
            SaveCurrent();
            return true;
        }
    }

    /// <summary>
    /// 增加资源（负数将被忽略）。
    /// </summary>
    public static void Add(int gold, int diamond, int shipTicket)
    {
        lock (FileLock)
        {
            var data = Load();
            if (_saveBlockedByLoadFailure)
            {
                Debug.LogError("Add currency blocked: current data was not loaded safely.");
                return;
            }

            if (gold > 0) data.gold += gold;
            if (diamond > 0) data.diamond += diamond;
            if (shipTicket > 0) data.shipTicket += shipTicket;
            SaveCurrent();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 创建默认资源（用于首次进入）。
    /// </summary>
    private static CurrencyData CreateDefault()
    {
        return new CurrencyData
        {
            gold = 10000,
            diamond = 100,
            shipTicket = 20
        };
    }

    /// <summary>
    /// 规范化资源值，避免负数。
    /// </summary>
    private static void Normalize(CurrencyData data)
    {
        data.gold = Mathf.Max(0, data.gold);
        data.diamond = Mathf.Max(0, data.diamond);
        data.shipTicket = Mathf.Max(0, data.shipTicket);
    }

    /// <summary>
    /// 标记当前缓存已经可信，可被后续自动保存写回。
    /// </summary>
    private static void MarkCacheReadyForSave()
    {
        _hasCachedData = true;
        _saveBlockedByLoadFailure = false;
    }

    /// <summary>
    /// 标记加载失败，后续自动保存必须跳过以保护原始文件。
    /// </summary>
    private static void MarkLoadFailure()
    {
        _hasCachedData = true;
        _saveBlockedByLoadFailure = true;
    }

    /// <summary>
    /// 获取资源数据目录（游戏根目录/UserData）。
    /// </summary>
    private static string GetDataFolderPath()
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
    /// 获取资源加密文件完整路径。
    /// </summary>
    private static string GetEncryptedFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

    #endregion
}
