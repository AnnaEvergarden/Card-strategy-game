using Cysharp.Threading.Tasks;

/// <summary>
/// 道具仓储接口。定义在 Domain 层，实现由 Infrastructure 提供。
/// </summary>
public interface IInventoryRepository : IRepository
{
    /// <summary>加载（或从缓存返回）玩家道具列表。</summary>
    UniTask<Inventory> LoadAsync();

    /// <summary>标记缓存已修改（由 ApplicationService 在领域操作后调用）。</summary>
    void MarkDirty();

    /// <summary>持久化当前状态到存档。</summary>
    UniTask SaveAsync();
    /// <summary>清除缓存，下次 LoadAsync 将从存储重新加载（供 IUnitOfWork.RollbackAsync 使用）。</summary>
    void InvalidateCache();

}