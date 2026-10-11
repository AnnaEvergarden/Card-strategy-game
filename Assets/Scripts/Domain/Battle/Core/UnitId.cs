/// <summary>
/// UnitId 工具：战斗内单位的唯一标识生成与解析。
/// 格式为 "P1_0" ~ "P1_5" / "P2_0" ~ "P2_N"（阵营_编队序号），
/// 取代旧的 cardId + Side 双参数传递模式——同名卡牌在双方场上互不干扰。
/// </summary>
public static class UnitId
{
    /// <summary>
    /// 生成 UnitId 字符串。
    /// </summary>
    /// <param name="side">所属阵营</param>
    /// <param name="index">编队内序号（0 起始）</param>
    public static string Build(BattleSide side, int index)
    {
        return $"{side}_{index}";
    }

    /// <summary>
    /// 解析 UnitId 字符串为阵营与序号。
    /// </summary>
    /// <param name="unitId">UnitId 字符串</param>
    /// <param name="side">输出：阵营</param>
    /// <param name="index">输出：编队内序号</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParse(string unitId, out BattleSide side, out int index)
    {
        side = default;
        index = -1;

        if (string.IsNullOrEmpty(unitId))
            return false;

        var separatorIndex = unitId.IndexOf('_');
        if (separatorIndex <= 0 || separatorIndex == unitId.Length - 1)
            return false;

        var sideText = unitId.Substring(0, separatorIndex);
        var indexText = unitId.Substring(separatorIndex + 1);

        if (!int.TryParse(indexText, out index) || index < 0)
            return false;

        switch (sideText)
        {
            case "P1": side = BattleSide.P1; return true;
            case "P2": side = BattleSide.P2; return true;
            default: return false;
        }
    }
}
