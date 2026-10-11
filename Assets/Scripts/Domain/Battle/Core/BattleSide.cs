/// <summary>
/// 战斗阵营枚举：P1 为玩家侧，P2 为敌方/NPC 侧。
/// 双阵营对称设计：回合系统与 AI 均按阵营参数化，不硬编码玩家与敌方，
/// 未来 PvP 双真人对战时无需改动战斗核心。
/// </summary>
public enum BattleSide
{
    /// <summary>玩家侧（先手方）。</summary>
    P1 = 0,

    /// <summary>敌方侧（NPC/AI 方）。</summary>
    P2 = 1,
}
