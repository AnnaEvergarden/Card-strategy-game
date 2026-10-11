using Cysharp.Threading.Tasks;

/// <summary>
/// 玩家资料仓储实现。SaveKey 私有，外部不可见。
/// </summary>
public class PlayerProfileRepository : IPlayerProfileRepository
{
    private const string SaveKey = "player_profile";

    private readonly ISaveService _saveService;
    private PlayerProfile _cache;
    private bool _isDirty;

    public PlayerProfileRepository(ISaveService saveService)
    {
        _saveService = saveService;
    }

    /// <summary>
    /// 加载玩家资料：优先缓存，否则从存档读取（无存档时返回默认资料）。
    /// </summary>
    public async UniTask<PlayerProfile> LoadAsync()
    {
        if (_cache != null)
            return _cache;

        var saveData = await _saveService.LoadAsync<PlayerProfileSaveData>(SaveKey);
        _cache = PlayerProfileMapper.ToDomain(saveData);
        _isDirty = false;
        return _cache;
    }

    /// <summary>标记待保存。</summary>
    public void MarkDirty() => _isDirty = true;

    /// <summary>保存玩家资料。</summary>
    public async UniTask SaveAsync()
    {
        if (_cache == null) return;

        var saveData = PlayerProfileMapper.ToSaveData(_cache);
        await _saveService.SaveAsync(SaveKey, saveData);
        _isDirty = false;
    }

    /// <summary>是否待保存。</summary>
    internal bool IsDirty => _isDirty;

    /// <summary>清除缓存，下次 LoadAsync 从存储重新加载（供 IUnitOfWork.RollbackAsync 使用）。</summary>
    public void InvalidateCache() => _cache = null;
}
