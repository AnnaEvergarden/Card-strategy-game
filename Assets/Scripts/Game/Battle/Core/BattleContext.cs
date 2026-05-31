/// <summary>
/// 本局战斗上下文：技能执行期间的共享入口（单例）。
/// 运行时 HP/冷却等由 <see cref="Field"/> 持有；回合数以 <see cref="Turns.RoundNumber"/> 为准。
/// </summary>
public sealed class BattleContext
{
    #region Fields

    /// <summary>
    /// 当前局战斗上下文单例（单场景战斗流程足够；多局并行时可改为注入）。
    /// </summary>
    private static readonly BattleContext Instance = new();

    #endregion

    #region Properties

    /// <summary>
    /// 当前战斗上下文。
    /// </summary>
    public static BattleContext Current => Instance;

    /// <summary>
    /// 本局场上状态（HP、上场列表、技能次数与冷却）。
    /// </summary>
    public BattleFieldState Field { get; } = new();

    /// <summary>
    /// 本局回合与行动权。
    /// </summary>
    public BattleTurnSystem Turns { get; }

    /// <summary>
    /// 战斗事件总线（伤害、死亡、技能流程等）。
    /// </summary>
    public BattleEventBus Events { get; } = new();

    /// <summary>
    /// 当前正在执行的技能流水线上下文；非 null 时 <see cref="BattleUnit.TakeDamage"/> 暂存事件而非立即发布。
    /// </summary>
    public SkillExecutionContext ActiveSkillExecution { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// 构造并装配子系统依赖。
    /// </summary>
    private BattleContext()
    {
        Turns = new BattleTurnSystem(Field);
    }

    #endregion

    #region Public API

    /// <summary>
    /// 确保场上状态已初始化。
    /// </summary>
    public void EnsureReady()
    {
        Field.EnsureInitialized();
    }

    /// <summary>
    /// 离开战斗或放弃本局时重置全部战斗子系统。
    /// </summary>
    public void ResetBattle()
    {
        Field.ClearState();
        Turns.Reset();
        Events.ClearAll();
        BattleBuffState.Reset();
        ActiveSkillExecution = null;
    }

    #endregion
}
