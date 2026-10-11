/// <summary>
/// 玩家资料修改结果：成功标志与失败原因。
/// （异步方法不能使用 out 参数，故用结果对象承载校验失败信息。）
/// </summary>
public sealed class ProfileUpdateResult
{
    /// <summary>是否成功。</summary>
    public bool Success { get; }

    /// <summary>失败原因（成功时为空）。</summary>
    public string Error { get; }

    private ProfileUpdateResult(bool success, string error)
    {
        Success = success;
        Error = error ?? string.Empty;
    }

    /// <summary>创建成功结果。</summary>
    public static ProfileUpdateResult Ok() => new ProfileUpdateResult(true, string.Empty);

    /// <summary>创建失败结果。</summary>
    public static ProfileUpdateResult Fail(string error) => new ProfileUpdateResult(false, error);
}
