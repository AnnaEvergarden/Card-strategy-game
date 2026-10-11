using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面板配置 SOAsset —— 映射 PanelId → 预制体资源 → 层级 → 缓存策略。
/// 通过 CreateAssetMenu → CardGame/Config/Panel/Panel Config 创建。
/// </summary>
[CreateAssetMenu(menuName = "CardGame/Config/Panel/Panel Config", fileName = "PanelConfig_")]
public class PanelConfigSO : ScriptableObject, IConfigData
{
    [Header("面板信息")]
    [SerializeField] private string _panelId;
    [SerializeField] private int _resourceId;
    [SerializeField] private int _layer = 1;
    [SerializeField] private bool _cacheOnClose = true;

    public string Id => _panelId;
    public string PanelId => _panelId;
    public int ResourceId => _resourceId;
    public int Layer => _layer;
    public bool CacheOnClose => _cacheOnClose;

    public PanelConfig ToDomain() => new PanelConfig
    {
        PanelId = _panelId,
        ResourceId = _resourceId,
        Layer = _layer,
        CacheOnClose = _cacheOnClose,
    };
}


