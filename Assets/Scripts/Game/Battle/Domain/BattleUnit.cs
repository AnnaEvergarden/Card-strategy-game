using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗单位（Domain）：持久化对象，内嵌 HP/防御/技能/Buff 等完整运行时数据。
/// 由 <see cref="BattleFieldState"/> 创建并在槽位间绑定/解绑。
/// </summary>
public sealed class BattleUnit
{
    #region Fields

    private readonly string _unitId;
    private readonly string _cardId;
    private readonly BattleSide _side;
    private readonly BattleContext _battle;
    private int _hp;
    private int _defense;

    /// <summary>
    /// 本局技能冷却剩余回合：skillId → 剩余回合（0=可释放）。
    /// </summary>
    private readonly Dictionary<string, int> _skillCooldowns = new();

    /// <summary>
    /// 本局技能剩余次数：skillId → 剩余次数（-1=无限）。
    /// </summary>
    private readonly Dictionary<string, int> _skillRemainingUses = new();

    /// <summary>
    /// 挂载的 Buff 列表。
    /// </summary>
    private readonly List<Buff> _buffs = new();

    #endregion

    #region Constructors

    /// <summary>
    /// 创建战斗单位。
    /// </summary>
    /// <param name="battle">战斗上下文。</param>
    /// <param name="unitId">单位唯一标识（如 "P1_0"）。</param>
    /// <param name="cardId">配置表 cardId。</param>
    /// <param name="side">所属阵营。</param>
    /// <param name="maxHp">初始 HP（来自 CardConfigSO）。</param>
    /// <param name="defense">初始防御力。</param>
    public BattleUnit(BattleContext battle, string unitId, string cardId, BattleSide side, int maxHp, int defense = 0)
    {
        _battle = battle;
        _unitId = unitId;
        _cardId = cardId;
        _side = side;
        _hp = maxHp;
        _defense = defense;
    }

    #endregion

    #region Properties

    /// <summary>
    /// 单位唯一标识（如 "P1_0" ~ "P1_5"、"P2_0" ~ "P2_5"）。
    /// </summary>
    public string UnitId => _unitId;

    /// <summary>
    /// 卡牌配置 id。
    /// </summary>
    public string CardId => _cardId;

    /// <summary>
    /// 所属阵营。
    /// </summary>
    public BattleSide Side => _side;

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public int Hp => _hp;

    /// <summary>
    /// 当前防御力。
    /// </summary>
    public int Defense => _defense;

    /// <summary>
    /// 是否已阵亡。
    /// </summary>
    public bool IsDead => _hp <= 0;

    /// <summary>
    /// 只读 Buff 列表。
    /// </summary>
    public IReadOnlyList<Buff> Buffs => _buffs;

    #endregion

    #region Damage & Healing

    /// <summary>
    /// 受到伤害：直接修改 HP，在技能流水线内暂存事件，非流水线路径立即发布。
    /// </summary>
    /// <param name="damage">请求伤害值。</param>
    /// <returns>实际扣除的 HP。</returns>
    public int TakeDamage(int damage)
    {
        if (_hp <= 0)
        {
            return 0;
        }

        damage = Mathf.Max(0, damage);
        var mitigatedDamage = Mathf.Max(0, damage - _defense);
        var applied = Mathf.Min(mitigatedDamage, _hp);
        _hp -= applied;

        var activeExecution = _battle?.ActiveSkillExecution;
        if (activeExecution != null)
        {
            activeExecution.StageDamageOutcome(_cardId, applied, _hp);
        }
        else
        {
            _battle?.Events.Publish(new DamageEvent(_cardId, applied, _hp));
            if (_hp <= 0)
            {
                _battle?.Events.Publish(new DeadEvent(_cardId));
            }
        }

        return applied;
    }

    /// <summary>
    /// 恢复生命值。
    /// </summary>
    /// <param name="amount">恢复量。</param>
    /// <returns>实际恢复的 HP。</returns>
    public int Heal(int amount)
    {
        if (_hp <= 0 || amount <= 0)
        {
            return 0;
        }

        var maxHp = ResolveMaxHp();
        var missing = Mathf.Max(0, maxHp - _hp);
        var applied = Mathf.Min(amount, missing);
        _hp += applied;
        return applied;
    }

    #endregion

    #region Skill State

    /// <summary>
    /// 获取技能剩余可用次数；无限制时返回 <see cref="int.MaxValue"/>。
    /// </summary>
    public int GetSkillRemainingUses(string skillId)
    {
        if (_skillRemainingUses.TryGetValue(skillId, out var remaining))
        {
            return remaining;
        }
        return int.MaxValue;
    }

    /// <summary>
    /// 是否仍可释放该技能（冷却=0 且剩余次数>0）。
    /// </summary>
    public bool CanUseSkill(SkillConfigSO skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
        {
            return false;
        }
        return GetSkillCooldownRemaining(skill.SkillId) <= 0 && GetSkillRemainingUses(skill.SkillId) > 0;
    }

    /// <summary>
    /// 获取技能冷却剩余回合数（0 表示可释放）。
    /// </summary>
    public int GetSkillCooldownRemaining(string skillId)
    {
        if (_skillCooldowns.TryGetValue(skillId, out var turns))
        {
            return Mathf.Max(0, turns);
        }
        return 0;
    }

    /// <summary>
    /// 消耗一次技能使用次数；无限制时返回 true。
    /// </summary>
    public bool ConsumeSkillUse(string skillId)
    {
        if (!_skillRemainingUses.TryGetValue(skillId, out var remaining))
        {
            return true; // 无限使用
        }
        if (remaining <= 0)
        {
            return false;
        }
        _skillRemainingUses[skillId] = remaining - 1;
        return true;
    }

    /// <summary>
    /// 设置技能冷却（释放成功后调用）。
    /// </summary>
    public void SetCooldown(string skillId, int cooldownTurns)
    {
        if (cooldownTurns > 0)
        {
            _skillCooldowns[skillId] = cooldownTurns;
        }
    }

    /// <summary>
    /// 清空指定技能冷却（概率刷新等效果）。
    /// </summary>
    /// <returns>是否曾存在冷却条目。</returns>
    public bool ClearCooldown(string skillId)
    {
        return _skillCooldowns.Remove(skillId);
    }

    /// <summary>
    /// 内部：HP 回滚（流水线失败时撤销变更，与 TakeDamage/Heal 的符号相反）。
    /// </summary>
    internal void ApplyHpRollback(int delta)
    {
        _hp -= delta;
        if (_hp < 0)
        {
            _hp = 0;
        }
    }

    /// <summary>
    /// 内部：获取冷却快照（流水线回滚用）。
    /// </summary>
    internal bool TryGetCooldownSnapshot(string skillId, out int previousRemaining)
    {
        return _skillCooldowns.TryGetValue(skillId, out previousRemaining);
    }

    /// <summary>
    /// 内部：恢复冷却至清空前状态（流水线回滚用）。
    /// </summary>
    internal void RestoreCooldown(string skillId, int previousRemaining, bool hadEntry)
    {
        if (!hadEntry || previousRemaining <= 0)
        {
            _skillCooldowns.Remove(skillId);
        }
        else
        {
            _skillCooldowns[skillId] = previousRemaining;
        }
    }

    /// <summary>
    /// 内部：调整防御力（Buff 系统用）。
    /// </summary>
    internal void AddDefense(int delta)
    {
        _defense = Mathf.Max(0, _defense + delta);
    }

    /// <summary>
    /// 回合结束时减少冷却。
    /// </summary>
    public void TickCooldowns()
    {
        var keys = new List<string>(_skillCooldowns.Keys);
        foreach (var key in keys)
        {
            if (!_skillCooldowns.TryGetValue(key, out var turns))
            {
                continue;
            }
            if (turns <= 1)
            {
                _skillCooldowns.Remove(key);
            }
            else
            {
                _skillCooldowns[key] = turns - 1;
            }
        }
    }

    /// <summary>
    /// 初始化技能剩余次数（由 <see cref="BattleFieldState"/> 在创建时调用）。
    /// </summary>
    internal void InitializeSkillUses(List<SkillConfigSO> skills)
    {
        _skillRemainingUses.Clear();
        foreach (var skill in skills)
        {
            if (skill == null || skill.IsUnlimitedUses || string.IsNullOrWhiteSpace(skill.SkillId))
            {
                continue;
            }
            _skillRemainingUses[skill.SkillId] = skill.ConfiguredMaxUses;
        }
    }

    #endregion

    #region Buffs

    /// <summary>
    /// 添加 Buff。
    /// </summary>
    public void AddBuff(Buff buff)
    {
        if (buff == null)
        {
            return;
        }
        _buffs.Add(buff);
    }

    /// <summary>
    /// 移除指定 Buff。
    /// </summary>
    public void RemoveBuff(Buff buff)
    {
        _buffs.Remove(buff);
    }

    /// <summary>
    /// 清除所有 Buff。
    /// </summary>
    public void ClearBuffs()
    {
        _buffs.Clear();
    }

    #endregion

    #region Private Helpers

    private int ResolveMaxHp()
    {
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && map.TryGetValue(_cardId, out var cfg) && cfg != null)
        {
            return cfg.HP;
        }
        return _hp; // 兜底：以当前 HP 为上限
    }

    #endregion
}
