/// <summary>
/// 技能静态配置数据。
/// 由 ConfigService 从导出的 JSON 加载，运行时只读。
/// 技能 = 效果配置的组合：Effects 列表按顺序执行，全部成功才结算（失败逆序回滚）。
/// </summary>
[System.Serializable]
public class SkillConfig : IConfigData
{
    public string Id => SkillId;
    public string SkillId;
    public string DisplayName;
    public string Description;
    public SkillTargetKind TargetKind;
    public int Cooldown;

    /// <summary>本场可用次数上限（-1 无限制，&gt;0 每场 N 次）。</summary>
    public int UseLimit = -1;

    /// <summary>图标资源 ID（ResourceService 寻址）。</summary>
    public int IconResourceId;

    /// <summary>效果配置列表（按序执行）。</summary>
    public System.Collections.Generic.List<SkillEffectConfig> Effects =
        new System.Collections.Generic.List<SkillEffectConfig>();
}

/// <summary>
/// 技能配置表容器。
/// </summary>
[System.Serializable]
public class SkillConfigTable
{
    public System.Collections.Generic.List<SkillConfig> Items = new System.Collections.Generic.List<SkillConfig>();
}

