using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 战斗应用服务接口：Presentation 层执行战斗操作的唯一入口。
/// UI 不直接触碰 BattleSession / BattleField 等领域对象，
/// 一切操作（选技能/选目标/换牌/结束回合/撤退）经此编排。
/// </summary>
public interface IBattleService
{
    /// <summary>当前会话的事件总线（UI 只订阅、不发布战斗数据事件）。</summary>
    IBattleEventBus EventBus { get; }

    /// <summary>是否存在进行中的战斗会话。</summary>
    bool HasActiveSession { get; }

    /// <summary>是否正在等待玩家选择技能目标（两段式选目标第二段）。</summary>
    bool IsAwaitingTarget { get; }

    /// <summary>
    /// 开始一场战斗：读取编队与关卡敌方配置 → 构建会话 → 发布开始事件。
    /// </summary>
    /// <param name="context">战斗入口上下文（关卡/编队/首发）</param>
    /// <returns>是否成功开局（配置缺失/编队为空等返回 false）</returns>
    UniTask<bool> StartBattleAsync(BattleStartContext context);

    /// <summary>
    /// 获取战斗只读视图（回合/双方槽位/替补快照），无会话时返回 null。
    /// </summary>
    BattleViewData GetBattleView();

    /// <summary>
    /// 获取指定单位的技能菜单条目（冷却/次数/可用性），无会话或单位不存在时返回空列表。
    /// </summary>
    IReadOnlyList<SkillMenuEntry> GetSkillMenuEntries(string unitId);

    /// <summary>
    /// 获取编队成员预览（战斗前的选卡界面展示），编队不存在时返回空列表。
    /// </summary>
    /// <param name="fleetIndex">编队索引</param>
    UniTask<IReadOnlyList<FleetCardPreview>> GetFleetPreviewAsync(int fleetIndex);

    /// <summary>
    /// 获取战斗结果（战斗结束后供结算面板展示），未结束时返回 null。
    /// </summary>
    BattleOutcome GetOutcome();

    /// <summary>
    /// 获取技能的合法手动目标（两段式选目标：第一段选中技能后调用）。
    /// </summary>
    IReadOnlyList<string> GetValidTargets(string casterUnitId, string skillId);

    /// <summary>
    /// 发起技能释放（第一段）：
    /// - 需要手动目标的技能：发布 SkillTargetSelectingEvent 进入选目标模式（等待 ConfirmCastTarget）；
    /// - 自动目标技能：立即释放并消耗行动权。
    /// </summary>
    /// <param name="casterUnitId">施法单位</param>
    /// <param name="skillId">技能 ID</param>
    /// <param name="failureReason">输出：失败原因</param>
    /// <returns>是否已释放或已进入选目标模式</returns>
    bool TryBeginCast(string casterUnitId, string skillId, out string failureReason);

    /// <summary>
    /// 确认技能目标（第二段）：对等待中的技能以指定目标释放。
    /// </summary>
    /// <param name="targetUnitId">玩家点选的目标</param>
    /// <param name="failureReason">输出：失败原因</param>
    /// <returns>是否释放成功</returns>
    bool ConfirmCastTarget(string targetUnitId, out string failureReason);

    /// <summary>
    /// 取消等待中的技能释放（退出选目标模式）。
    /// </summary>
    void CancelPendingCast();

    /// <summary>
    /// 换牌：场上单位下场、替补上场，消耗该槽位行动权。
    /// </summary>
    bool TrySwitchUnit(string outgoingUnitId, string incomingUnitId, out string failureReason);

    /// <summary>
    /// 结束玩家回合：跳过未行动槽位并交棒（P2 AI 阶段由服务自动驱动）。
    /// </summary>
    bool TryEndPlayerTurn(out string failureReason);

    /// <summary>
    /// 开关 P1 自动战斗（托管）。
    /// </summary>
    void SetAutoBattle(bool enabled);

    /// <summary>
    /// 撤退：立即认负结束战斗。
    /// </summary>
    bool TryRetreat();

    /// <summary>
    /// 清理会话（离开战斗场景时调用）。
    /// </summary>
    void ClearSession();
}
