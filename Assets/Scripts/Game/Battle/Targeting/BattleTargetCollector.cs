using System.Collections.Generic;

/// <summary>
/// 战斗目标收集：从 <see cref="BattleFieldState"/> 槽位枚举单位供目标策略使用。
/// </summary>
public static class BattleTargetCollector
{
    #region Public API

    /// <summary>
    /// 收集场上所有存活单位（遍历 P1、P2 槽位）。
    /// </summary>
    public static void CollectAllAlive(SkillExecutionContext context)
    {
        if (context == null) return;

        var battle = context.Battle ?? BattleContext.Current;
        var field = battle.Field;
        context.ClearTargets();
        field.EnsureInitialized();

        for (var i = 0; i < BattleFieldState.SlotCount; i++)
        {
            var p1 = field.P1Slots[i];
            if (p1 != null && !p1.IsDead)
            {
                context.AddTarget(p1);
            }

            var p2 = field.P2Slots[i];
            if (p2 != null && !p2.IsDead)
            {
                context.AddTarget(p2);
            }
        }
    }

    /// <summary>
    /// 收集全体己方（根据施法者阵营自动判断）。
    /// </summary>
    public static void CollectAllAllies(SkillExecutionContext context)
    {
        if (context == null) return;

        var battle = context.Battle ?? BattleContext.Current;
        var field = battle.Field;
        context.ClearTargets();
        field.EnsureInitialized();

        var casterSide = context.Caster?.Side ?? BattleSide.P1;
        var slots = casterSide == BattleSide.P1 ? field.P1Slots : field.P2Slots;
        for (var i = 0; i < slots.Length; i++)
        {
            var unit = slots[i];
            if (unit != null && !unit.IsDead)
            {
                context.AddTarget(unit);
            }
        }
    }

    /// <summary>
    /// 收集全体敌方（根据施法者阵营自动判断）。
    /// </summary>
    public static void CollectAllEnemies(SkillExecutionContext context)
    {
        if (context == null) return;

        var battle = context.Battle ?? BattleContext.Current;
        var field = battle.Field;
        context.ClearTargets();
        field.EnsureInitialized();

        var casterSide = context.Caster?.Side ?? BattleSide.P1;
        var slots = casterSide == BattleSide.P1 ? field.P2Slots : field.P1Slots;
        for (var i = 0; i < slots.Length; i++)
        {
            var unit = slots[i];
            if (unit != null && !unit.IsDead)
            {
                context.AddTarget(unit);
            }
        }
    }

    #endregion
}
