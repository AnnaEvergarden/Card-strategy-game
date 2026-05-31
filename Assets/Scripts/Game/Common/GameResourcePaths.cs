/// <summary>
/// 游戏内固定 Resources 路径集中定义。
/// 统一维护路径字符串，避免各模块散落魔法字符串。
/// </summary>
public static class GameResourcePaths
{
    #region Fields

    /// <summary>
    /// 卡牌配置数据库 Resources 根目录（相对路径，不含扩展名）。
    /// </summary>
    public const string CardDatabaseResourcesFolder = "ScriptableObjects/Database/Card";

    /// <summary>
    /// 旧版单文件卡牌库（无阵营后缀）；按阵营加载失败时回退到此路径。
    /// </summary>
    public const string CardConfigDatabaseLegacy = "ScriptableObjects/Database/Card/CardConfigDatabase";

    /// <summary>
    /// 构建阵营卡牌库 Resources 路径（不含扩展名）。
    /// 资产命名约定：<c>CardConfigDatabase_{阵营枚举名}</c>，例如 <c>CardConfigDatabase_EagleUnion</c>。
    /// </summary>
    /// <param name="faction">舰娘阵营。</param>
    /// <returns>例如 <c>ScriptableObjects/Database/Card/CardConfigDatabase_EagleUnion</c>。</returns>
    public static string BuildCardConfigDatabaseResourcePath(ShipFaction faction)
    {
        return $"{CardDatabaseResourcesFolder}/CardConfigDatabase_{faction}";
    }

    /// <summary>
    /// 技能库 Resources 根目录（相对路径，不含扩展名）。
    /// </summary>
    public const string SkillDatabaseResourcesFolder = "ScriptableObjects/Database/Skill";

    /// <summary>
    /// 旧版单文件技能库（无阵营后缀）；按阵营加载失败时回退到此路径。
    /// </summary>
    public const string SkillConfigDatabaseLegacy = "ScriptableObjects/Database/Skill/SkillConfigDatabase";

    /// <summary>
    /// 建造卡池数据库路径（Resources 相对路径，不含扩展名）。
    /// </summary>
    public const string BuildPoolDatabase = "ScriptableObjects/Database/Pool/BuildPoolDatabase";

    /// <summary>
    /// 道具配置数据库路径（Resources 相对路径，不含扩展名）。
    /// </summary>
    public const string ItemConfigDatabase = "ScriptableObjects/Database/Item/ItemConfigDatabase";

    /// <summary>
    /// 区域配置数据库路径（Resources 相对路径，不含扩展名）。
    /// </summary>
    public const string LevelAreaDatabase = "ScriptableObjects/Database/Level/LevelAreaDatabase";

    /// <summary>
    /// 舰娘头像根路径（Resources 相对路径，不含扩展名）。
    /// </summary>
    public const string ShipgirlIconRoot = "Art/Icon/Shipgirl";

    /// <summary>
    /// 技能图标根路径（Resources 相对路径，不含扩展名）；默认与 <see cref="BuildSkillIconPath"/> 拼接技能展示名。
    /// </summary>
    public const string SkillIconRoot = "Art/Icon/Skills";

    #endregion

    #region Public API

    /// <summary>
    /// 构建阵营技能库 Resources 路径（不含扩展名）。
    /// 资产命名约定：<c>SkillConfigDatabase_{阵营枚举名}</c>，例如 <c>SkillConfigDatabase_EagleUnion</c>（路径避免中文编码问题）。
    /// </summary>
    /// <param name="faction">舰娘/技能阵营。</param>
    /// <returns>例如 <c>ScriptableObjects/Database/Skill/SkillConfigDatabase_EagleUnion</c>。</returns>
    public static string BuildSkillConfigDatabaseResourcePath(ShipFaction faction)
    {
        return $"{SkillDatabaseResourcesFolder}/SkillConfigDatabase_{faction}";
    }

    /// <summary>
    /// 构建舰娘头像资源路径：根目录 + 英文名。
    /// </summary>
    /// <param name="englishName">舰娘英文名（可含斜杠；会在内部标准化）。</param>
    /// <returns>可传入 <see cref="UnityEngine.Resources.Load{T}(string)"/> 的相对路径。</returns>
    public static string BuildShipgirlIconPath(string englishName)
    {
        var normalizedName = string.IsNullOrWhiteSpace(englishName)
            ? string.Empty
            : englishName.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(normalizedName)
            ? ShipgirlIconRoot
            : $"{ShipgirlIconRoot}/{normalizedName}";
    }

    /// <summary>
    /// 构建技能图标 Resources 路径：根目录 + 技能展示名（<see cref="SkillConfigSO.DisplayName"/>）。
    /// </summary>
    /// <param name="displayName">技能展示名称。</param>
    /// <returns>可传入 <see cref="UnityEngine.Resources.Load{T}(string)"/> 的相对路径。</returns>
    public static string BuildSkillIconPath(string displayName)
    {
        var normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? string.Empty
            : displayName.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(normalizedName)
            ? SkillIconRoot
            : $"{SkillIconRoot}/{normalizedName}";
    }

    #endregion
}
