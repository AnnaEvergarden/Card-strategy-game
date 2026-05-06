using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏 Resources 统一加载门面：集中路径、缓存与告警日志，避免业务层重复编写加载细节。
/// </summary>
public static class GameResourceLoader
{
    #region Fields

    /// <summary>
    /// 舰娘头像缓存：key 为 Resources 相对路径，value 为加载结果（可为 null，用于避免重复失败加载）。
    /// </summary>
    private static readonly Dictionary<string, Sprite> ShipgirlIconCache = new();

    /// <summary>
    /// 通用 Sprite 缓存：key 为 Resources 相对路径。
    /// </summary>
    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    /// <summary>
    /// 通用预制体缓存：key 为 Resources 相对路径。
    /// </summary>
    private static readonly Dictionary<string, GameObject> PrefabCache = new();

    /// <summary>
    /// 卡牌配置数据库缓存。
    /// </summary>
    private static CardConfigDatabaseSO _cardConfigDatabase;

    /// <summary>
    /// 建造卡池数据库缓存。
    /// </summary>
    private static BuildPoolDatabaseSO _buildPoolDatabase;

    /// <summary>
    /// 道具配置数据库缓存。
    /// </summary>
    private static ItemConfigDatabaseSO _itemConfigDatabase;

    /// <summary>
    /// 区域配置数据库缓存。
    /// </summary>
    private static LevelAreaDatabaseSO _levelAreaDatabase;

    #endregion

    #region Public API

    /// <summary>
    /// 加载卡牌配置数据库（带缓存）。
    /// </summary>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>卡牌数据库；缺失时为 null。</returns>
    public static CardConfigDatabaseSO LoadCardConfigDatabase(bool logOnMissing = true)
    {
        if (_cardConfigDatabase != null)
        {
            return _cardConfigDatabase;
        }

        _cardConfigDatabase = Resources.Load<CardConfigDatabaseSO>(GameResourcePaths.CardConfigDatabase);
        if (_cardConfigDatabase == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到卡牌数据库 Resources/{GameResourcePaths.CardConfigDatabase}。");
        }

        return _cardConfigDatabase;
    }

    /// <summary>
    /// 加载建造卡池数据库（带缓存）。
    /// </summary>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>卡池数据库；缺失时为 null。</returns>
    public static BuildPoolDatabaseSO LoadBuildPoolDatabase(bool logOnMissing = true)
    {
        if (_buildPoolDatabase != null)
        {
            return _buildPoolDatabase;
        }

        _buildPoolDatabase = Resources.Load<BuildPoolDatabaseSO>(GameResourcePaths.BuildPoolDatabase);
        if (_buildPoolDatabase == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到卡池数据库 Resources/{GameResourcePaths.BuildPoolDatabase}。");
        }

        return _buildPoolDatabase;
    }

    /// <summary>
    /// 加载道具配置数据库（带缓存）。
    /// </summary>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>道具数据库；缺失时为 null。</returns>
    public static ItemConfigDatabaseSO LoadItemConfigDatabase(bool logOnMissing = true)
    {
        if (_itemConfigDatabase != null)
        {
            return _itemConfigDatabase;
        }

        _itemConfigDatabase = Resources.Load<ItemConfigDatabaseSO>(GameResourcePaths.ItemConfigDatabase);
        if (_itemConfigDatabase == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到道具数据库 Resources/{GameResourcePaths.ItemConfigDatabase}。");
        }

        return _itemConfigDatabase;
    }

    /// <summary>
    /// 加载区域配置数据库（带缓存）。
    /// </summary>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>区域数据库；缺失时为 null。</returns>
    public static LevelAreaDatabaseSO LoadLevelAreaDatabase(bool logOnMissing = true)
    {
        if (_levelAreaDatabase != null)
        {
            return _levelAreaDatabase;
        }

        _levelAreaDatabase = Resources.Load<LevelAreaDatabaseSO>(GameResourcePaths.LevelAreaDatabase);
        if (_levelAreaDatabase == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到区域数据库 Resources/{GameResourcePaths.LevelAreaDatabase}。");
        }

        return _levelAreaDatabase;
    }

    /// <summary>
    /// 按资源路径加载 Sprite（带缓存）。
    /// </summary>
    /// <param name="path">Resources 相对路径，不含扩展名。</param>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>Sprite 资源；无效或缺失时为 null。</returns>
    public static Sprite LoadSprite(string path, bool logOnMissing = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalizedPath = path.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        if (SpriteCache.TryGetValue(normalizedPath, out var cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(normalizedPath);
        SpriteCache[normalizedPath] = sprite;
        if (sprite == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到 Sprite 资源 Resources/{normalizedPath}。");
        }

        return sprite;
    }

    /// <summary>
    /// 按资源路径加载预制体（带缓存）。
    /// </summary>
    /// <param name="path">Resources 相对路径，不含扩展名。</param>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>预制体资源；无效或缺失时为 null。</returns>
    public static GameObject LoadPrefab(string path, bool logOnMissing = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalizedPath = path.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        if (PrefabCache.TryGetValue(normalizedPath, out var cached))
        {
            return cached;
        }

        var prefab = Resources.Load<GameObject>(normalizedPath);
        PrefabCache[normalizedPath] = prefab;
        if (prefab == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到预制体资源 Resources/{normalizedPath}。");
        }

        return prefab;
    }

    /// <summary>
    /// 按英文名加载舰娘头像（带缓存）。
    /// </summary>
    /// <param name="englishName">舰娘英文名。</param>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>头像 Sprite；无效或缺失时为 null。</returns>
    public static Sprite LoadShipgirlIcon(string englishName, bool logOnMissing = false)
    {
        if (string.IsNullOrWhiteSpace(englishName))
        {
            return null;
        }

        var path = GameResourcePaths.BuildShipgirlIconPath(englishName);
        if (ShipgirlIconCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(path);
        ShipgirlIconCache[path] = sprite;
        if (sprite == null && logOnMissing)
        {
            Debug.LogWarning($"GameResourceLoader: 未找到舰娘头像 Resources/{path}。");
        }

        return sprite;
    }

    #endregion
}
