/// <summary>
/// AI 评分计算器接口：对特定效果类型计算 Utility 评分。
/// 所有标识使用 UnitId，禁止 CardId 运行时传递。
/// </summary>
public interface IScoreCalculator
{
    /// <summary>
    /// 计算评分。
    /// </summary>
    /// <param name="effectValue">效果配置值（伤害量/治疗量/防御值等）。</param>
    /// <param name="casterUnitId">施法者 UnitId。</param>
    /// <param name="targetUnitId">目标 UnitId（空字符串 = 无目标）。</param>
    /// <param name="profile">AI 权重配置。</param>
    /// <returns>评分值（可为负值）。</returns>
    float Calculate(int effectValue, string casterUnitId, string targetUnitId, AIProfileSO profile);
}
