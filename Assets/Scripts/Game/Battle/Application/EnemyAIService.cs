using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌方 Utility AI 服务：为敌方 NPC 提供基于评分的自动决策与行动执行能力。
///
/// <para>评分流程（<see cref="EvaluateBestActions"/>）：</para>
/// <list type="bullet">
///   <item>遍历场上存活且本回合未行动的敌方 NPC。</item>
///   <item>对每个 NPC，通过 <see cref="CardSkillQuery.ResolveSkillsForCard"/> 解析其所有技能。</item>
///   <item>过滤不可用技能（<see cref="BattleUnit.CanUseSkill"/>：冷却中、次数耗尽）。</item>
///   <item>按 <see cref="SkillTargetKind"/> 枚举有效目标，对每个（技能, 目标）组合调用 <see cref="IScoreCalculator"/> 计算 Utility 评分。</item>
///   <item>为每个 NPC 选择评分最高的行动；若评分 ≥ <see cref="AIProfileSO.MinActionScore"/> 则加入结果列表。</item>
/// </list>
///
/// <para>执行流程（<see cref="ExecuteSingleAction"/>）：</para>
/// <list type="bullet">
///   <item>调用 <see cref="SkillSystem.TryCast"/> 执行技能释放。</item>
///   <item>成功后在 <see cref="BattleFieldState"/> 中消耗技能次数、设置冷却回合。</item>
///   <item>通过 <see cref="BattleTurnSystem.TryCompleteCardAction"/> 标记该单位本回合已行动。</item>
/// </list>
///
/// <para>收尾（<see cref="PassRemainingEnemies"/>）：</para>
/// 敌方阶段结束时将仍未行动的单位标记为已行动，避免回合卡死。
///
/// 调用入口：<see cref="BattleMainPanel.P2PhaseRoutine"/> 协程。
/// </summary>
public static class EnemyAIService
{
    #region Fields

    private static readonly List<string> _unitIdBuffer = new(4);
    private static readonly List<string> _targetBuffer = new(8);
    private static readonly List<string> _allyTempBuffer = new(4);
    private static readonly List<SkillConfigSO> _skillBuffer = new(CardConfigSO.MaxSkillsPerCard);
    private static AIProfileSO _cachedDefaultProfile;
    private static bool _defaultProfileResolved;

    private static readonly DamageScoreCalculator _damageCalc = new();
    private static readonly HealScoreCalculator _healCalc = new();
    private static readonly SelfDrainScoreCalculator _drainCalc = new();
    private static readonly CooldownScoreCalculator _cooldownCalc = new();
    private static readonly DefenseBuffScoreCalculator _defenseCalc = new();

    #endregion

    #region Public API

    /// <summary>
    /// 评估所有未行动的存活敌方 NPC，为每个 NPC 选出 Utility 评分最高的行动。
    /// </summary>
    public static void EvaluateBestActions(List<AIAction> results)
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
        turns.CopyFieldUnitIds(BattleSide.P2, _unitIdBuffer);

        Debug.Log($"[EnemyAI] EvaluateBestActions: 敌方上场 {_unitIdBuffer.Count} 名, profile={profile.name}");

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
            CardSkillQuery.ResolveSkillsForCard(casterCardId, ResolveCardFaction(casterCardId), _skillBuffer);

            if (_skillBuffer.Count == 0)
            {
                Debug.LogWarning($"[EnemyAI] {ShipName(casterUnitId)} 没有可用技能配置，将跳过行动");
                continue;
            }

            Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 解析到 {_skillBuffer.Count} 个技能");

            AIAction bestAction = null;
            var bestScore = 0f;
            var usableSkillCount = 0;

            for (var s = 0; s < _skillBuffer.Count; s++)
            {
                var skill = _skillBuffer[s];
                if (skill == null)
                {
                    continue;
                }

                if (!casterUnit.CanUseSkill(skill))
                {
                    Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 技能 {skill.DisplayName} 不可用（冷却或次数不足）");
                    continue;
                }

                usableSkillCount++;

                EnumerateTargetsFor(casterUnitId, skill.TargetKind, _targetBuffer);
                var isMultiTarget = IsMultiTargetKind(skill.TargetKind);

                if (_targetBuffer.Count == 0 && skill.TargetKind != SkillTargetKind.None)
                {
                    Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 技能 {skill.DisplayName} 无合法目标");
                    continue;
                }

                if (_targetBuffer.Count == 0)
                {
                    var score = ScoreSkillTarget(casterUnitId, skill, string.Empty, profile, false);
                    Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 技能 {skill.DisplayName} 自评 score={score:F2}");
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
                        Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 群体技能 {skill.DisplayName} -> {ShipName(targetUnitId)} score={score:F2}");
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAction = new AIAction(casterUnitId, skill, targetUnitId, score);
                        }
                        break;
                    }

                    Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 技能 {skill.DisplayName} -> {ShipName(targetUnitId)} score={score:F2}");
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAction = new AIAction(casterUnitId, skill, targetUnitId, score);
                    }
                }
            }

            if (usableSkillCount == 0)
            {
                Debug.LogWarning($"[EnemyAI] {ShipName(casterUnitId)} 所有技能均不可用（冷却/次数），将跳过行动");
            }

            if (bestAction != null && bestAction.Score >= profile.MinActionScore)
            {
                results.Add(bestAction);
                Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 选择行动: {bestAction.SkillConfig.DisplayName} -> {ShipName(bestAction.TargetUnitId)} score={bestAction.Score:F2}");
            }
            else if (bestAction != null)
            {
                Debug.Log($"[EnemyAI] {ShipName(casterUnitId)} 最优评分 {bestAction.Score:F2} < 阈值 {profile.MinActionScore}，跳过");
            }
        }

        Debug.Log($"[EnemyAI] EvaluateBestActions 完成: 共 {results.Count} 条行动");
    }

    /// <summary>
    /// 执行单条 AI 行动：调用 <see cref="SkillSystem.TryCast"/> 释放技能，
    /// 成功后消耗技能次数、设置冷却、标记该单位本回合已行动。
    /// </summary>
    public static bool ExecuteSingleAction(AIAction action)
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
            Debug.LogWarning($"EnemyAIService: 敌方 {ShipName(action.CasterUnitId)} 技能 {action.SkillConfig.DisplayName} 执行失败");
            return false;
        }

        field.TryRemoveDeadCards(BattleSide.P1);
        field.TryRemoveDeadCards(BattleSide.P2);

        if (!field.TryConsumeSkillUse(action.CasterUnitId, action.SkillConfig.SkillId))
        {
            Debug.LogWarning($"EnemyAIService: 敌方 {ShipName(action.CasterUnitId)} 技能次数不足");
        }

        if (!outcome.CooldownRefreshed && action.SkillConfig.CooldownTurns > 0)
        {
            field.SetSkillCooldown(
                action.CasterUnitId,
                action.SkillConfig.SkillId,
                action.SkillConfig.CooldownTurns);
        }

        turns.TryCompleteCardAction(action.CasterUnitId);
        Debug.Log($"EnemyAIService: 敌方 {ShipName(action.CasterUnitId)} 释放 {action.SkillConfig.DisplayName} -> {ShipName(action.TargetUnitId)}: {outcome.Message}");
        return true;
    }

    /// <summary>
    /// 敌方阶段收尾：遍历所有上场敌方 NPC，将尚未标记为已行动的单位强制标记为已行动。
    /// </summary>
    public static void PassRemainingEnemies()
    {
        var battle = BattleContext.Current;
        if (battle == null)
        {
            return;
        }

        var turns = battle.Turns;
        _unitIdBuffer.Clear();
        turns.CopyFieldUnitIds(BattleSide.P2, _unitIdBuffer);

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

        Debug.LogWarning($"[EnemyAI] ScoreEffectItem: 未处理的技能效果类别 {item.Category}（caster={ShipName(casterUnitId)}），评分为 0");
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

        var score = _defenseCalc.Calculate(entry.Value, casterUnitId, targetUnitId, profile);
        Debug.Log($"[EnemyAI] ScoreBuffEffect: val={entry.Value} target={ShipName(targetUnitId)} score={score:F2}");
        return score;
    }

    #endregion

    #region Target Enumeration

    private static void EnumerateTargetsFor(string casterUnitId, SkillTargetKind targetKind, List<string> dest)
    {
        dest.Clear();
        var field = BattleContext.Current.Field;
        if (field == null)
        {
            return;
        }

        switch (targetKind)
        {
            case SkillTargetKind.None:
                break;

            case SkillTargetKind.SingleEnemy:
            case SkillTargetKind.AllEnemies:
                CollectP1Actives(field, dest);
                break;

            case SkillTargetKind.SingleAlly:
            case SkillTargetKind.AllAllies:
                CollectP2Allies(casterUnitId, dest);
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

    private static void CollectP1Actives(BattleFieldState field, List<string> dest)
    {
        for (var i = 0; i < field.P1Slots.Length; i++)
        {
            if (field.P1Slots[i] != null && !field.P1Slots[i].IsDead)
            {
                dest.Add(field.P1Slots[i].UnitId);
            }
        }
    }

    private static void CollectP2Allies(string casterUnitId, List<string> dest)
    {
        _allyTempBuffer.Clear();
        var field = BattleContext.Current.Field;
        if (field == null)
        {
            return;
        }

        dest.Clear();
        for (var i = 0; i < field.P2Slots.Length; i++)
        {
            if (field.P2Slots[i] != null && !field.P2Slots[i].IsDead && field.P2Slots[i].UnitId != casterUnitId)
            {
                dest.Add(field.P2Slots[i].UnitId);
            }
        }
    }

    #endregion

    #region Helpers

    private static string ShipName(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return unitId ?? string.Empty;
        }

        var field = BattleContext.Current?.Field;
        var unit = field?.GetUnit(unitId);
        var cardId = unit?.CardId;
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return unitId;
        }

        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && map.TryGetValue(cardId.Trim(), out var cfg) && cfg != null)
        {
            return cfg.DisplayName;
        }
        return cardId;
    }

    private static ShipFaction ResolveCardFaction(string cardId)
    {
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && !string.IsNullOrWhiteSpace(cardId) && map.TryGetValue(cardId.Trim(), out var cfg) && cfg != null)
        {
            return cfg.Faction;
        }
        return ShipFaction.Other;
    }

    private static bool IsMultiTargetKind(SkillTargetKind kind)
    {
        return kind == SkillTargetKind.All
            || kind == SkillTargetKind.AllEnemies
            || kind == SkillTargetKind.AllAllies;
    }

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
            if (_cachedDefaultProfile == null)
            {
                Debug.LogWarning("[EnemyAI] ResolveProfile: 未找到 Resources/DefaultAIProfile，将使用代码内建默认值");
            }
        }

        if (_cachedDefaultProfile != null)
        {
            return _cachedDefaultProfile;
        }

        Debug.LogWarning("[EnemyAI] ResolveProfile: 无 AIProfile 配置，使用代码内建默认值（关卡可能缺少 EnemyAIProfile 设置）");
        return ScriptableObject.CreateInstance<AIProfileSO>();
    }

    #endregion
}
