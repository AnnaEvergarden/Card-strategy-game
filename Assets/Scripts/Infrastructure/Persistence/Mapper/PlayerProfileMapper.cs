/// <summary>
/// PlayerProfile 领域对象 ↔ PlayerProfileSaveData DTO 映射器。纯函数，无状态。
/// </summary>
public static class PlayerProfileMapper
{
    /// <summary>
    /// 存档 DTO → 领域对象（空存档返回默认资料）。
    /// </summary>
    public static PlayerProfile ToDomain(PlayerProfileSaveData data)
    {
        if (data == null)
            return new PlayerProfile();

        return new PlayerProfile(data.PlayerName, data.BirthYear, data.BirthMonth,
            data.BirthDay, data.Level, data.Experience);
    }

    /// <summary>
    /// 领域对象 → 存档 DTO。
    /// </summary>
    public static PlayerProfileSaveData ToSaveData(PlayerProfile profile)
    {
        if (profile == null)
            return new PlayerProfileSaveData();

        return new PlayerProfileSaveData
        {
            PlayerName = profile.PlayerName,
            BirthYear = profile.BirthYear,
            BirthMonth = profile.BirthMonth,
            BirthDay = profile.BirthDay,
            Level = profile.Level,
            Experience = profile.Experience,
        };
    }
}
