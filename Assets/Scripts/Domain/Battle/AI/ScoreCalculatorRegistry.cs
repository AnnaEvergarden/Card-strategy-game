using System.Collections.Generic;

/// <summary>
/// 评分器注册表：效果种类 → 评分器。
/// </summary>
public sealed class ScoreCalculatorRegistry
{
    private readonly Dictionary<SkillEffectKind, IScoreCalculator> _calculators =
        new Dictionary<SkillEffectKind, IScoreCalculator>();

    /// <summary>
    /// 构造注册表并登记内置评分器。
    /// </summary>
    public ScoreCalculatorRegistry()
    {
        Register(new DamageScoreCalculator());
        Register(new HealScoreCalculator());
        Register(new DefenseBuffScoreCalculator());
        Register(new SelfDrainScoreCalculator());
        Register(new CooldownRefreshScoreCalculator());
    }

    /// <summary>
    /// 注册/替换评分器。
    /// </summary>
    public void Register(IScoreCalculator calculator)
    {
        if (calculator != null && calculator.Kind != SkillEffectKind.None)
            _calculators[calculator.Kind] = calculator;
    }

    /// <summary>
    /// 按效果种类获取评分器。
    /// </summary>
    /// <returns>评分器；未注册返回 null</returns>
    public IScoreCalculator TryGet(SkillEffectKind kind)
    {
        return _calculators.TryGetValue(kind, out var calculator) ? calculator : null;
    }
}
