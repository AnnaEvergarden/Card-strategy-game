/// <summary>
/// 游戏内固定 Resources 路径集中定义。
/// 统一维护路径字符串，避免各模块散落魔法字符串。
/// </summary>
public static class GameResourcePaths
{
    #region Fields

    /// <summary>
    /// 卡牌配置数据库路径（Resources 相对路径，不含扩展名）。
    /// </summary>
    public const string CardConfigDatabase = "ScriptableObjects/Database/Card/CardConfigDatabase";

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

    #endregion

    #region Public API

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

    #endregion
}
