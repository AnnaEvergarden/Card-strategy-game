/// <summary>
/// 配置数据接口：所有业务配置表数据类需实现此接口，以便 ConfigService 统一管理。
/// </summary>
public interface IConfigData
{
    /// <summary>
    /// 配置唯一标识。
    /// </summary>
    string Id { get; }
}
