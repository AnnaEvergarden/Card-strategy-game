using Cysharp.Threading.Tasks;

/// <summary>
/// 卡牌收藏仓储实现。SaveKey 私有，外部不可见。
/// </summary>
public class CardCollectionRepository : ICardCollectionRepository
{
    private const string SaveKey = "card_collection";

    private readonly ISaveService _saveService;
    private CardCollection _cache;
    private bool _isDirty;

    public CardCollectionRepository(ISaveService saveService)
    {
        _saveService = saveService;
    }

    public async UniTask<CardCollection> LoadAsync()
    {
        if (_cache != null)
            return _cache;

        var saveData = await _saveService.LoadAsync<CardCollectionSaveData>(SaveKey);
        _cache = CardCollectionMapper.ToDomain(saveData);
        _isDirty = false;
        return _cache;
    }

    public void MarkDirty() => _isDirty = true;

    public async UniTask SaveAsync()
    {
        if (_cache == null) return;
        var saveData = CardCollectionMapper.ToSaveData(_cache);
        await _saveService.SaveAsync(SaveKey, saveData);
        _isDirty = false;
    }

    internal bool IsDirty => _isDirty;
    /// <summary>清除缓存，下次 LoadAsync 将从存储重新加载（供 IUnitOfWork.RollbackAsync 使用）。</summary>
    public void InvalidateCache() => _cache = null;

}