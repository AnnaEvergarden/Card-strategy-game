using Cysharp.Threading.Tasks;

/// <summary>
/// 工作单元接口：提供事务边界。
/// 所有 Repository 跨多个聚合根的写入操作应在同一事务内执行。
/// </summary>
public interface IUnitOfWork
{
    /// <summary>是否正在事务中。</summary>
    bool IsInProgress { get; }

    /// <summary>开始事务。</summary>
    UniTask BeginAsync();

    /// <summary>注册一个 Repository 的保存和回滚操作。</summary>
    void Register(System.Func<UniTask> saveAction, System.Action rollbackAction);

    /// <summary>提交事务：持久化所有修改。</summary>
    UniTask CommitAsync();

    /// <summary>回滚事务：丢弃所有未提交的修改。</summary>
    UniTask RollbackAsync();
}