using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 卡牌收藏应用服务接口。UI 层通过此接口执行所有卡牌收藏操作。
/// </summary>
public interface ICardCollectionService
{
    /// <summary>获取玩家所有已收集卡牌（只读列表）。</summary>
    UniTask<IReadOnlyList<CardInstance>> GetCardsAsync();

    /// <summary>添加一张新卡牌到收藏。</summary>
    UniTask AddCardAsync(string instanceId, string cardId);

    /// <summary>按实例 ID 移除一张卡牌。</summary>
    UniTask<bool> RemoveCardAsync(string instanceId);

    /// <summary>获取卡牌收藏总数。</summary>
    UniTask<int> GetCountAsync();

    /// <summary>获取指定 CardId 的持有数量。</summary>
    UniTask<int> GetCountByCardIdAsync(string cardId);
}