using System;
using System.Collections.Generic;

/// <summary>
/// 战斗单位：一场战斗中某张卡牌的运行时状态（固定 UnitId 绑定，取代 cardId+Side 双参传递）。
/// 拥有 HP / 防御 / Buff / 技能冷却与次数，并遵循数据所有权原则——
/// HP 属于本类，因此 UnitDamagedEvent / UnitDiedEvent 由本类发布，技能系统不直接修改 HP。
/// 纯 C# 领域对象：不引用 UnityEngine，可脱离引擎独立单元测试。
/// </summary>
public sealed class BattleUnit
{
    private readonly List<Buff> _buffs = new List<Buff>();
    private readonly Dictionary<string, int> _skillCooldowns = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _skillRemainingUses = new Dictionary<string, int>();
    private readonly IBattleEventBus _eventBus;

    /// <summary>战斗内唯一标识（如 "P1_0"）。</summary>
    public string UnitId { get; }

    /// <summary>对应的配置表卡牌 ID。</summary>
    public string CardId { get; }

    /// <summary>显示名称（日志与 UI 使用）。</summary>
    public string DisplayName { get; }

    /// <summary>所属阵营（构造时确定，不可变更）。</summary>
    public BattleSide Side { get; }

    /// <summary>最大生命值。</summary>
    public int MaxHp { get; }

    /// <summary>当前生命值（只读，只能经 TakeDamage/Heal 修改）。</summary>
    public int Hp { get; private set; }

    /// <summary>基础防御（不含 Buff）。</summary>
    public int BaseDefense { get; }

    /// <summary>实际防御 = 基础防御 + 防御类 Buff 总和。</summary>
    public int Defense => BaseDefense + GetBuffTotalValue(BuffKind.DefenseBuff);

    /// <summary>是否已阵亡。</summary>
    public bool IsDead => Hp <= 0;

    /// <summary>当前携带的 Buff 列表（只读视图）。</summary>
    public IReadOnlyList<Buff> Buffs => _buffs;

    /// <summary>本单位可用的技能 ID 列表（来自卡牌配置，会话构建时注入）。</summary>
    public IReadOnlyList<string> SkillIds { get; }

    /// <summary>技能冷却表（skillId → 剩余冷却回合），只读视图。</summary>
    public IReadOnlyDictionary<string, int> SkillCooldowns => _skillCooldowns;

    /// <summary>
    /// 构造战斗单位。
    /// </summary>
    /// <param name="unitId">战斗内唯一标识</param>
    /// <param name="cardId">配置表卡牌 ID</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="side">所属阵营</param>
    /// <param name="maxHp">最大生命值</param>
    /// <param name="baseDefense">基础防御</param>
    /// <param name="eventBus">战斗事件总线（可为空：纯逻辑测试场景不发事件）</param>
    /// <param name="skillIds">技能 ID 列表（可空：无技能单位）</param>
    public BattleUnit(string unitId, string cardId, string displayName, BattleSide side,
        int maxHp, int baseDefense, IBattleEventBus eventBus,
        IReadOnlyList<string> skillIds = null)
    {
        if (string.IsNullOrEmpty(unitId))
            throw new ArgumentException("UnitId 不能为空。", nameof(unitId));
        if (string.IsNullOrEmpty(cardId))
            throw new ArgumentException("CardId 不能为空。", nameof(cardId));

        UnitId = unitId;
        CardId = cardId;
        DisplayName = displayName;
        Side = side;
        MaxHp = maxHp > 0 ? maxHp : 1;
        Hp = MaxHp;
        BaseDefense = baseDefense < 0 ? 0 : baseDefense;
        SkillIds = skillIds ?? new List<string>(0);
        _eventBus = eventBus;
    }

    /// <summary>
    /// 受击结算：先扣除防御，实际伤害下限为 0、上限为当前 HP；
    /// 已阵亡单位不受伤害。发布 UnitDamagedEvent；致死时发布 UnitDiedEvent。
    /// </summary>
    /// <param name="rawDamage">原始伤害值</param>
    /// <param name="appliedDamage">输出：实际扣减的 HP</param>
    public void TakeDamage(int rawDamage, out int appliedDamage)
    {
        appliedDamage = 0;

        if (IsDead)
            return;

        var effective = rawDamage - Defense;
        if (effective < 0)
            effective = 0;

        appliedDamage = effective < Hp ? effective : Hp;
        if (appliedDamage <= 0)
        {
            // 完全被防御抵挡时仍发事件（供 UI 播放"格挡"表现）。
            _eventBus?.Publish(new UnitDamagedEvent(UnitId, Side, CardId, 0, Hp));
            return;
        }

        Hp -= appliedDamage;
        _eventBus?.Publish(new UnitDamagedEvent(UnitId, Side, CardId, appliedDamage, Hp));

        if (IsDead)
            _eventBus?.Publish(new UnitDiedEvent(UnitId, Side, CardId));
    }

    /// <summary>
    /// 治疗结算：回复量上限为缺失生命；已阵亡单位不可治疗。
    /// </summary>
    /// <param name="amount">治疗量</param>
    /// <param name="healed">输出：实际回复的 HP</param>
    public void Heal(int amount, out int healed)
    {
        healed = 0;

        if (IsDead || amount <= 0)
            return;

        var missing = MaxHp - Hp;
        healed = amount < missing ? amount : missing;
        if (healed <= 0)
            return;

        Hp += healed;
        _eventBus?.Publish(new UnitHealedEvent(UnitId, Side, CardId, healed, Hp));
    }

    /// <summary>
    /// 添加 Buff：同种类 Buff 不叠加，后到者整体替换（数值与持续时间均取新值）。
    /// 发布 BuffAddedEvent。
    /// </summary>
    /// <param name="buff">Buff 实例</param>
    public void AddBuff(Buff buff)
    {
        if (IsDead || buff == null)
            return;

        // 同种类替换：移除旧条目。
        for (var i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].Kind == buff.Kind)
            {
                _buffs.RemoveAt(i);
                break;
            }
        }

        _buffs.Add(buff);
        _eventBus?.Publish(new BuffAddedEvent(UnitId, buff.Kind, buff.Value, buff.RemainingTurns));
    }

    /// <summary>
    /// 是否携带指定种类的 Buff。
    /// </summary>
    public bool HasBuff(BuffKind kind)
    {
        for (var i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].Kind == kind)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取指定种类 Buff 的数值总和。
    /// </summary>
    public int GetBuffTotalValue(BuffKind kind)
    {
        var total = 0;
        for (var i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].Kind == kind)
                total += _buffs[i].Value;
        }
        return total;
    }

    /// <summary>
    /// 回合结束时 Tick 所有 Buff：持续时间 -1，到期移除并发布 BuffExpiredEvent。
    /// </summary>
    public void TickBuffsEndOfRound()
    {
        for (var i = _buffs.Count - 1; i >= 0; i--)
        {
            var buff = _buffs[i];
            if (buff.TickTurnEnd())
            {
                _buffs.RemoveAt(i);
                _eventBus?.Publish(new BuffExpiredEvent(UnitId, buff.Kind));
            }
        }
    }

    /// <summary>
    /// 直接损耗生命（技能代价，无视防御）：发受击事件、可致死。
    /// 与 TakeDamage 的区别：不经过防御结算，SelfHpDrain 类效果使用。
    /// </summary>
    /// <param name="amount">损耗量</param>
    /// <param name="drained">输出：实际损耗的 HP</param>
    public void DrainHp(int amount, out int drained)
    {
        drained = 0;

        if (IsDead || amount <= 0)
            return;

        drained = amount < Hp ? amount : Hp;
        Hp -= drained;
        _eventBus?.Publish(new UnitDamagedEvent(UnitId, Side, CardId, drained, Hp));

        if (IsDead)
            _eventBus?.Publish(new UnitDiedEvent(UnitId, Side, CardId));
    }

    // ───────────────────────── 回滚支持 ─────────────────────────
    // 以下方法仅供技能管线在"后续效果失败"时逆序撤销已生效效果使用，
    // 不发布事件（静默恢复），避免表现层闪烁。

    /// <summary>
    /// 回滚 HP 变化：amount 为正恢复（回滚伤害/损耗）、为负扣回（回滚治疗），
    /// 结果钳制在 [0, MaxHp]（不触发事件；若单位曾因管线伤害"阵亡"，此调用使其复活，
    /// 已发出的 UnitDiedEvent 由会话层的后续事件自然覆盖——此为已知可接受的罕见表现边角）。
    /// </summary>
    internal void RestoreHpForRollback(int amount)
    {
        if (amount == 0)
            return;

        Hp += amount;
        if (Hp > MaxHp)
            Hp = MaxHp;
        if (Hp < 0)
            Hp = 0;
    }

    /// <summary>
    /// 回滚 Buff：移除指定种类的首个 Buff（不触发事件）。
    /// </summary>
    internal void RemoveBuffForRollback(BuffKind kind)
    {
        for (var i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].Kind == kind)
            {
                _buffs.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 设置技能冷却（释放技能成功后调用）。
    /// </summary>
    /// <param name="skillId">技能 ID</param>
    /// <param name="turns">冷却回合数</param>
    public void SetSkillCooldown(string skillId, int turns)
    {
        if (string.IsNullOrEmpty(skillId))
            return;

        if (turns <= 0)
            _skillCooldowns.Remove(skillId);
        else
            _skillCooldowns[skillId] = turns;
    }

    /// <summary>
    /// 回合结束时 Tick 所有技能冷却：剩余冷却 -1，归零移除。
    /// </summary>
    public void TickSkillCooldowns()
    {
        if (_skillCooldowns.Count == 0)
            return;

        // 注意：该运行时下字典索引器写入同样会使枚举器失效，
        // 故先取键快照，再逐个更新/移除，避免遍历中修改集合。
        var keys = new List<string>(_skillCooldowns.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            var remaining = _skillCooldowns[keys[i]] - 1;
            if (remaining <= 0)
                _skillCooldowns.Remove(keys[i]);
            else
                _skillCooldowns[keys[i]] = remaining;
        }
    }

    /// <summary>
    /// 查询技能是否冷却中。
    /// </summary>
    /// <param name="skillId">技能 ID</param>
    /// <param name="remainingTurns">输出：剩余冷却回合（未冷却为 0）</param>
    public bool IsSkillOnCooldown(string skillId, out int remainingTurns)
    {
        if (_skillCooldowns.TryGetValue(skillId, out remainingTurns))
            return true;
        remainingTurns = 0;
        return false;
    }

    /// <summary>
    /// 设置技能本场剩余可用次数（战斗开始时按配置表 UseLimit 初始化）。
    /// </summary>
    /// <param name="skillId">技能 ID</param>
    /// <param name="uses">剩余次数（小于 0 表示无限制）</param>
    public void SetSkillRemainingUses(string skillId, int uses)
    {
        if (string.IsNullOrEmpty(skillId))
            return;

        if (uses < 0)
            _skillRemainingUses.Remove(skillId); // 无限制：不占条目
        else
            _skillRemainingUses[skillId] = uses;
    }

    /// <summary>
    /// 查询技能本场剩余次数。
    /// </summary>
    /// <param name="skillId">技能 ID</param>
    /// <param name="remainingUses">输出：剩余次数（-1 表示无限制）</param>
    public void GetSkillRemainingUses(string skillId, out int remainingUses)
    {
        if (!_skillRemainingUses.TryGetValue(skillId, out remainingUses))
            remainingUses = -1;
    }

    /// <summary>
    /// 消耗一次技能使用次数：未配置次数的技能视为无限制恒成功；
    /// 次数耗尽返回 false（调用方应在释放前用 CanUseSkill 校验）。
    /// </summary>
    public bool TryConsumeSkillUse(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        if (!_skillRemainingUses.TryGetValue(skillId, out var remaining))
            return true; // 未配置 = 无限制

        if (remaining <= 0)
            return false;

        _skillRemainingUses[skillId] = remaining - 1;
        return true;
    }

    /// <summary>
    /// 技能是否可释放：存活 + 不在冷却 + 次数未耗尽。
    /// </summary>
    public bool CanUseSkill(string skillId)
    {
        if (IsDead || string.IsNullOrEmpty(skillId))
            return false;

        if (IsSkillOnCooldown(skillId, out _))
            return false;

        if (_skillRemainingUses.TryGetValue(skillId, out var remaining) && remaining <= 0)
            return false;

        return true;
    }
}
