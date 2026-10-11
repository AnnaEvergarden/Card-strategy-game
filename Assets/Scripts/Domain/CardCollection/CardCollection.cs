using System;
using System.Collections.Generic;

/// <summary>
/// 卡牌收藏聚合根。封装 CardInstance 列表，对外只暴露只读视图。
/// 所有增删操作必须经过 Add/Remove 方法，不允许外部直接修改列表。
/// </summary>
public sealed class CardCollection
{
    private readonly List<CardInstance> _cards = new List<CardInstance>();

    /// <summary>对外只读视图。</summary>
    public IReadOnlyList<CardInstance> Cards => _cards;

    public int Count => _cards.Count;

    /// <summary>添加一张卡牌到收藏。</summary>
    public void Add(string instanceId, string cardId, int level = 1, int experience = 0)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("InstanceId cannot be null or empty", nameof(instanceId));
        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("CardId cannot be null or empty", nameof(cardId));
        if (level < 1)
            throw new ArgumentException("Level must be at least 1", nameof(level));
        if (experience < 0)
            throw new ArgumentException("Experience cannot be negative", nameof(experience));

        if (FindByInstanceId(instanceId) != null)
            throw new InvalidOperationException($"Card instance {instanceId} already exists");

        _cards.Add(new CardInstance
        {
            InstanceId = instanceId,
            CardId = cardId,
            Level = level,
            Experience = experience
        });
    }

    /// <summary>按实例 ID 移除一张卡牌。</summary>
    public bool Remove(string instanceId)
    {
        var card = FindByInstanceId(instanceId);
        if (card == null) return false;
        return _cards.Remove(card);
    }

    /// <summary>按实例 ID 查找卡牌。</summary>
    public CardInstance FindByInstanceId(string instanceId)
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].InstanceId == instanceId)
                return _cards[i];
        }
        return null;
    }

    /// <summary>检查是否拥有至少一张指定 CardId 的卡牌。</summary>
    public bool HasCardId(string cardId)
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].CardId == cardId)
                return true;
        }
        return false;
    }

    /// <summary>获取指定 CardId 的卡牌数量。</summary>
    public int GetCountByCardId(string cardId)
    {
        int count = 0;
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].CardId == cardId)
                count++;
        }
        return count;
    }

    /// <summary>清空所有卡牌。</summary>
    public void Clear() => _cards.Clear();
}