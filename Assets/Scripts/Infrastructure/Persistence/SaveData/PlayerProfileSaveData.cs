/// <summary>
/// 玩家资料存档 DTO。只用于序列化，无行为。
/// </summary>
[System.Serializable]
public class PlayerProfileSaveData
{
    /// <summary>玩家名称。</summary>
    public string PlayerName = "指挥官";

    /// <summary>出生年份。</summary>
    public int BirthYear;

    /// <summary>出生月份。</summary>
    public int BirthMonth;

    /// <summary>出生日。</summary>
    public int BirthDay;

    /// <summary>玩家等级。</summary>
    public int Level = 1;

    /// <summary>当前经验。</summary>
    public int Experience;
}
