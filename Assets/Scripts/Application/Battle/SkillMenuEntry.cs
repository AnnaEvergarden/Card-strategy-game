/// <summary>
/// 技能菜单条目：操作菜单中单个技能按钮的只读数据（冷却/次数/可用性由应用层统一判定）。
/// </summary>
public sealed class SkillMenuEntry
{
    /// <summary>技能 ID。</summary>
    public string SkillId { get; set; }

    /// <summary>显示名。</summary>
    public string DisplayName { get; set; }

    /// <summary>描述（Tooltip/详情）。</summary>
    public string Description { get; set; }

    /// <summary>图标资源 ID（0 = 占位图）。</summary>
    public int IconResourceId { get; set; }

    /// <summary>剩余冷却回合（0 = 可用）。</summary>
    public int CooldownRemaining { get; set; }

    /// <summary>本场剩余次数（-1 = 无限制）。</summary>
    public int RemainingUses { get; set; }

    /// <summary>是否当前可释放。</summary>
    public bool IsUsable { get; set; }

    /// <summary>不可释放原因（可用时为空）。</summary>
    public string BlockReason { get; set; }
}
