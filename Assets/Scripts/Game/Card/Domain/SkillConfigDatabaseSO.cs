using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能配置表：按 skillId 查询 <see cref="SkillConfigSO"/>；运行时由 <see cref="GameResourceLoader"/> 加载。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Skill Config Database", fileName = "SkillConfigDatabase_EagleUnion")]
public sealed class SkillConfigDatabaseSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 全部技能配置条目。
    /// </summary>
    [SerializeField] private List<SkillConfigSO> skills = new();

    /// <summary>
    /// 本数据库侧重阵营（策划标注，便于分资产维护）；运行时筛选见 <see cref="CopySkillsMatchingFaction"/>。
    /// </summary>
    [SerializeField] private ShipFaction databaseFaction = ShipFaction.Other;

    /// <summary>
    /// skillId（规范化）到配置的映射缓存。
    /// </summary>
    [NonSerialized]
    private Dictionary<string, SkillConfigSO> _map;

    #endregion

    #region Public API

    /// <summary>
    /// 本库标注阵营。
    /// </summary>
    public ShipFaction DatabaseFaction => databaseFaction;

    /// <summary>
    /// 只读技能列表。
    /// </summary>
    public IReadOnlyList<SkillConfigSO> Skills => skills;

    /// <summary>
    /// 按 skillId 查找配置（忽略首尾空白；找不到则返回 false）。
    /// </summary>
    /// <param name="skillId">技能 ID。</param>
    /// <param name="config">输出配置。</param>
    /// <returns>是否找到。</returns>
    public bool TryGet(string skillId, out SkillConfigSO config)
    {
        config = null;
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        EnsureMap();
        return _map.TryGetValue(skillId.Trim(), out config) && config != null;
    }

    /// <summary>
    /// 按技能条目上的 <see cref="SkillConfigSO.ShipFaction"/> 收集技能。
    /// </summary>
    /// <param name="faction">目标阵营。</param>
    /// <param name="destination">输出列表（先 Clear）。</param>
    /// <param name="restrictToDatabaseScope">为 true 时：若 <see cref="DatabaseFaction"/> 非 <see cref="ShipFaction.Other"/>，则仅当与 <paramref name="faction"/> 一致时才收录（用于「单阵营分表」资产）。</param>
    public void CopySkillsMatchingFaction(
        ShipFaction faction,
        List<SkillConfigSO> destination,
        bool restrictToDatabaseScope = false)
    {
        destination?.Clear();
        if (destination == null || skills == null)
        {
            return;
        }

        if (restrictToDatabaseScope &&
            databaseFaction != ShipFaction.Other &&
            databaseFaction != faction)
        {
            return;
        }

        for (var i = 0; i < skills.Count; i++)
        {
            var s = skills[i];
            if (s == null || s.ShipFaction != faction)
            {
                continue;
            }

            destination.Add(s);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 由序列化列表构建字典缓存。
    /// </summary>
    private void EnsureMap()
    {
        if (_map != null)
        {
            return;
        }

        _map = new Dictionary<string, SkillConfigSO>(StringComparer.Ordinal);
        if (skills == null)
        {
            return;
        }

        for (var i = 0; i < skills.Count; i++)
        {
            var s = skills[i];
            if (s == null || string.IsNullOrWhiteSpace(s.SkillId))
            {
                continue;
            }

            var key = s.SkillId.Trim();
            if (_map.ContainsKey(key) && _map[key] != s)
            {
                Debug.LogError(
                    $"SkillConfigDatabase: 重复 SkillId「{key}」— 保留 {_map[key].name}，忽略 {s.name}",
                    s);
                continue;
            }

            _map[key] = s;
        }
    }

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：刷新字典语义并同步数据库文件名。
    /// </summary>
    private void OnValidate()
    {
        _map = null;
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, $"SkillConfigDatabase_{databaseFaction}");
    }
#endif
}
