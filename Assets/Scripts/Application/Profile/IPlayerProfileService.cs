using Cysharp.Threading.Tasks;

/// <summary>
/// 玩家资料应用服务接口。UI 只能通过此接口读写玩家资料，
/// 不允许直接访问 Repository 或 SaveService（分层约束）。
/// </summary>
public interface IPlayerProfileService
{
    /// <summary>获取玩家资料（含等级/生日/名称）。</summary>
    UniTask<PlayerProfile> GetProfileAsync();

    /// <summary>修改玩家名称（校验由领域对象完成）。</summary>
    UniTask<ProfileUpdateResult> SetNameAsync(string name);

    /// <summary>修改生日（年份/月份/日期分别校验，含每月天数与闰年判断）。</summary>
    UniTask<ProfileUpdateResult> SetBirthdayAsync(int year, int month, int day);
}
