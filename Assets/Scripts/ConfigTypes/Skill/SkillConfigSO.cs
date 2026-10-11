using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Skill/Skill Config", fileName = "SkillConfig_")]
public class SkillConfigSO : ScriptableObject, IConfigData
{
    [SerializeField] private string _skillId;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private SkillTargetKind _targetKind;
    [SerializeField] private int _cooldown;
    [Tooltip("本场可用次数上限（-1 无限制，>0 每场 N 次）")]
    [SerializeField] private int _useLimit = -1;
    [SerializeField] private int _iconResourceId;
    [Tooltip("效果配置列表：按序执行，全部成功才结算，失败逆序回滚")]
    [SerializeField] private List<SkillEffectConfig> _effects = new List<SkillEffectConfig>();

    public string Id => SkillId;
    public string SkillId => _skillId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public SkillTargetKind TargetKind => _targetKind;
    public int Cooldown => _cooldown;
    public int UseLimit => _useLimit;
    public int IconResourceId => _iconResourceId;
    public IReadOnlyList<SkillEffectConfig> Effects => _effects;

    public SkillConfig ToDomain() => new SkillConfig
    {
        SkillId = _skillId,
        DisplayName = _displayName,
        Description = _description,
        TargetKind = _targetKind,
        Cooldown = _cooldown,
        UseLimit = _useLimit,
        IconResourceId = _iconResourceId,
        Effects = new List<SkillEffectConfig>(_effects)
    };
}




