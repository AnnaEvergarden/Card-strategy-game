using Cysharp.Threading.Tasks;

/// <summary>
/// 玩家资料仓储接口：定义在 Domain 层，实现由 Infrastructure 提供。
/// SaveKey 管理、DTO 转换与缓存由实现方负责。
/// </summary>
public interface IPlayerProfileRepository : IRepository
{
    /// <summary>加载玩家资料（带缓存；无存档时返回默认资料）。</summary>
    UniTask<PlayerProfile> LoadAsync();

    /// <summary>标记为待保存。</summary>
    void MarkDirty();

    /// <summary>保存玩家资料。</summary>
    UniTask SaveAsync();

    /// <summary>清除缓存，下次 LoadAsync 从存储重新加载。</summary>
    void InvalidateCache();
}
