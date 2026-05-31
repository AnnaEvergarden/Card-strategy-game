using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本局战斗 Buff 状态：按 cardId 记录持续类效果，回合结束时扣减；具体数值变更由 <see cref="BattleBuffHandlerRegistry"/> 分发。
/// </summary>
public static class BattleBuffState
{
    #region Nested Types

    /// <summary>
    /// 单条运行时 Buff。
    /// </summary>
    public sealed class RuntimeBuff
    {
        /// <summary>
        /// Buff 种类。
        /// </summary>
        public SkillBuffKind Kind;

        /// <summary>
        /// 数值（如防御加成）。
        /// </summary>
        public int Value;

        /// <summary>
        /// 剩余回合数。
        /// </summary>
        public int RemainingTurns;
    }

    #endregion

    #region Fields

    /// <summary>
    /// cardId → Buff 列表。
    /// </summary>
    private static readonly Dictionary<string, List<RuntimeBuff>> BuffsByCardId = new();

    /// <summary>
    /// 遍历字典时的 cardId 缓冲（避免迭代中修改字典）。
    /// </summary>
    private static readonly List<string> CardIdSweepBuffer = new(16);

    #endregion

    #region Public API

    /// <summary>
    /// 清空本局 Buff（离开战斗时与 <see cref="BattleContext.ResetBattle"/> 一并调用）。
    /// </summary>
    public static void Reset()
    {
        BuffsByCardId.Clear();
        CardIdSweepBuffer.Clear();
    }

    /// <summary>
    /// 施加一条 Buff（由 Effect 或系统调用）。
    /// </summary>
    public static void ApplyBuff(string cardId, SkillBuffKind kind, int value, int durationTurns)
    {
        if (string.IsNullOrWhiteSpace(cardId) || kind == SkillBuffKind.None || value <= 0)
        {
            return;
        }

        cardId = cardId.Trim();
        durationTurns = Mathf.Max(1, durationTurns);

        if (!BuffsByCardId.TryGetValue(cardId, out var list))
        {
            list = new List<RuntimeBuff>(4);
            BuffsByCardId[cardId] = list;
        }

        var buff = new RuntimeBuff
        {
            Kind = kind,
            Value = value,
            RemainingTurns = durationTurns
        };
        list.Add(buff);
        BattleBuffHandlerRegistry.OnApplied(cardId, buff);
    }

    /// <summary>
    /// 获取某卡当前防御类 Buff 数值合计。
    /// </summary>
    public static int GetDefenseBonusTotal(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || !BuffsByCardId.TryGetValue(cardId.Trim(), out var list))
        {
            return 0;
        }

        var sum = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var b = list[i];
            if (b != null && b.Kind == SkillBuffKind.DefenseBuff)
            {
                sum += b.Value;
            }
        }

        return sum;
    }

    /// <summary>
    /// 读取某卡当前 Buff 列表。
    /// </summary>
    public static IReadOnlyList<RuntimeBuff> GetBuffs(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) ||
            !BuffsByCardId.TryGetValue(cardId.Trim(), out var list) ||
            list == null)
        {
            return System.Array.Empty<RuntimeBuff>();
        }

        return list;
    }

    /// <summary>
    /// 移除指定 Buff 并经由 Handler 回收数值（技能流水线回滚用）。
    /// </summary>
    /// <returns>是否找到并移除。</returns>
    public static bool TryRemoveBuff(string cardId, RuntimeBuff buff)
    {
        if (string.IsNullOrWhiteSpace(cardId) || buff == null)
        {
            return false;
        }

        cardId = cardId.Trim();
        if (!BuffsByCardId.TryGetValue(cardId, out var list) || list == null)
        {
            return false;
        }

        if (!list.Remove(buff))
        {
            return false;
        }

        BattleBuffHandlerRegistry.OnExpired(cardId, buff);
        if (list.Count == 0)
        {
            BuffsByCardId.Remove(cardId);
        }

        return true;
    }

    /// <summary>
    /// 施加 Buff 并返回运行时条目引用（供回滚移除）。
    /// </summary>
    public static RuntimeBuff ApplyBuffAndGet(string cardId, SkillBuffKind kind, int value, int durationTurns)
    {
        if (string.IsNullOrWhiteSpace(cardId) || kind == SkillBuffKind.None || value <= 0)
        {
            return null;
        }

        cardId = cardId.Trim();
        durationTurns = Mathf.Max(1, durationTurns);

        if (!BuffsByCardId.TryGetValue(cardId, out var list))
        {
            list = new List<RuntimeBuff>(4);
            BuffsByCardId[cardId] = list;
        }

        var buff = new RuntimeBuff
        {
            Kind = kind,
            Value = value,
            RemainingTurns = durationTurns
        };
        list.Add(buff);
        BattleBuffHandlerRegistry.OnApplied(cardId, buff);
        return buff;
    }

    /// <summary>
    /// 回合结束：扣减剩余回合，到期则经注册表回收效果。
    /// </summary>
    public static void TickTurnEnd()
    {
        CardIdSweepBuffer.Clear();
        foreach (var pair in BuffsByCardId)
        {
            CardIdSweepBuffer.Add(pair.Key);
        }

        for (var c = 0; c < CardIdSweepBuffer.Count; c++)
        {
            var cardId = CardIdSweepBuffer[c];
            if (!BuffsByCardId.TryGetValue(cardId, out var list) || list == null || list.Count == 0)
            {
                continue;
            }

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var buff = list[i];
                if (buff == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                buff.RemainingTurns--;
                if (buff.RemainingTurns > 0)
                {
                    continue;
                }

                BattleBuffHandlerRegistry.OnExpired(cardId, buff);
                list.RemoveAt(i);
            }
        }
    }

    #endregion
}
