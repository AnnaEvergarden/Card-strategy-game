using System;
using System.Collections.Generic;

/// <summary>
/// 建造领域服务：提供基于权重的随机抽取算法。
/// 纯 C# 逻辑，不依赖 Unity API，可独立测试。
/// </summary>
public static class BuildService
{
    /// <summary>
    /// 一次建造结算结果。
    /// </summary>
    public class BuildResult
    {
        public string PoolId;
        public List<string> CardIds = new List<string>();
        public int GoldCost;
        public int DiamondCost;
        public int ShipTicketCost;
    }

    /// <summary>
    /// 执行权重抽取。
    /// </summary>
    /// <param name="pool">卡池配置</param>
    /// <param name="count">抽取次数（1 或 10）</param>
    /// <param name="random">随机数生成器（可传入模拟种子用于测试）</param>
    public static BuildResult Draw(BuildPoolConfig pool, int count, Random random = null)
    {
        if (pool == null) throw new ArgumentNullException(nameof(pool));
        if (count != 1 && count != 10)
            throw new ArgumentException("抽取次数仅支持 1 或 10", nameof(count));

        random = random ?? new Random();

        var result = new BuildResult
        {
            PoolId = pool.PoolId,
            GoldCost = pool.BuildOneCost?.Gold ?? 0,
            DiamondCost = pool.BuildOneCost?.Diamond ?? 0,
            ShipTicketCost = pool.BuildOneCost?.ShipTicket ?? 0
        };

        // 十连使用十连消耗
        if (count == 10 && pool.BuildTenCost != null)
        {
            result.GoldCost = pool.BuildTenCost.Gold;
            result.DiamondCost = pool.BuildTenCost.Diamond;
            result.ShipTicketCost = pool.BuildTenCost.ShipTicket;
        }

        for (int i = 0; i < count; i++)
        {
            var cardId = DrawOne(pool, random);
            if (!string.IsNullOrWhiteSpace(cardId))
                result.CardIds.Add(cardId);
        }

        return result;
    }

    /// <summary>
    /// 单次权重抽取。
    /// </summary>
    private static string DrawOne(BuildPoolConfig pool, Random random)
    {
        var entries = pool.DropEntries;
        if (entries == null || entries.Count == 0) return null;

        // 计算总权重
        int totalWeight = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].Weight > 0)
                totalWeight += entries[i].Weight;
        }

        if (totalWeight <= 0) return null;

        int roll = random.Next(0, totalWeight);
        int accumulated = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.Weight <= 0) continue;

            accumulated += entry.Weight;
            if (roll < accumulated)
                return entry.CardId;
        }

        return null;
    }
}
