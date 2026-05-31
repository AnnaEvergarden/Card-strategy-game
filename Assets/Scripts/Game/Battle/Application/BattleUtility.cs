using UnityEngine;

/// <summary>
/// 战斗系统共享工具：舰娘显示名解析、阵营解析等。
/// </summary>
public static class BattleUtility
{
    /// <summary>
    /// 将 cardId 解析为舰娘显示名（用于调试日志和 UI）。
    /// </summary>
    public static string GetShipDisplayName(string cardId)
    {
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && !string.IsNullOrWhiteSpace(cardId) && map.TryGetValue(cardId.Trim(), out var cfg) && cfg != null)
        {
            return cfg.DisplayName;
        }
        return cardId ?? string.Empty;
    }

    /// <summary>
    /// 将 cardId 解析为卡牌所属阵营。
    /// </summary>
    public static ShipFaction GetCardFaction(string cardId)
    {
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && !string.IsNullOrWhiteSpace(cardId) && map.TryGetValue(cardId.Trim(), out var cfg) && cfg != null)
        {
            return cfg.Faction;
        }
        return ShipFaction.Other;
    }

    /// <summary>
    /// 从配置表查询指定 cardId 的最大 HP。
    /// </summary>
    public static int ResolveMaxHp(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return 0;
        }
        var configMap = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (configMap != null && configMap.TryGetValue(cardId, out var cfg) && cfg != null)
        {
            return cfg.HP;
        }
        return 0;
    }
}
