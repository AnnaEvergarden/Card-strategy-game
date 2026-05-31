using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用 Utility AI 服务：为指定阵营（P1 自动战斗 / P2 敌方 NPC）提供基于评分的自动决策与行动执行能力。
/// 评分流程（<see cref="EvaluateBestActions"/>）：
///   - 遍历该阵营场上存活且本回合未行动的单位。
///   - 对每个单位解析其所有技能，遍历（技能，目标）组合调用 <see cref="IScoreCalculator"/> 计算评分。
///   - 为每个单位选择评分最高的行动；若评分 ≥ <see cref="AIProfileSO.MinActionScore"/> 则加入结果列表。
/// </summary>
public static class BattleAIService
{
    #region Fields

    private static readonly List<string> _unitIdBuffer = new(4);
    private static readonly List<string> _targetBuffer = new(8);
    private static readonly List<string> _allyTempBuffer = new(4);
    private static readonly List<SkillConfigSO> _skillBuffer = new(CardConfigSO.MaxSkillsPerCard);
    private static AIProfileSO _cachedDefaultProfile;
    private static bool _defaultProfileResolved;

    // Calculator 实例缓存
    private static readonly DamageScoreCalculator _damageCalc = new();
    private static readonly HealScoreCalculator _healCalc = new();
    private static readonly SelfDrainScoreCalculator _drainCalc = new();
    private static readonly CooldownScoreCalculator _cooldownCalc = new();
    private static readonly DefenseBuffScoreCalculator _defenseCalc = new();

    #endregion

    #region Public API

    /// <summary>
    /// 评估未行动且存活的所有单位（P2 向后兼容），为每个单位选出最优行动。
    /// </summary>
    public static void EvaluateBestActions(List<AIAction> results)
    {
        EvaluateBestActions(results, BattleSide.P2);
    }

    /// <summary>
    /// 评估指定阵营中未行动且存活的所有单位，为每个单位选出 Utility 评分最高的行动。
    /// </summary>
    public static void EvaluateBestActions(List<AIAction> results, BattleSide side)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        var battle = BattleContext.Current;
        if (battle == null)
        {
            return;
        }

        var field = battle.Field;
        var turns = battle.Turns;
        var profile = ResolveProfile();

        _unitIdBuffer.Clear();
        turns.CopyFieldUnitIds(side, _unitIdBuffer);

        for (var e = 0; e < _unitIdBuffer.Count; e++)
        {
            var casterUnitId = _unitIdBuffer[e];
            if (turns.HasActedThisRound(casterUnitId) || !field.IsAlive(casterUnitId))
            {
                continue;
            }

            var casterUnit = field.GetUnit(casterUnitId);
            if (casterUnit == null)
            {
                continue;
            }

            var casterCardId = casterUnit.CardId;

            _skillBuffer.Clear();
            CardSkillQuery.ResolveSkillsForCard(casterCardId, BattleUtility.GetCardFaction(casterCardId), _skillBuffer);

            if (_skillBuffer.Count == 0)
            {
                continue;
            }

            AIAction bestAction = null;
            var bestScore = 0f;

            for (var s = 0; s < _skillBuffer.Count; s++)
            {
                var skill = _skillBuffer[s];
                if (skill == null || !casterUnit.CanUseSkill(skill))
                {
                    continue;
                }

                EnumerateTargetsFor(casterUnitId, skill.TargetKind, side, _targetBuffer);
                var isMultiTarget = IsMultiTargetKind(skill.TargetKind);

                if (_targetBuffer.Count == 0 && skill.TargetKind != SkillTargetKind.None)
                {
                    continue;
                }

                if (_targetBuffer.Count == 0)
                {
                    var score = ScoreSkillTarget(casterUnitId, skill, string.Empty, profile, false);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAction = new AIAction(casterUnitId, skill, string.Empty, score);
                    }
                    continue;
                }

                for (var t = 0; t < _targetBuffer.Count; t++)
                {
                    var targetUnitId = _targetBuffer[t];
                    var score = ScoreSkillTarget(casterUnitId, skill, targetUnitId, profile, isMultiTarget);

                    if (isMultiTarget)
                    {
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAction = new AIAction(casterUnitId, skill, targetUnitId, score);
                        }
                        break;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAction = new AIAction(casterUnitId, skill, targetUnitId, score);
                    }
                }
            }

            if (bestAction != null && bestAction.Score >= profile.MinActionScore)
            {
                results.Add(bestAction);
            }
        }
    }

    /// <summary>
    /// 执行单条 AI 行动（P2 向后兼容）。
    /// </summary>
    public static bool ExecuteSingleAction(AIAction action)
    {
        return ExecuteSingleAction(action, BattleSide.P2);
    }

    /// <summary>
    /// 执行单条 AI 行动，指定阵营。
    /// </summary>
    public static bool ExecuteSingleAction(AIAction action, BattleSide side)
    {
        if (action == null || string.IsNullOrWhiteSpace(action.CasterUnitId) || action.SkillConfig == null)
        {
            return false;
        }

        var battle = BattleContext.Current;
        if (battle == null)
        {
            return false;
        }

        var field = battle.Field;
        var turns = battle.Turns;

        var caster = field.GetUnit(action.CasterUnitId);
        if (caster == null || caster.IsDead)
        {
            return false;
        }

        if (!SkillSystem.TryCast(
                action.CasterUnitId,
                action.SkillConfig,
                action.TargetUnitId,
                out var outcome) || outcome == null || !outcome.Success)
        {
            return false;
        }

        field.TryRemoveDeadCards(BattleSide.P1);
        field.TryRemoveDeadCards(BattleSide.P2);

        if (!field.TryConsumeSkillUse(action.CasterUnitId, action.SkillConfig.SkillId))
        {
            Debug.LogWarning($"BattleAIService: {BattleUtility.GetShipDisplayName(caster.CardId)} 技能次数不足");
        }

        if (!outcome.CooldownRefreshed && action.SkillConfig.CooldownTurns > 0)
        {
            field.SetSkillCooldown(
                action.CasterUnitId,
                action.SkillConfig.SkillId,
                action.SkillConfig.CooldownTurns);
        }

        turns.TryCompleteCardAction(action.CasterUnitId);
        return true;
    }

    /// <summary>
    /// 阶段收尾（P2 向后兼容）：标记该阵营仍未行动的单位为已行动。
    /// </summary>
    public static void PassRemainingEnemies()
    {
        PassRemaining(BattleSide.P2);
    }

    /// <summary>
    /// 指定阵营阶段收尾：标记仍未行动的单位为已行动。
    /// </summary>
    public static void PassRemaining(BattleSide side)
    {
        var battle = BattleContext.Current;
        if (battle == null)
        {
            return;
        }

        var turns = battle.Turns;
        _unitIdBuffer.Clear();
        turns.CopyFieldUnitIds(side, _unitIdBuffer);

        for (var i = 0; i < _unitIdBuffer.Count; i++)
        {
            var unitId = _unitIdBuffer[i];
            if (!turns.HasActedThisRound(unitId))
            {
                turns.TryCompleteCardAction(unitId);
            }
        }
    }

    #endregion

    #region Scoring

    private static float ScoreSkillTarget(
        string casterUnitId, SkillConfigSO skill, string targetUnitId,
        AIProfileSO profile, bool isMultiTarget)
    {
        var totalScore = 0f;
        var effectList = skill.EffectList;
        if (effectList == null)
        {
            return 0f;
        }

        for (var i = 0; i < effectList.Count; i++)
        {
            var item = effectList[i];
            if (item == null || !item.IsValid)
            {
                continue;
            }

            totalScore += ScoreEffectItem(item, casterUnitId, targetUnitId, profile);
        }

        if (isMultiTarget)
        {
            totalScore *= profile.AoeMultiplier;
        }

        return totalScore;
    }

    private static float ScoreEffectItem(
        SkillEffectListItem item, string casterUnitId, string targetUnitId,
        AIProfileSO profile)
    {
        if (item.Category == SkillEffectCategory.Instant)
        {
            return ScoreInstantEffect(item.Instant, casterUnitId, targetUnitId, profile);
        }

        if (item.Category == SkillEffectCategory.Buff)
        {
            return ScoreBuffEffect(item.Buff, casterUnitId, targetUnitId, profile);
        }

        return 0f;
    }

    private static float ScoreInstantEffect(
        InstantEffectEntry entry, string casterUnitId, string targetUnitId,
        AIProfileSO profile)
    {
        if (entry == null)
        {
            return 0f;
        }

        return entry.InstantKind switch
        {
            SkillInstantKind.Damage => _damageCalc.Calculate(entry.Value, casterUnitId, targetUnitId, profile),
            SkillInstantKind.Heal => _healCalc.Calculate(entry.Value, casterUnitId, targetUnitId, profile),
            SkillInstantKind.SelfHpDrain => _drainCalc.Calculate(entry.Value, casterUnitId, targetUnitId, profile),
            SkillInstantKind.RefreshCooldown => _cooldownCalc.Calculate(entry.ChancePercent, casterUnitId, targetUnitId, profile),
            _ => 0f,
        };
    }

    private static float ScoreBuffEffect(
        BuffEffectEntry entry, string casterUnitId, string targetUnitId,
        AIProfileSO profile)
    {
        if (entry == null || entry.BuffKind != SkillBuffKind.DefenseBuff)
        {
            return 0f;
        }

        return _defenseCalc.Calculate(entry.Value, casterUnitId, targetUnitId, profile);
    }

    #endregion

    #region Target Enumeration

    private static void EnumerateTargetsFor(string casterUnitId, SkillTargetKind targetKind, BattleSide side, List<string> dest)
    {
        dest.Clear();
        var field = BattleContext.Current.Field;
        if (field == null)
        {
            return;
        }

        var enemySide = side == BattleSide.P1 ? BattleSide.P2 : BattleSide.P1;

        switch (targetKind)
        {
            case SkillTargetKind.None:
                break;

            case SkillTargetKind.SingleEnemy:
            case SkillTargetKind.AllEnemies:
                CollectSideUnitIds(field, enemySide, dest);
                break;

            case SkillTargetKind.SingleAlly:
            case SkillTargetKind.AllAllies:
                CollectSideAllies(casterUnitId, side, dest);
                break;

            case SkillTargetKind.All:
                for (var i = 0; i < BattleFieldState.SlotCount; i++)
                {
                    var p1 = field.P1Slots[i];
                    if (p1 != null && !p1.IsDead) dest.Add(p1.UnitId);
                    var p2 = field.P2Slots[i];
                    if (p2 != null && !p2.IsDead) dest.Add(p2.UnitId);
                }
                break;
        }
    }

    private static void CollectSideUnitIds(BattleFieldState field, BattleSide side, List<string> dest)
    {
        var slots = side == BattleSide.P1 ? field.P1Slots : field.P2Slots;
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i]!.IsDead)
            {
                dest.Add(slots[i]!.UnitId);
            }
        }
    }

    private static void CollectSideAllies(string casterUnitId, BattleSide side, List<string> dest)
    {
        _allyTempBuffer.Clear();
        var field = BattleContext.Current.Field;
        if (field == null)
        {
            return;
        }

        var slots = side == BattleSide.P1 ? field.P1Slots : field.P2Slots;
        dest.Clear();
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i]!.IsDead && slots[i]!.UnitId != casterUnitId)
            {
                dest.Add(slots[i]!.UnitId);
            }
        }
    }

    private static bool IsMultiTargetKind(SkillTargetKind kind)
    {
        return kind == SkillTargetKind.All
            || kind == SkillTargetKind.AllEnemies
            || kind == SkillTargetKind.AllAllies;
    }

    #endregion

    #region Profile Resolution

    private static AIProfileSO ResolveProfile()
    {
        var stage = BattleStartContext.CurrentStage;
        if (stage != null)
        {
            var stageProfile = stage.EnemyAIProfile;
            if (stageProfile != null)
            {
                return stageProfile;
            }
        }

        if (!_defaultProfileResolved)
        {
            _cachedDefaultProfile = Resources.Load<AIProfileSO>("DefaultAIProfile");
            _defaultProfileResolved = true;
        }

        if (_cachedDefaultProfile != null)
        {
            return _cachedDefaultProfile;
        }

        return ScriptableObject.CreateInstance<AIProfileSO>();
    }

    #endregion
}
