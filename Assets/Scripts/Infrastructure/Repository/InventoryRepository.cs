using Cysharp.Threading.Tasks;

/// <summary>
/// 道具仓储实现。
/// - SaveKey 私有，外部不可见
/// - 维护内存缓存，避免反复加载
/// - MarkDirty 供 ApplicationService 在领域操作后调用
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    private const string SaveKey = "inventory";

    private readonly ISaveService _saveService;
    private Inventory _cache;
    private bool _isDirty;

    public InventoryRepository(ISaveService saveService)
    {
        _saveService = saveService;
    }

    public async UniTask<Inventory> LoadAsync()
    {
        if (_cache != null)
            return _cache;

        var saveData = await _saveService.LoadAsync<InventorySaveData>(SaveKey);
        _cache = InventoryMapper.ToDomain(saveData);
        _isDirty = false;
        return _cache;
    }

    public void MarkDirty()
    {
        _isDirty = true;
    }

    public async UniTask SaveAsync()
    {
        if (_cache == null)
            return;

        var saveData = InventoryMapper.ToSaveData(_cache);
        await _saveService.SaveAsync(SaveKey, saveData);
        _isDirty = false;
    }

    /// <summary>供 UnitOfWork 或退出自动保存时检查。</summary>
    internal bool IsDirty => _isDirty;
    /// <summary>清除缓存，下次 LoadAsync 将从存储重新加载（供 IUnitOfWork.RollbackAsync 使用）。</summary>
    public void InvalidateCache() => _cache = null;

}