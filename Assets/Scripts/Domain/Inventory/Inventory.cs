using System;
using System.Collections.Generic;

/// <summary>
/// 道具列表聚合根。封装所有道具操作逻辑，对外只暴露只读列表。
/// 不变量：数量不能为负数；每个堆叠不超过 MaxStack。
/// 不含 IsDirty——Dirty 跟踪由 Repository 负责。
/// </summary>
public sealed class Inventory
{
    private readonly List<InventoryItem> _items = new List<InventoryItem>();

    /// <summary>对外只读视图。</summary>
    public IReadOnlyList<InventoryItem> Items => _items;

    /// <summary>
    /// 添加道具。自动处理堆叠合并和超出上限拆分。
    /// </summary>
    /// <param name="itemId">道具 ID</param>
    /// <param name="quantity">要增加的数量（必须 &gt; 0）</param>
    /// <param name="maxStack">单堆叠上限，默认 99</param>
    public void AddItem(string itemId, int quantity, int maxStack = 99)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("ItemId cannot be null or empty", nameof(itemId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (maxStack <= 0)
            throw new ArgumentException("MaxStack must be positive", nameof(maxStack));

        int remaining = quantity;

        // 尝试合并到已有堆叠
        for (int i = 0; i < _items.Count && remaining > 0; i++)
        {
            var item = _items[i];
            if (item.ItemId != itemId) continue;
            int space = maxStack - item.Quantity;
            if (space <= 0) continue;
            int add = Math.Min(remaining, space);
            _items[i] = new InventoryItem(itemId, item.Quantity + add);
            remaining -= add;
        }

        // 剩余部分创建新堆叠
        while (remaining > 0)
        {
            int batch = Math.Min(remaining, maxStack);
            _items.Add(new InventoryItem(itemId, batch));
            remaining -= batch;
        }
    }

    /// <summary>
    /// 移除道具。从已有堆叠中扣除，不够则抛异常。
    /// </summary>
    /// <returns>是否全部扣除成功</returns>
    public bool RemoveItem(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("ItemId cannot be null or empty", nameof(itemId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        int total = GetQuantity(itemId);
        if (total < quantity) return false;

        int remaining = quantity;
        for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = _items[i];
            if (item.ItemId != itemId) continue;

            if (item.Quantity <= remaining)
            {
                remaining -= item.Quantity;
                _items.RemoveAt(i);
            }
            else
            {
                _items[i] = new InventoryItem(itemId, item.Quantity - remaining);
                remaining = 0;
            }
        }

        return true;
    }

    /// <summary>检查是否拥有至少指定数量的道具。</summary>
    public bool HasItem(string itemId, int quantity = 1)
    {
        return GetQuantity(itemId) >= quantity;
    }

    /// <summary>获取指定道具的总数量。</summary>
    public int GetQuantity(string itemId)
    {
        int total = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].ItemId == itemId)
                total += _items[i].Quantity;
        }
        return total;
    }

    /// <summary>查找指定道具的条目列表（可能有多个堆叠）。</summary>
    public InventoryItem GetItem(string itemId)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].ItemId == itemId)
                return _items[i];
        }
        return null;
    }

    /// <summary>清空所有道具。</summary>
    public void Clear()
    {
        _items.Clear();
    }
}