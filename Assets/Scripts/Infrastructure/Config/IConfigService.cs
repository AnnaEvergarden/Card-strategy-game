using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 配置服务接口：管理所有配置数据的加载、查询与热重载。
/// 运行时从 Addressables 加载预导出的 JSON 配置，解析为内存表供查询。
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 初始化配置服务：加载所有配置表到内存并在内部建立索引。
    /// </summary>
    UniTask InitializeAsync(IProgress<float> progress = null);

    /// <summary>
    /// 按 ID 获取单条配置。
    /// </summary>
    /// <typeparam name="T">配置数据类型，需实现 IConfigData 接口</typeparam>
    /// <param name="id">配置唯一标识</param>
    /// <returns>配置数据；不存在时返回 null</returns>
    T Get<T>(string id) where T : class, IConfigData;

    /// <summary>
    /// 获取指定类型的全部配置列表。
    /// </summary>
    /// <typeparam name="T">配置数据类型</typeparam>
    /// <returns>只读配置列表</returns>
    IReadOnlyList<T> GetAll<T>() where T : class, IConfigData;

    /// <summary>
    /// 热重载指定类型的配置表（用于热更新场景）。
    /// </summary>
    /// <typeparam name="T">配置数据类型</typeparam>
    UniTask ReloadAsync<T>() where T : class, IConfigData;
}
