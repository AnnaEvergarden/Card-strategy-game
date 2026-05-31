/// <summary>
/// 战斗场景 UI 会话：焦点舰娘、待释放技能（选目标）等。
/// 所有标识均使用 UnitId（全局唯一），禁止 CardId 运行时传递。
/// </summary>
public static class BattleUiSession
{
    #region Fields

    /// <summary>
    /// 当前焦点舰娘 UnitId。
    /// </summary>
    private static string _focusUnitId = string.Empty;

    /// <summary>
    /// 打开操作菜单时登记的行动归属 UnitId。
    /// </summary>
    private static string _actionOwnerUnitId = string.Empty;

    /// <summary>
    /// 待释放技能的施法者 UnitId。
    /// </summary>
    private static string _pendingCasterUnitId = string.Empty;

    /// <summary>
    /// 待释放技能 id。
    /// </summary>
    private static string _pendingSkillId = string.Empty;

    /// <summary>
    /// 待释放技能配置缓存。
    /// </summary>
    private static SkillConfigSO _pendingSkillConfig;

    #endregion

    #region Public API

    /// <summary>
    /// 当前焦点 UnitId（可能为空）。
    /// </summary>
    public static string FocusUnitId => _focusUnitId;

    /// <summary>
    /// 本回合打开操作菜单的舰娘 UnitId（换牌/技能消耗的是该卡的行动次数）。
    /// </summary>
    public static string ActionOwnerUnitId => _actionOwnerUnitId;

    /// <summary>
    /// 是否正在等待玩家点选技能目标。
    /// </summary>
    public static bool IsAwaitingSkillTarget =>
        !string.IsNullOrEmpty(_pendingCasterUnitId) && !string.IsNullOrEmpty(_pendingSkillId);

    /// <summary>
    /// 设置当前操作目标舰娘。
    /// </summary>
    /// <param name="unitId">单位 UnitId。</param>
    public static void SetFocusUnit(string unitId)
    {
        _focusUnitId = string.IsNullOrWhiteSpace(unitId) ? string.Empty : unitId.Trim();
    }

    /// <summary>
    /// 清空焦点。
    /// </summary>
    public static void ClearFocus()
    {
        _focusUnitId = string.Empty;
    }

    /// <summary>
    /// 记录本回合操作归属舰娘（打开槽位菜单时设置）。
    /// </summary>
    public static void SetActionOwnerUnit(string unitId)
    {
        _actionOwnerUnitId = string.IsNullOrWhiteSpace(unitId) ? string.Empty : unitId.Trim();
    }

    /// <summary>
    /// 清空操作归属。
    /// </summary>
    public static void ClearActionOwnerUnit()
    {
        _actionOwnerUnitId = string.Empty;
    }

    /// <summary>
    /// 记录待选目标的技能释放。
    /// </summary>
    public static void BeginPendingSkillCast(string casterUnitId, string skillId, SkillConfigSO skillConfig)
    {
        _pendingCasterUnitId = string.IsNullOrWhiteSpace(casterUnitId) ? string.Empty : casterUnitId.Trim();
        _pendingSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        _pendingSkillConfig = skillConfig;
    }

    /// <summary>
    /// 读取待释放技能信息。
    /// </summary>
    public static bool TryGetPendingSkillCast(out string casterUnitId, out string skillId, out SkillConfigSO skillConfig)
    {
        casterUnitId = _pendingCasterUnitId;
        skillId = _pendingSkillId;
        skillConfig = _pendingSkillConfig;
        return IsAwaitingSkillTarget;
    }

    /// <summary>
    /// 取消待释放技能。
    /// </summary>
    public static void ClearPendingSkillCast()
    {
        _pendingCasterUnitId = string.Empty;
        _pendingSkillId = string.Empty;
        _pendingSkillConfig = null;
    }

    #endregion
}
