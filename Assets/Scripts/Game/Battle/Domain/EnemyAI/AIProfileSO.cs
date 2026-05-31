using UnityEngine;

/// <summary>
/// 敌方 Utility AI 权重配置 ScriptableObject：定义各效果类型的基础权重、奖励/惩罚系数、生存倾向与 AOE 衰减等行为参数。
/// 挂载于 <see cref="LevelStageConfigSO.EnemyAIProfile"/>，实现每关卡独立的敌方行为调参。
/// 若关卡未分配此资产，<see cref="EnemyAIService.ResolveProfile"/> 将依次尝试 Resources 默认资产与代码内默认值。
/// </summary>
[CreateAssetMenu(
    fileName = "AIProfile",
    menuName = "CardGame/Battle/AI Profile")]
public sealed class AIProfileSO : ScriptableObject
{
    #region Fields

    [Header("基础权重")]
    [SerializeField] [Range(0f, 10f)] [Tooltip("即时伤害效果（SkillInstantKind.Damage）的评分权重。越高则敌方越倾向选择伤害技能。")]
    private float damageWeight = 1.0f;

    [SerializeField] [Range(0f, 10f)] [Tooltip("治疗效果（SkillInstantKind.Heal）的评分权重。越高则敌方越倾向治疗。")]
    private float healWeight = 0.8f;

    [SerializeField] [Range(0f, 10f)] [Tooltip("防御 Buff（SkillBuffKind.DefenseBuff）的评分权重。越高则敌方越倾向施加防御 Buff。")]
    private float defenseWeight = 0.6f;

    [SerializeField] [Range(0f, 10f)] [Tooltip("刷新冷却效果（SkillInstantKind.RefreshCooldown）的评分权重。仅当有技能处于冷却时生效。")]
    private float refreshCooldownWeight = 0.5f;

    [Header("奖励与惩罚")]
    [SerializeField] [Range(0f, 100f)] [Tooltip("击杀奖励分：当伤害值足以击杀目标时额外加此分数。用于鼓励集火残血。")]
    private float killBonus = 50f;

    [SerializeField] [Range(0f, 5f)] [Tooltip("自伤惩罚系数：SelfHpDrain 造成的自损 × 此系数作为负分。越高则越避免自伤技能。")]
    private float selfDamagePenalty = 1.5f;

    [Header("生存倾向")]
    [SerializeField] [Range(0f, 1f)] [Tooltip("低血量阈值（百分比）。当目标或施法者当前 HP / 最大 HP 低于此值时触发生存奖励倍率。")]
    private float survivalThreshold = 0.3f;

    [SerializeField] [Range(1f, 5f)] [Tooltip("低血量生存额外倍率。HP 低于 SurvivalThreshold 时，治疗/防御 Buff 等保命效果的评分乘以本系数。")]
    private float survivalMultiplier = 2.0f;

    [Header("AOE 倾向")]
    [SerializeField] [Range(0.5f, 2f)] [Tooltip("群体技能（All/AllEnemies/AllAllies）的评分衰减/放大系数。小于 1 降低 AOE 偏好，大于 1 提高。")]
    private float aoeMultiplier = 0.8f;

    [Header("阈值")]
    [SerializeField] [Range(0f, 50f)] [Tooltip("最小行动分阈值。某敌方候选行动的总评分低于此值时视为无可用行动，该单位将直接跳过。")]
    private float minActionScore = 1f;

    #endregion

    #region Properties

    /// <summary>
    /// 即时伤害效果的评分权重。越高则敌方越倾向使用伤害类技能。
    /// </summary>
    public float DamageWeight => Mathf.Max(0f, damageWeight);

    /// <summary>
    /// 治疗效果的评分权重。越高则敌方越倾向治疗受伤的友军。
    /// </summary>
    public float HealWeight => Mathf.Max(0f, healWeight);

    /// <summary>
    /// 防御 Buff 的评分权重。越高则敌方越倾向施加防御增益。
    /// </summary>
    public float DefenseWeight => Mathf.Max(0f, defenseWeight);

    /// <summary>
    /// 刷新冷却效果的评分权重。仅当该敌方有技能处于冷却中时此权重才生效。
    /// </summary>
    public float RefreshCooldownWeight => Mathf.Max(0f, refreshCooldownWeight);

    /// <summary>
    /// 击杀奖励分数。当一次伤害足以击杀目标时额外累加此分，鼓励集火残血单位。
    /// </summary>
    public float KillBonus => Mathf.Max(0f, killBonus);

    /// <summary>
    /// 自伤惩罚系数。自损 HP × 此系数作为负分累加到总评分中，值越大越避免自伤。
    /// </summary>
    public float SelfDamagePenalty => Mathf.Max(0f, selfDamagePenalty);

    /// <summary>
    /// 低血量阈值（0～1）。当实体当前 HP 比例低于此值时触发生存奖励倍率。
    /// </summary>
    public float SurvivalThreshold => Mathf.Clamp01(survivalThreshold);

    /// <summary>
    /// 生存奖励倍率。HP 低于 <see cref="SurvivalThreshold"/> 时，保命类效果（治疗、防御 Buff）评分乘以本系数。
    /// </summary>
    public float SurvivalMultiplier => Mathf.Max(1f, survivalMultiplier);

    /// <summary>
    /// 群体技能评分系数。作用于 All / AllEnemies / AllAllies 类技能的总分缩放。
    /// </summary>
    public float AoeMultiplier => Mathf.Max(0f, aoeMultiplier);

    /// <summary>
    /// 最小行动分阈值。低于此分的候选行动将被过滤，对应敌方标记为无可用行动。
    /// </summary>
    public float MinActionScore => Mathf.Max(0f, minActionScore);

    #endregion
}
