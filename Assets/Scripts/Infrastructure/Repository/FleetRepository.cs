using Cysharp.Threading.Tasks;

/// <summary>
/// 编队仓储实现。
/// </summary>
public class FleetRepository : IFleetRepository
{
    private const string SaveKey = "fleet";

    private readonly ISaveService _saveService;
    private FleetSaveData _cache;
    public FleetRepository(ISaveService saveService)
    {
        _saveService = saveService;
    }

    public async UniTask<FleetSaveData> LoadAsync()
    {
        if (_cache != null)
            return _cache;

        _cache = await _saveService.LoadAsync<FleetSaveData>(SaveKey) ?? new FleetSaveData();
        return _cache;
    }

    public void MarkDirty() { }

    public async UniTask SaveAsync()
    {
        if (_cache == null) return;
        await _saveService.SaveAsync(SaveKey, _cache);
    }

    public void InvalidateCache() => _cache = null;
}