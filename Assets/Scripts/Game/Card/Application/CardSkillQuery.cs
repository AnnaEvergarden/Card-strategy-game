using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 舰娘技能查询：根据 cardId 从卡牌库与技能库解析至多 <see cref="CardConfigSO.MaxSkillsPerCard"/> 条技能配置。
/// </summary>
public static class CardSkillQuery
{
    #region Public API

    /// <summary>
    /// 解析指定舰娘在静态配置中声明的技能（按顺序，跳过表中缺失的 skillId）。
    /// </summary>
    /// <param name="cardId">舰娘 cardId。</param>
    /// <param name="faction">舰娘阵营（用于定位分阵营卡牌库）。</param>
    /// <param name="destination">输出列表（会先 Clear）。</param>
    public static void ResolveSkillsForCard(string cardId, ShipFaction faction, List<SkillConfigSO> destination)
    {
        destination?.Clear();
        if (destination == null || string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        var cardDb = GameResourceLoader.LoadCardConfigDatabase(faction, logOnMissing: false);
        CardConfigSO cardCfg = null;
        if (cardDb?.Cards != null)
        {
            var trimmed = cardId.Trim();
            for (var i = 0; i < cardDb.Cards.Count; i++)
            {
                var c = cardDb.Cards[i];
                if (c != null && string.Equals(c.CardId.Trim(), trimmed, System.StringComparison.Ordinal))
                {
                    cardCfg = c;
                    break;
                }
            }
        }

        // 未在分阵营库中找到，回退到其他阵营库
        if (cardCfg == null)
        {
            var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
            if (map != null && map.TryGetValue(cardId.Trim(), out var fallback) && fallback != null)
            {
                cardCfg = fallback;
            }
        }

        if (cardCfg == null)
        {
            Debug.LogWarning($"[CardSkillQuery] 未找到 {ResolveShipName(cardId)} 的卡牌配置，请确认卡牌库中存在该条目");
            return;
        }

        var refs = cardCfg.GetSkillRefsOrdered();
        if (refs.Count > 0)
        {
            for (var i = 0; i < refs.Count; i++)
            {
                var sk = refs[i];
                if (sk == null || sk.ShipFaction != cardCfg.Faction)
                {
                    if (sk != null)
                    {
                        Debug.LogWarning($"[CardSkillQuery] {cardCfg.DisplayName} 技能 {sk.SkillId} 阵营 {sk.ShipFaction} 与卡牌阵营 {cardCfg.Faction} 不匹配，已跳过");
                    }
                    continue;
                }

                destination.Add(sk);
            }

            return;
        }

        var skillDb = GameResourceLoader.LoadSkillConfigDatabase(cardCfg.Faction, logOnMissing: false);
        if (skillDb == null)
        {
            Debug.LogWarning($"[CardSkillQuery] {cardCfg.DisplayName} 阵营 {cardCfg.Faction} 的技能库为空");
            return;
        }

        var ids = cardCfg.GetSkillIdsOrdered();
        Debug.Log($"[CardSkillQuery] {cardCfg.DisplayName} 技能IDs={ids.Count}个, 阵营={cardCfg.Faction}");
        for (var i = 0; i < ids.Count; i++)
        {
            if (skillDb.TryGet(ids[i], out var sk) && sk != null)
            {
                if (sk.ShipFaction != cardCfg.Faction)
                {
                    Debug.LogWarning($"[CardSkillQuery] {cardCfg.DisplayName} 技能 {ids[i]} 解析到阵营 {sk.ShipFaction} 而非 {cardCfg.Faction}，可能加载了错误的技能库，已跳过");
                    continue;
                }

                destination.Add(sk);
            }
            else
            {
                Debug.LogWarning($"[CardSkillQuery] {cardCfg.DisplayName} 技能ID={ids[i]} 在阵营 {cardCfg.Faction} 技能库中未找到");
            }
        }

        Debug.Log($"[CardSkillQuery] {cardCfg.DisplayName} 最终解析到 {destination.Count} 个技能");
    }

    private static string ResolveShipName(string cardId)
    {
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && !string.IsNullOrWhiteSpace(cardId) && map.TryGetValue(cardId.Trim(), out var cfg) && cfg != null)
        {
            return cfg.DisplayName;
        }
        return cardId ?? string.Empty;
    }

    /// <summary>
    /// 按阵营枚举技能（数据来源当前加载的 <see cref="SkillConfigDatabaseSO"/>）。
    /// </summary>
    /// <param name="faction">阵营。</param>
    /// <param name="destination">输出列表（会先 Clear）。</param>
    /// <param name="restrictToDatabaseScope">是否启用库的 <see cref="SkillConfigDatabaseSO.DatabaseFaction"/> 范围限制。</param>
    public static void ResolveSkillsByFaction(
        ShipFaction faction,
        List<SkillConfigSO> destination,
        bool restrictToDatabaseScope = false)
    {
        destination?.Clear();
        if (destination == null)
        {
            return;
        }

        var skillDb = GameResourceLoader.LoadSkillConfigDatabase(faction, logOnMissing: false);
        if (skillDb == null)
        {
            return;
        }

        skillDb.CopySkillsMatchingFaction(faction, destination, restrictToDatabaseScope);
    }

    #endregion
}
