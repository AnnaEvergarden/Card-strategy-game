using System;
using System.Collections.Generic;

/// <summary>
/// skillId 到 <see cref="SkillDefinition"/> 的注册表：从各阵营 <see cref="SkillConfigSO"/> 特性块构建。
/// </summary>
public static class SkillDefinitionRegistry
{
    #region Fields

    /// <summary>
    /// 已注册技能定义。
    /// </summary>
    private static readonly Dictionary<string, SkillDefinition> Definitions = new();

    /// <summary>
    /// 是否已从技能数据库完成注册。
    /// </summary>
    private static bool _defaultsRegistered;

    /// <summary>
    /// 遍历阵营加载技能配置时的缓冲。
    /// </summary>
    private static readonly List<SkillConfigSO> _skillBuffer = new(32);

    #endregion

    #region Public API

    /// <summary>
    /// 注册技能定义（同 skillId 后注册覆盖前者；Mod/热更可额外调用）。
    /// </summary>
    public static void Register(SkillDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId))
        {
            return;
        }

        Definitions[definition.SkillId.Trim()] = definition;
    }

    /// <summary>
    /// 从 <see cref="SkillConfigSO"/> 注册（无有效特性块时跳过）。
    /// </summary>
    public static void RegisterFromConfig(SkillConfigSO config)
    {
        var definition = SkillDefinitionBuilder.TryBuild(config);
        if (definition != null)
        {
            Register(definition);
        }
    }

    /// <summary>
    /// 从全部已加载技能库 SO 注册定义。
    /// </summary>
    public static void EnsureDefaultDefinitionsRegistered()
    {
        if (_defaultsRegistered)
        {
            return;
        }

        RegisterAllFromSkillDatabases();
        _defaultsRegistered = true;
    }

    /// <summary>
    /// 按 skillId 查找定义。
    /// </summary>
    public static bool TryGet(string skillId, out SkillDefinition definition)
    {
        EnsureDefaultDefinitionsRegistered();
        definition = null;
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        return Definitions.TryGetValue(skillId.Trim(), out definition) && definition != null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 遍历各阵营技能库，为有特性块的配置构建 Definition。
    /// </summary>
    private static void RegisterAllFromSkillDatabases()
    {
        Definitions.Clear();
        _skillBuffer.Clear();
        GameResourceLoader.CopyAllSkillConfigs(_skillBuffer, logOnMissing: false);

        for (var i = 0; i < _skillBuffer.Count; i++)
        {
            RegisterFromConfig(_skillBuffer[i]);
        }
    }

    #endregion
}
