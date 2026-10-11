using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 道具应用服务接口。UI 层通过此接口执行所有道具操作，不允许直接访问 Repository 或 SaveService。
/// </summary>
public interface IInventoryService
{
    /// <summary>获取玩家所有道具（只读列表）。</summary>
    UniTask<IReadOnlyList<InventoryItem>> GetItemsAsync();

    /// <summary>增加道具。自动处理堆叠合并。</summary>
    UniTask<bool> AddItemAsync(string itemId, int quantity);

    /// <summary>移除道具。数量不足时返回 false。</summary>
    UniTask<bool> RemoveItemAsync(string itemId, int quantity);

    /// <summary>检查是否拥有至少指定数量的道具。</summary>
    UniTask<bool> HasItemAsync(string itemId, int quantity = 1);

    /// <summary>获取指定道具的总持有量。</summary>
    UniTask<int> GetQuantityAsync(string itemId);
}