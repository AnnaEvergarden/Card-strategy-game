using Cysharp.Threading.Tasks;

/// <summary>
/// 玩家资料应用服务实现：校验（领域对象负责）→ 持久化（仓储）→ 事务边界（工作单元）。
/// </summary>
public class PlayerProfileService : IPlayerProfileService
{
    private readonly IPlayerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PlayerProfileService(IPlayerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>获取玩家资料。</summary>
    public UniTask<PlayerProfile> GetProfileAsync() => _repository.LoadAsync();

    /// <summary>修改玩家名称。</summary>
    public async UniTask<ProfileUpdateResult> SetNameAsync(string name)
    {
        var profile = await _repository.LoadAsync();
        if (!profile.TrySetName(name, out var error))
            return ProfileUpdateResult.Fail(error);

        await PersistAsync();
        return ProfileUpdateResult.Ok();
    }

    /// <summary>修改生日。</summary>
    public async UniTask<ProfileUpdateResult> SetBirthdayAsync(int year, int month, int day)
    {
        var profile = await _repository.LoadAsync();
        if (!profile.TrySetBirthday(year, month, day, out var error))
            return ProfileUpdateResult.Fail(error);

        await PersistAsync();
        return ProfileUpdateResult.Ok();
    }

    /// <summary>保存资料：标记脏数据 → 事务内写入 → 提交/回滚。</summary>
    private async UniTask PersistAsync()
    {
        _repository.MarkDirty();

        if (_unitOfWork != null)
            await _unitOfWork.BeginAsync();

        try
        {
            await _repository.SaveAsync();
            if (_unitOfWork != null)
                await _unitOfWork.CommitAsync();
        }
        catch
        {
            if (_unitOfWork != null)
                await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
