using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建造服务：负责资源校验/扣减、按卡池权重抽卡并写入玩家卡牌仓库。
/// </summary>
public static class BuildService
{
    #region Fields

    /// <summary>
    /// 卡池缓存。
    /// </summary>
    private static BuildPoolDatabaseSO _db;

    #endregion

    #region Nested Models

    /// <summary>
    /// 一次建造结算结果。
    /// </summary>
    public sealed class BuildResult
    {
        /// <summary>
        /// 卡池 id。
        /// </summary>
        public string poolId;

        /// <summary>
        /// 本次抽到的 cardId 列表。
        /// </summary>
        public List<string> cardIds = new();

        /// <summary>
        /// 实际消耗金币。
        /// </summary>
        public int goldCost;

        /// <summary>
        /// 实际消耗钻石。
        /// </summary>
        public int diamondCost;

        /// <summary>
        /// 实际消耗船票。
        /// </summary>
        public int shipTicketCost;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 获取全部可选卡池。
    /// </summary>
    public static IReadOnlyList<BuildPoolConfigSO> GetPools()
    {
        EnsureDatabase();
        return _db != null ? _db.Pools : null;
    }

    /// <summary>
    /// 执行建造（1 次或 10 次）：先校验并扣资源，再抽卡并入库。
    /// </summary>
    /// <param name="poolId">卡池 id。</param>
    /// <param name="count">次数（仅支持 1 或 10）。</param>
    /// <param name="result">输出结果。</param>
    /// <param name="error">失败原因。</param>
    public static bool TryBuild(string poolId, int count, out BuildResult result, out string error)
    {
        result = null;
        error = string.Empty;
        if (count != 1 && count != 10)
        {
            error = "建造次数仅支持 1 或 10。";
            return false;
        }

        EnsureDatabase();
        var pool = FindPool(poolId);
        if (pool == null)
        {
            error = "未找到对应卡池。";
            return false;
        }

        var cost = count == 10 ? pool.BuildTenCost : pool.BuildOneCost;
        var gold = cost != null ? Mathf.Max(0, cost.gold) : 0;
        var diamond = cost != null ? Mathf.Max(0, cost.diamond) : 0;
        var ticket = cost != null ? Mathf.Max(0, cost.shipTicket) : 0;

        if (!CurrencyStore.TryConsume(gold, diamond, ticket))
        {
            error = "资源不足（金币/钻石/船票）。";
            return false;
        }

        var ids = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var id = DrawOneCardId(pool);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            id = id.Trim();
            ids.Add(id);
            CardCollectionStore.AddCards(id, 1);
        }

        if (ids.Count == 0)
        {
            // 没抽到有效结果时回滚资源，避免白消耗。
            CurrencyStore.Add(gold, diamond, ticket);
            error = "该卡池未配置有效掉落项。";
            return false;
        }

        result = new BuildResult
        {
            poolId = pool.PoolId,
            cardIds = ids,
            goldCost = gold,
            diamondCost = diamond,
            shipTicketCost = ticket
        };
        return true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 权重抽取单张 cardId。
    /// </summary>
    private static string DrawOneCardId(BuildPoolConfigSO pool)
    {
        var entries = pool.DropEntries;
        if (entries == null || entries.Count == 0)
        {
            return null;
        }

        var total = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.Card == null || e.weight <= 0)
            {
                continue;
            }

            if (pool.PoolFaction != ShipFaction.Other && e.Card.Faction != pool.PoolFaction)
            {
                continue;
            }

            var cid = e.Card.CardId;
            if (string.IsNullOrWhiteSpace(cid))
            {
                continue;
            }

            total += e.weight;
        }

        if (total <= 0)
        {
            return null;
        }

        var roll = Random.Range(0, total);
        var acc = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.Card == null || e.weight <= 0)
            {
                continue;
            }

            if (pool.PoolFaction != ShipFaction.Other && e.Card.Faction != pool.PoolFaction)
            {
                continue;
            }

            var cid = e.Card.CardId;
            if (string.IsNullOrWhiteSpace(cid))
            {
                continue;
            }

            acc += e.weight;
            if (roll < acc)
            {
                return cid.Trim();
            }
        }

        return null;
    }

    private static BuildPoolConfigSO FindPool(string poolId)
    {
        if (_db == null || string.IsNullOrWhiteSpace(poolId))
        {
            return null;
        }

        var list = _db.Pools;
        if (list == null)
        {
            return null;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var pool = list[i];
            if (pool != null && string.Equals(pool.PoolId, poolId, System.StringComparison.Ordinal))
            {
                return pool;
            }
        }

        return null;
    }

    private static void EnsureDatabase()
    {
        if (_db != null)
        {
            return;
        }

        _db = GameResourceLoader.LoadBuildPoolDatabase(logOnMissing: false);
        if (_db == null)
        {
            Debug.LogWarning($"BuildService: 未找到卡池数据库 Resources/{GameResourcePaths.BuildPoolDatabase}。");
        }
    }

    #endregion
}
