using Cysharp.Threading.Tasks;

/// <summary>
/// 编队仓储接口。
/// </summary>
public interface IFleetRepository : IRepository
{
    UniTask<FleetSaveData> LoadAsync();
    void MarkDirty();
    UniTask SaveAsync();
    void InvalidateCache();
}