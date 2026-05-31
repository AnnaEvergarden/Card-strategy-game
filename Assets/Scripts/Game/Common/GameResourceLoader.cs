using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    /// 卡牌配置数据库缓存（按阵营；含加载失败时的 null 占位）。
    /// </summary>
    private static readonly Dictionary<ShipFaction, CardConfigDatabaseSO> CardConfigDatabaseByFaction = new();

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

    /// <summary>
    /// 技能配置数据库缓存（按阵营；含加载失败时的 null 占位）。
    /// </summary>
    private static readonly Dictionary<ShipFaction, SkillConfigDatabaseSO> SkillConfigDatabaseByFaction = new();

    /// <summary>
    /// 资源缺失时共用的半透明黑底占位图（运行时生成一次）。
    /// </summary>
    private static Sprite _imagePlaceholderSprite;

    /// <summary>
    /// 占位图纹理边长（像素）。
    /// </summary>
    private const int ImagePlaceholderSize = 1;

    /// <summary>
    /// 占位图颜色：半透明黑。
    /// </summary>
    private static readonly Color ImagePlaceholderColor = new(0f, 0f, 0f, 0.45f);

    #endregion

    #region Public API

    /// <summary>
    /// 按阵营加载卡牌配置数据库（带缓存）；找不到阵营库时回退旧版单文件库。
    /// </summary>
    /// <param name="faction">目标阵营。</param>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>卡牌数据库；缺失时为 null。</returns>
    public static CardConfigDatabaseSO LoadCardConfigDatabase(ShipFaction faction, bool logOnMissing = true)
    {
        if (CardConfigDatabaseByFaction.TryGetValue(faction, out var cached))
        {
            if (cached != null || !logOnMissing)
            {
                return cached;
            }
        }

        var path = GameResourcePaths.BuildCardConfigDatabaseResourcePath(faction);
        var db = Resources.Load<CardConfigDatabaseSO>(path);
        if (db == null)
        {
            db = Resources.Load<CardConfigDatabaseSO>(GameResourcePaths.CardConfigDatabaseLegacy);
            if (db != null && logOnMissing)
            {
                Debug.Log(
                    $"GameResourceLoader: 未找到阵营卡牌库 Resources/{path}，已回退旧版 Resources/{GameResourcePaths.CardConfigDatabaseLegacy}。");
            }
        }

        CardConfigDatabaseByFaction[faction] = db;

        if (db == null && logOnMissing)
        {
            Debug.LogWarning(
                $"GameResourceLoader: 未找到卡牌数据库 Resources/{path}，且旧版 {GameResourcePaths.CardConfigDatabaseLegacy} 亦不可用。");
        }

        return db;
    }

    /// <summary>
    /// 加载卡牌配置数据库（向后兼容，等效于 <see cref="LoadCardConfigDatabase(ShipFaction, bool)"/> 传 <see cref="ShipFaction.Other"/>）。
    /// </summary>
    public static CardConfigDatabaseSO LoadCardConfigDatabase(bool logOnMissing = true)
    {
        return LoadCardConfigDatabase(ShipFaction.Other, logOnMissing);
    }

    /// <summary>
    /// 卡牌配置字典缓存（cardId 到 CardConfigSO，由 <see cref="GetCardConfigMap"/> 构建并缓存）。
    /// </summary>
    private static Dictionary<string, CardConfigSO> _cardConfigMap;

    /// <summary>
    /// 获取 cardId 到 CardConfigSO 的字典（线程安全懒加载并缓存；供各面板复用，避免 8 处重复构建）。
    /// 汇聚所有阵营的卡牌，相同 cardId 以最后加载的阵营为准。
    /// </summary>
    /// <param name="logOnMissing">数据库缺失时是否告警。</param>
    /// <returns>cardId 到配置的字典；数据库缺失时返回空字典。</returns>
    public static Dictionary<string, CardConfigSO> GetCardConfigMap(bool logOnMissing = true)
    {
        if (_cardConfigMap != null)
        {
            return _cardConfigMap;
        }

        var map = new Dictionary<string, CardConfigSO>();
        var factions = (ShipFaction[])System.Enum.GetValues(typeof(ShipFaction));
        for (var i = 0; i < factions.Length; i++)
        {
            var db = LoadCardConfigDatabase(factions[i], logOnMissing: false);
            if (db?.Cards == null)
            {
                continue;
            }

            for (var j = 0; j < db.Cards.Count; j++)
            {
                var cfg = db.Cards[j];
                if (cfg == null || string.IsNullOrWhiteSpace(cfg.CardId))
                {
                    continue;
                }

                map[cfg.CardId.Trim()] = cfg;
            }
        }

        _cardConfigMap = map;
        return _cardConfigMap;
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
    /// 按舰娘阵营加载对应技能库（带缓存）；路径见 <see cref="GameResourcePaths.BuildSkillConfigDatabaseResourcePath"/>。
    /// 若不存在则回退 <see cref="GameResourcePaths.SkillConfigDatabaseLegacy"/>。
    /// </summary>
    /// <param name="faction">与 <see cref="CardConfigSO.Faction"/> 一致。</param>
    /// <param name="logOnMissing">阵营库与旧版均缺失时是否告警。</param>
    /// <returns>技能数据库；缺失时为 null。</returns>
    public static SkillConfigDatabaseSO LoadSkillConfigDatabase(ShipFaction faction, bool logOnMissing = true)
    {
        if (SkillConfigDatabaseByFaction.TryGetValue(faction, out var cached))
        {
            if (cached != null || !logOnMissing)
            {
                return cached;
            }
        }

        var path = GameResourcePaths.BuildSkillConfigDatabaseResourcePath(faction);
        var db = Resources.Load<SkillConfigDatabaseSO>(path);
        if (db == null)
        {
            db = Resources.Load<SkillConfigDatabaseSO>(GameResourcePaths.SkillConfigDatabaseLegacy);
            if (db != null && logOnMissing)
            {
                Debug.Log(
                    $"GameResourceLoader: 未找到阵营技能库 Resources/{path}，已回退旧版 Resources/{GameResourcePaths.SkillConfigDatabaseLegacy}。");
            }
        }

        SkillConfigDatabaseByFaction[faction] = db;

        if (db == null && logOnMissing)
        {
            Debug.LogWarning(
                $"GameResourceLoader: 未找到技能数据库 Resources/{path}，且旧版 {GameResourcePaths.SkillConfigDatabaseLegacy} 亦不可用。");
        }

        return db;
    }

    /// <summary>
    /// 汇总各阵营技能库中的全部 <see cref="SkillConfigSO"/>（按 skillId 去重）。
    /// </summary>
    /// <param name="destination">输出列表（先 Clear）。</param>
    /// <param name="logOnMissing">库缺失时是否告警。</param>
    public static void CopyAllSkillConfigs(List<SkillConfigSO> destination, bool logOnMissing = false)
    {
        destination?.Clear();
        if (destination == null)
        {
            return;
        }

        var seen = new HashSet<string>();
        var factions = (ShipFaction[])System.Enum.GetValues(typeof(ShipFaction));
        for (var i = 0; i < factions.Length; i++)
        {
            var db = LoadSkillConfigDatabase(factions[i], logOnMissing);
            if (db?.Skills == null)
            {
                continue;
            }

            var skills = db.Skills;
            for (var s = 0; s < skills.Count; s++)
            {
                var cfg = skills[s];
                if (cfg == null || string.IsNullOrWhiteSpace(cfg.SkillId))
                {
                    continue;
                }

                var key = cfg.SkillId.Trim();
                if (!seen.Add(key))
                {
                    continue;
                }

                destination.Add(cfg);
            }
        }
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
    /// 获取半透明黑底占位 Sprite（运行时生成并缓存；用于头像等资源缺失时仍显示 Image）。
    /// </summary>
    /// <returns>可赋给 <see cref="Image.sprite"/> 的占位图。</returns>
    public static Sprite GetImagePlaceholderSprite()
    {
        if (_imagePlaceholderSprite != null)
        {
            return _imagePlaceholderSprite;
        }

        var tex = new Texture2D(ImagePlaceholderSize, ImagePlaceholderSize, TextureFormat.RGBA32, false)
        {
            name = "UIImagePlaceholder_Texture",
            hideFlags = HideFlags.HideAndDontSave
        };

        tex.SetPixel(0, 0, ImagePlaceholderColor);
        tex.Apply(false, true);

        _imagePlaceholderSprite = Sprite.Create(
            tex,
            new Rect(0, 0, ImagePlaceholderSize, ImagePlaceholderSize),
            new Vector2(0.5f, 0.5f),
            100f);
        _imagePlaceholderSprite.name = "UIImagePlaceholder_Sprite";
        return _imagePlaceholderSprite;
    }

    /// <summary>
    /// 按英文名加载舰娘头像；路径无效或 Resources 中不存在时返回 <see cref="GetImagePlaceholderSprite"/>。
    /// </summary>
    /// <param name="englishName">舰娘英文名。</param>
    /// <param name="logOnMissing">缺失真实头像时是否输出告警。</param>
    /// <returns>头像或占位 Sprite（不为 null）。</returns>
    public static Sprite LoadShipgirlIcon(string englishName, bool logOnMissing = false)
    {
        if (string.IsNullOrWhiteSpace(englishName))
        {
            return GetImagePlaceholderSprite();
        }

        var path = GameResourcePaths.BuildShipgirlIconPath(englishName);
        if (ShipgirlIconCache.TryGetValue(path, out var cached) && cached != null)
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            if (logOnMissing)
            {
                Debug.LogWarning($"GameResourceLoader: 未找到舰娘头像 Resources/{path}，已使用半透明黑底占位图。");
            }

            sprite = GetImagePlaceholderSprite();
        }

        ShipgirlIconCache[path] = sprite;
        return sprite;
    }

    /// <summary>
    /// 将舰娘头像赋给 Image；无配置名或资源缺失时使用占位图并保持 Image 可见。
    /// </summary>
    /// <param name="image">目标 Image。</param>
    /// <param name="englishName">舰娘英文名（可为空）。</param>
    /// <param name="logOnMissing">缺失真实头像时是否输出告警。</param>
    public static void ApplyShipgirlIconToImage(Image image, string englishName, bool logOnMissing = false)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;
        image.sprite = LoadShipgirlIcon(englishName, logOnMissing);
        image.enabled = true;
    }

    /// <summary>
    /// 按技能配置解析路径并加载图标（<see cref="SkillConfigSO.ResolveSkillIconResourcePath"/>）。
    /// </summary>
    /// <param name="skill">技能配置。</param>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    /// <returns>Sprite；路径无效或资源不存在时为 null。</returns>
    public static Sprite LoadSkillIcon(SkillConfigSO skill, bool logOnMissing = false)
    {
        if (skill == null)
        {
            return null;
        }

        var path = skill.ResolveSkillIconResourcePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return LoadSprite(path, logOnMissing);
    }

    /// <summary>
    /// 将技能图标赋给 Image；无配置或加载失败时使用占位图（与战斗技能栏一致）。
    /// </summary>
    /// <param name="image">目标 Image。</param>
    /// <param name="skill">技能配置。</param>
    /// <param name="logOnMissing">缺失时是否输出告警。</param>
    public static void ApplySkillIconToImage(Image image, SkillConfigSO skill, bool logOnMissing = false)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;
        var sprite = LoadSkillIcon(skill, logOnMissing);
        ApplySpriteOrPlaceholderToImage(image, sprite);
    }

    /// <summary>
    /// 将 Sprite 赋给 Image；<paramref name="sprite"/> 为 null 时使用占位图。
    /// </summary>
    /// <param name="image">目标 Image。</param>
    /// <param name="sprite">已加载的 Sprite，可为 null。</param>
    public static void ApplySpriteOrPlaceholderToImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;
        image.sprite = sprite != null ? sprite : GetImagePlaceholderSprite();
        image.enabled = true;
    }

    #endregion
}
