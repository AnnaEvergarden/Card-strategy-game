/// <summary>
/// 编队卡片预览：战斗前选卡界面展示的只读数据（编队成员的卡牌名与头像）。
/// </summary>
public sealed class FleetCardPreview
{
    /// <summary>编队内序号（0 起始）。</summary>
    public int SlotIndex { get; set; }

    /// <summary>配置表卡牌 ID。</summary>
    public string CardId { get; set; }

    /// <summary>显示名称。</summary>
    public string DisplayName { get; set; }

    /// <summary>头像资源 ID（0 = 占位图）。</summary>
    public int IconResourceId { get; set; }

    /// <summary>最大生命。</summary>
    public int MaxHp { get; set; }

    /// <summary>基础防御。</summary>
    public int BaseDefense { get; set; }
}
