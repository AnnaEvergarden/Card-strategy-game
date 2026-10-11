using Cysharp.Threading.Tasks;

/// <summary>
/// 账号仓储实现。SaveKey 私有，外部不可见。
/// </summary>
public class AccountRepository : IAccountRepository
{
    private const string SaveKey = "account";

    private readonly ISaveService _saveService;
    private Account _cache;
    private bool _isDirty;

    public AccountRepository(ISaveService saveService)
    {
        _saveService = saveService;
    }

    public async UniTask<Account> LoadAsync()
    {
        if (_cache != null)
            return _cache;

        var saveData = await _saveService.LoadAsync<AccountSaveData>(SaveKey);
        _cache = AccountMapper.ToDomain(saveData);
        _isDirty = false;
        return _cache;
    }

    public void MarkDirty() => _isDirty = true;

    public async UniTask SaveAsync()
    {
        if (_cache == null) return;
        var saveData = AccountMapper.ToSaveData(_cache);
        await _saveService.SaveAsync(SaveKey, saveData);
        _isDirty = false;
    }

    internal bool IsDirty => _isDirty;
    /// <summary>清除缓存，下次 LoadAsync 将从存储重新加载（供 IUnitOfWork.RollbackAsync 使用）。</summary>
    public void InvalidateCache() => _cache = null;

}