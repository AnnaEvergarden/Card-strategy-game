using Cysharp.Threading.Tasks;

/// <summary>
/// 卡牌收藏仓储接口。定义在 Domain 层，实现由 Infrastructure 提供。
/// </summary>
public interface ICardCollectionRepository : IRepository
{
    /// <summary>加载（或从缓存返回）玩家卡牌收藏。</summary>
    UniTask<CardCollection> LoadAsync();

    /// <summary>标记缓存已修改。</summary>
    void MarkDirty();

    /// <summary>持久化当前状态到存档。</summary>
    UniTask SaveAsync();
    /// <summary>清除缓存，下次 LoadAsync 将从存储重新加载（供 IUnitOfWork.RollbackAsync 使用）。</summary>
    void InvalidateCache();

}