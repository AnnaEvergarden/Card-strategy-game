using System;
using System.Collections.Generic;

/// <summary>
/// Utility AI 决策服务：按阵营评估"单位 × 技能 × 目标"的全部候选并评分，
/// 与玩家共用同一 SkillSystem 释放管道（AI 不享有规则外特权）。
/// 双阵营参数化：side = P1 时用于玩家托管自动战斗，side = P2 时用于敌方 AI。
/// </summary>
public sealed class BattleAIService
{
    private readonly BattleField _field;
    private readonly BattleTurnSystem _turns;
    private readonly SkillSystem _skills;
    private readonly TargetSelector _targetSelector;
    private readonly ScoreCalculatorRegistry _calculators;
    private readonly Func<string, SkillConfig> _skillResolver;
    private readonly Random _random;

    /// <summary>
    /// 构造 AI 决策服务。
    /// </summary>
    /// <param name="field">战场</param>
    /// <param name="turns">回合系统</param>
    /// <param name="skills">技能系统（AI 与玩家共用释放管道）</param>
    /// <param name="skillResolver">技能 ID → 配置解析器（由应用层注入 ConfigService 查询）</param>
    /// <param name="targetSelector">目标选择器</param>
    /// <param name="calculators">评分器注册表</param>
    /// <param name="random">随机源</param>
    public BattleAIService(BattleField field, BattleTurnSystem turns, SkillSystem skills,
        Func<string, SkillConfig> skillResolver, TargetSelector targetSelector,
        ScoreCalculatorRegistry calculators, Random random)
    {
        _field = field;
        _turns = turns;
        _skills = skills;
        _skillResolver = skillResolver ?? (_ => null);
        _targetSelector = targetSelector ?? new TargetSelector();
        _calculators = calculators ?? new ScoreCalculatorRegistry();
        _random = random ?? new Random();
    }

    /// <summary>
    /// 评估指定阵营当前可执行的全部行动候选（不执行，仅供决策/调试）。
    /// 候选范围：上场存活且本回合未行动的单位 × 可释放技能 × 合法目标。
    /// </summary>
    /// <param name="side">阵营</param>
    /// <param name="results">输出：候选列表（按评分降序）</param>
    public void EvaluateBestActions(BattleSide side, List<AIAction> results)
    {
        results.Clear();

        var actives = _field.GetActiveUnits(side);
        for (var i = 0; i < actives.Count; i++)
        {
            var unit = actives[i];
            if (unit.IsDead || !_turns.CanUnitAct(unit.UnitId))
                continue;

            EvaluateUnitActions(unit, results);
        }

        // 评分降序：AI 总是先执行当前局势下价值最高的行动。
        results.Sort((a, b) => b.Score.CompareTo(a.Score));
    }

    /// <summary>
    /// 执行单个 AI 行动：走技能系统释放管道，成功后消耗该槽位行动权。
    /// </summary>
    /// <returns>是否行动成功</returns>
    public bool ExecuteSingleAction(AIAction action)
    {
        if (action == null)
            return false;

        var ok = _skills.TryCast(action.CasterUnitId, action.Skill, action.TargetUnitId, out var outcome);
        if (!ok || !outcome.IsSuccess)
            return false;

        if (_field.TryGetSlotOfUnit(action.CasterUnitId, out var side, out var slotIndex))
            _turns.TryCompleteSlotAction(side, slotIndex);

        return true;
    }

    /// <summary>
    /// 跳过指定阵营全部未行动槽位（AI 阶段无可执行行动时收尾用）。
    /// </summary>
    public void PassRemainingUnits(BattleSide side)
    {
        _turns.PassRemainingSideActions(side);
    }

    /// <summary>
    /// 评估单个单位的全部（技能 × 目标）候选。
    /// </summary>
    private void EvaluateUnitActions(BattleUnit unit, List<AIAction> results)
    {
        for (var i = 0; i < unit.SkillIds.Count; i++)
        {
            var skillId = unit.SkillIds[i];

            // 校验放行：冷却中/次数耗尽的技能不产生候选。
            if (!unit.CanUseSkill(skillId))
                continue;

            var skill = _skillResolver(skillId);
            if (skill == null || skill.Effects == null || skill.Effects.Count == 0)
                continue;

            var context = new SkillExecutionContext(unit, null, skill, _field, _random);

            if (_targetSelector.RequiresManualTarget(skill))
            {
                EvaluateManualTargetActions(unit, skill, context, results);
            }
            else
            {
                if (!_targetSelector.TrySelectTargets(context, skill, null, out var targets, out _))
                    continue;

                var score = ScoreSkill(unit, skill, targets);
                if (score > float.MinValue)
                    results.Add(new AIAction(unit.UnitId, skill, null, score));
            }
        }
    }

    /// <summary>
    /// 评估需要手动目标的技能：对每个合法候选目标分别评分。
    /// </summary>
    private void EvaluateManualTargetActions(BattleUnit unit, SkillConfig skill,
        SkillExecutionContext context, List<AIAction> results)
    {
        var enemySide = skill.TargetKind == SkillTargetKind.SingleEnemy
            ? (unit.Side == BattleSide.P1 ? BattleSide.P2 : BattleSide.P1)
            : unit.Side;

        var candidates = _field.GetActiveUnits(enemySide);
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate.IsDead)
                continue;

            // 单体友方允许以施法者自身为目标（如自我治疗）。
            if (skill.TargetKind == SkillTargetKind.SingleAlly && candidate.Side != unit.Side)
                continue;

            if (!_targetSelector.TrySelectTargets(context, skill, candidate.UnitId, out var targets, out _))
                continue;

            var score = ScoreSkill(unit, skill, targets);
            results.Add(new AIAction(unit.UnitId, skill, candidate.UnitId, score));
        }
    }

    /// <summary>
    /// 综合评分：逐效果分发到评分器（作用于施法者自身的效果按单次计）。
    /// </summary>
    private float ScoreSkill(BattleUnit caster, SkillConfig skill, IReadOnlyList<BattleUnit> targets)
    {
        var total = 0f;

        for (var i = 0; i < skill.Effects.Count; i++)
        {
            var effect = skill.Effects[i];
            if (effect == null)
                continue;

            var calculator = _calculators.TryGet(effect.Kind);
            if (calculator == null)
                continue;

            if (effect.Kind == SkillEffectKind.SelfHpDrain
                || effect.Kind == SkillEffectKind.RefreshCooldownChance)
            {
                // 作用于施法者自身的效果：只计一次。
                total += calculator.Calculate(caster, caster, effect);
                continue;
            }

            for (var t = 0; t < targets.Count; t++)
            {
                var target = targets[t];
                if (target.IsDead)
                    continue;
                total += calculator.Calculate(caster, target, effect);
            }
        }

        return total;
    }
}
