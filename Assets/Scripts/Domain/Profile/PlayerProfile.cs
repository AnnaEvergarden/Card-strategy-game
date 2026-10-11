using System;

/// <summary>
/// 玩家资料聚合根：名称、生日、等级与经验。
/// 纯领域对象（不依赖 UnityEngine），所有字段只能经自身的校验方法修改，保证数据始终合法。
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>名称最大长度。</summary>
    public const int MaxNameLength = 12;

    /// <summary>允许的最早出生年份。</summary>
    public const int MinBirthYear = 1900;

    /// <summary>玩家名称。</summary>
    public string PlayerName { get; private set; } = "指挥官";

    /// <summary>出生年份（0 表示未设置）。</summary>
    public int BirthYear { get; private set; }

    /// <summary>出生月份（0 表示未设置）。</summary>
    public int BirthMonth { get; private set; }

    /// <summary>出生日（0 表示未设置）。</summary>
    public int BirthDay { get; private set; }

    /// <summary>玩家等级。</summary>
    public int Level { get; private set; } = 1;

    /// <summary>当前经验值。</summary>
    public int Experience { get; private set; }

    /// <summary>是否已设置生日。</summary>
    public bool HasBirthday => BirthYear > 0 && BirthMonth > 0 && BirthDay > 0;

    /// <summary>生日显示文本（未设置时给出提示）。</summary>
    public string BirthdayText => HasBirthday ? $"{BirthYear}-{BirthMonth:D2}-{BirthDay:D2}" : "未设置";

    /// <summary>空构造：用于反序列化/默认资料。</summary>
    public PlayerProfile()
    {
    }

    /// <summary>
    /// 构造玩家资料（用于存档映射）。
    /// </summary>
    public PlayerProfile(string name, int year, int month, int day, int level, int experience)
    {
        PlayerName = string.IsNullOrWhiteSpace(name) ? "指挥官" : name.Trim();
        BirthYear = year < 0 ? 0 : year;
        BirthMonth = month < 0 ? 0 : month;
        BirthDay = day < 0 ? 0 : day;
        Level = level > 0 ? level : 1;
        Experience = experience < 0 ? 0 : experience;
    }

    /// <summary>
    /// 尝试修改名称：去空格后不能为空、长度不超过 <see cref="MaxNameLength"/>。
    /// </summary>
    public bool TrySetName(string name, out string error)
    {
        error = string.Empty;
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            error = "名称不能为空";
            return false;
        }

        if (trimmed.Length > MaxNameLength)
        {
            error = $"名称不能超过 {MaxNameLength} 个字符";
            return false;
        }

        PlayerName = trimmed;
        return true;
    }

    /// <summary>
    /// 尝试修改生日：
    /// 年份为纯数字且不小于 <see cref="MinBirthYear"/>、不大于当前年份；
    /// 月份 1~12；日期 1~该月实际天数（自动处理 30/31 天与闰年 2 月 28/29 天）。
    /// </summary>
    public bool TrySetBirthday(int year, int month, int day, out string error)
    {
        error = string.Empty;
        var currentYear = DateTime.Now.Year;

        if (year < MinBirthYear || year > currentYear)
        {
            error = $"年份需在 {MinBirthYear}~{currentYear} 之间";
            return false;
        }

        if (month < 1 || month > 12)
        {
            error = "月份需为 1~12";
            return false;
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        if (day < 1 || day > daysInMonth)
        {
            error = $"{year} 年 {month} 月只有 {daysInMonth} 天";
            return false;
        }

        BirthYear = year;
        BirthMonth = month;
        BirthDay = day;
        return true;
    }

    /// <summary>
    /// 设置等级与经验（供成长/结算逻辑调用）。
    /// </summary>
    public void SetLevel(int level, int experience)
    {
        Level = level > 0 ? level : 1;
        Experience = experience < 0 ? 0 : experience;
    }
}
