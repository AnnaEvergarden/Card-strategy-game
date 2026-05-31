using UnityEngine;

/// <summary>
/// 战斗门面：UI 唯一战斗入口，隔离 SkillPipeline / Effect / Buff 等内部实现。
/// 所有公开 API 使用 UnitId 标识单位，禁止 CardId 运行时传递。
/// </summary>
public static class BattleFacade
{
    #region Public API

    /// <summary>
    /// 玩家点击技能槽：进入释放流程（需选目标则登记待选并关闭技能面板）。
    /// </summary>
    public static void TryCastSkill(string casterUnitId, int skillIndex, string skillId)
    {
        var battle = BattleContext.Current;
        var field = battle.Field;
        var turns = battle.Turns;

        skillId = field.NormalizeCardId(skillId);
        if (string.IsNullOrWhiteSpace(casterUnitId) || string.IsNullOrEmpty(skillId))
        {
            return;
        }

        casterUnitId = casterUnitId.Trim();
        var caster = field.GetUnit(casterUnitId);
        if (caster == null || caster.IsDead)
        {
            Debug.LogWarning("BattleFacade: 施法者状态无效。");
            return;
        }

        if (caster.Side == BattleSide.P1 && !turns.IsPlayerActionPhase)
        {
            Debug.LogWarning("BattleFacade: 当前为敌方行动阶段。");
            return;
        }

        if (caster.Side == BattleSide.P1 && turns.HasActedThisRound(casterUnitId))
        {
            Debug.LogWarning("BattleFacade: 该舰娘本回合已行动。");
            return;
        }

        SkillDefinitionRegistry.EnsureDefaultDefinitionsRegistered();
        if (!TryResolveSkillConfig(caster.CardId, skillId, out var skillCfg))
        {
            Debug.LogWarning($"BattleFacade: 未找到技能配置 skillId={skillId}");
            return;
        }

        if (!caster.CanUseSkill(skillCfg))
        {
            return;
        }

        if (SkillSystem.RequiresManualTarget(skillId))
        {
            BattleUiSession.BeginPendingSkillCast(casterUnitId, skillId, skillCfg);
            UIPanelRegistry.TryPop();

            var main = BattleMainPanel.EnsureInstance();
            if (main != null)
            {
                main.RefreshTurnPresentation();
            }

            Debug.Log("BattleFacade: 请点击战场上的目标舰娘。");
            return;
        }

        var request = new SkillCastRequest
        {
            CasterUnitId = casterUnitId,
            SkillId = skillId,
            SkillConfig = skillCfg
        };
        TryFinalizeCast(request);
    }

    /// <summary>
    /// 点选战场目标后完成待释放技能。
    /// </summary>
    public static void TryCompleteSkillOnTarget(string targetUnitId)
    {
        if (!BattleUiSession.TryGetPendingSkillCast(out var casterUnitId, out var skillId, out var skillCfg))
        {
            return;
        }

        var field = BattleContext.Current.Field;
        targetUnitId = field.NormalizeCardId(targetUnitId);

        if (!IsTargetValidForSkill(casterUnitId, targetUnitId, skillId))
        {
            Debug.LogWarning("BattleFacade: 目标不合法，请重新选择技能和目标。");
            BattleUiSession.ClearPendingSkillCast();
            return;
        }

        var request = new SkillCastRequest
        {
            CasterUnitId = casterUnitId,
            TargetUnitId = targetUnitId,
            SkillId = field.NormalizeCardId(skillId),
            SkillConfig = skillCfg
        };

        if (TryFinalizeCast(request))
        {
            BattleUiSession.ClearPendingSkillCast();
        }
    }

    /// <summary>
    /// 取消待选目标状态。
    /// </summary>
    public static void CancelPendingSkillCast()
    {
        BattleUiSession.ClearPendingSkillCast();
    }

    #endregion

    #region Private Methods

    private static bool TryFinalizeCast(SkillCastRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CasterUnitId) ||
            string.IsNullOrWhiteSpace(request.SkillId))
        {
            return false;
        }

        var battle = BattleContext.Current;
        var field = battle.Field;
        var turns = battle.Turns;

        request.SkillId = field.NormalizeCardId(request.SkillId);
        request.TargetUnitId = field.NormalizeCardId(request.TargetUnitId);

        var caster = field.GetUnit(request.CasterUnitId);
        if (caster == null || caster.IsDead)
        {
            return false;
        }
        var isP1Caster = caster.Side == BattleSide.P1;

        if (!caster.CanUseSkill(request.SkillConfig))
        {
            return false;
        }

        if (!SkillSystem.TryCast(
                request.CasterUnitId,
                request.SkillConfig,
                request.TargetUnitId,
                out var pipelineOutcome) || !pipelineOutcome.Success)
        {
            Debug.LogWarning($"BattleFacade: {pipelineOutcome?.Message ?? "技能执行失败"}");
            return false;
        }

        field.TryRemoveDeadCards(BattleSide.P1);
        field.TryRemoveDeadCards(BattleSide.P2);

        if (TryOpenBattleSettlement())
        {
            return true;
        }

        if (!field.TryConsumeSkillUse(caster.UnitId, request.SkillId))
        {
            Debug.LogWarning("BattleFacade: 技能次数不足。");
        }

        if (!pipelineOutcome.CooldownRefreshed && request.SkillConfig != null &&
            request.SkillConfig.CooldownTurns > 0)
        {
            field.SetSkillCooldown(caster.UnitId, request.SkillId, request.SkillConfig.CooldownTurns);
        }

        if (isP1Caster)
        {
            turns.TryCompleteCardAction(caster.UnitId);
        }

        var main = BattleMainPanel.EnsureInstance();
        if (main != null)
        {
            main.RefreshBattlefield();
            main.RefreshTurnPresentation();
        }

        UIPanelRegistry.PopWhileTopIsAny(BattleUiFlow.BattleOverlayPanelNames);

        Debug.Log($"BattleFacade: {pipelineOutcome.Message}");
        return true;
    }

    /// <summary>
    /// 校验手动选目标是否合法：通过 UnitId 获取 <see cref="BattleUnit"/> 直接判断阵营。
    /// </summary>
    private static bool IsTargetValidForSkill(string casterUnitId, string targetUnitId, string skillId)
    {
        if (string.IsNullOrWhiteSpace(casterUnitId) || string.IsNullOrWhiteSpace(targetUnitId))
        {
            return false;
        }

        if (!BattleUiSession.TryGetPendingSkillCast(out _, out _, out var cfg) || cfg == null)
        {
            return false;
        }

        var kind = cfg.TargetKind;
        if (kind == SkillTargetKind.None || kind == SkillTargetKind.All ||
            kind == SkillTargetKind.AllAllies || kind == SkillTargetKind.AllEnemies)
        {
            return true;
        }

        var field = BattleContext.Current.Field;
        var target = field.GetUnit(targetUnitId);
        if (target == null || target.IsDead)
        {
            return false;
        }

        var caster = field.GetUnit(casterUnitId);
        if (caster == null)
        {
            return false;
        }

        return kind switch
        {
            SkillTargetKind.SingleEnemy => target.Side != caster.Side,
            SkillTargetKind.SingleAlly => target.Side == caster.Side,
            _ => true
        };
    }

    private static bool TryResolveSkillConfig(string cardId, string skillId, out SkillConfigSO skill)
    {
        skill = null;
        var field = BattleContext.Current.Field;
        cardId = field.NormalizeCardId(cardId);
        skillId = field.NormalizeCardId(skillId);
        if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        var faction = map != null && map.TryGetValue(cardId, out var cardCfg) && cardCfg != null
            ? cardCfg.Faction : ShipFaction.Other;
        var buffer = new System.Collections.Generic.List<SkillConfigSO>(CardConfigSO.MaxSkillsPerCard);
        CardSkillQuery.ResolveSkillsForCard(cardId, faction, buffer);
        for (var i = 0; i < buffer.Count; i++)
        {
            var s = buffer[i];
            if (s != null && field.NormalizeCardId(s.SkillId) == skillId)
            {
                skill = s;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查战斗结束条件：P2 全灭 → 胜利，P1 全灭 → 失败。
    /// </summary>
    public static bool TryOpenBattleSettlement()
    {
        var field = BattleContext.Current?.Field;
        if (field == null)
        {
            return false;
        }

        // 检查 P2 存活
        var p2Alive = false;
        for (var i = 0; i < BattleFieldState.SlotCount; i++)
        {
            if (field.P2Slots[i] != null && !field.P2Slots[i]!.IsDead)
            {
                p2Alive = true;
                break;
            }
        }
        if (!p2Alive)
        {
            Debug.Log("BattleFacade: 敌方全灭，战斗胜利。");
            BattleUiFlow.OpenSettlement(BattleSettlementKind.Victory);
            return true;
        }

        // 检查 P1 存活
        var p1Alive = false;
        for (var i = 0; i < BattleFieldState.SlotCount; i++)
        {
            if (field.P1Slots[i] != null && !field.P1Slots[i]!.IsDead)
            {
                p1Alive = true;
                break;
            }
        }
        if (!p1Alive)
        {
            Debug.Log("BattleFacade: 我方全灭，战斗失败。");
            BattleUiFlow.OpenSettlement(BattleSettlementKind.Defeat);
            return true;
        }

        return false;
    }

    #endregion
}
