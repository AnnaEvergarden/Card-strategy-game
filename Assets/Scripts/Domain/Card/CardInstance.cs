/// <summary>
/// 卡牌实例：玩家实际拥有的卡牌运行时数据。
/// 与 CardConfig 分离，包含个体状态（等级、经验等）。
/// </summary>
[System.Serializable]
public class CardInstance
{
    /// <summary>实例唯一标识。</summary>
    public string InstanceId;

    /// <summary>对应的 CardConfig.CardId。</summary>
    public string CardId;

    /// <summary>当前等级。</summary>
    public int Level = 1;

    /// <summary>当前经验值。</summary>
    public int Experience;
}
