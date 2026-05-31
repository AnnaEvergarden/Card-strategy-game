using UnityEngine;

/// <summary>
/// 技能/伤害事件表现桥接：订阅 <see cref="BattleEventBus"/>，仅刷新 UI，不修改战斗数据。
/// </summary>
public sealed class BattleSkillEventPresenter : MonoBehaviour
{
    #region Unity Lifecycle

    /// <summary>
    /// 订阅战斗事件。
    /// </summary>
    private void OnEnable()
    {
        var events = BattleContext.Current.Events;
        events.Subscribe<SkillCastFinishEvent>(OnSkillCastFinish);
        events.Subscribe<DamageEvent>(OnDamage);
        events.Subscribe<DeadEvent>(OnDead);
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    private void OnDisable()
    {
        var events = BattleContext.Current.Events;
        events.Unsubscribe<SkillCastFinishEvent>(OnSkillCastFinish);
        events.Unsubscribe<DamageEvent>(OnDamage);
        events.Unsubscribe<DeadEvent>(OnDead);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 技能释放结束后刷新战场（与门面层刷新互补，便于后续接动画/飘字）。
    /// </summary>
    private void OnSkillCastFinish(SkillCastFinishEvent evt)
    {
        if (evt == null || !evt.Success)
        {
            return;
        }

        var main = BattleMainPanel.EnsureInstance();
        if (main != null)
        {
            main.RefreshBattlefield();
        }
    }

    /// <summary>
    /// 受伤表现占位：后续可接飘字、受击动画（事件在技能流水线全部 Effect 成功后发布）。
    /// </summary>
    private void OnDamage(DamageEvent evt)
    {
        if (evt == null || evt.AppliedDamage <= 0)
        {
            return;
        }

        Debug.Log($"BattleSkillEventPresenter: {evt.TargetCardId} 受到 {evt.AppliedDamage} 点伤害，剩余 HP {evt.RemainingHp}");
    }

    /// <summary>
    /// 死亡表现占位。
    /// </summary>
    private void OnDead(DeadEvent evt)
    {
        if (evt == null)
        {
            return;
        }

        Debug.Log($"BattleSkillEventPresenter: {evt.CardId} 已无法战斗");
    }

    #endregion
}
