using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 工作单元实现。
/// 当前 JSON 阶段无法做真正的 ACID 事务，但 Commit 会保证所有注册的 Repository 都被按顺序持久化，Rollback 清除缓存。
/// 迁移到 SQLite 阶段时，只需替换此实现，内部使用 dbTransaction。
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly List<Func<UniTask>> _saveActions = new List<Func<UniTask>>();
    private readonly List<Action> _rollbackActions = new List<Action>();
    private bool _inProgress;

    public bool IsInProgress => _inProgress;

    public UniTask BeginAsync()
    {
        if (_inProgress) return UniTask.CompletedTask;
        _saveActions.Clear();
        _rollbackActions.Clear();
        _inProgress = true;
        return UniTask.CompletedTask;
    }

    public void Register(Func<UniTask> saveAction, Action rollbackAction)
    {
        if (_inProgress)
        {
            _saveActions.Add(saveAction);
            _rollbackActions.Add(rollbackAction);
        }
    }

    public async UniTask CommitAsync()
    {
        if (!_inProgress) return;
        var exceptions = new List<Exception>();
        for (int i = 0; i < _saveActions.Count; i++)
        {
            try { await _saveActions[i](); }
            catch (Exception ex) { exceptions.Add(ex); }
        }
        _saveActions.Clear();
        _rollbackActions.Clear();
        _inProgress = false;
        if (exceptions.Count > 0)
            throw new AggregateException("UnitOfWork commit failed", exceptions);
    }

    public async UniTask RollbackAsync()
    {
        if (!_inProgress) return;
        for (int i = 0; i < _rollbackActions.Count; i++)
        {
            try { _rollbackActions[i](); }
            catch { }
        }
        _saveActions.Clear();
        _rollbackActions.Clear();
        _inProgress = false;
        await UniTask.CompletedTask;
    }
}