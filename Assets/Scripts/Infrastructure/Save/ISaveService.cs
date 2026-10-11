using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 存档服务接口：提供键值对形式的本地数据持久化能力。
/// 内部处理序列化、加密与文件写入，业务层只需提供可序列化的数据对象。
/// </summary>
public interface ISaveService
{
    /// <summary>
    /// 异步保存数据。
    /// </summary>
    /// <typeparam name="T">数据类型（建议为可序列化的 Pure C# 类）</typeparam>
    /// <param name="key">存储键（如 "account"、"currency"）</param>
    /// <param name="data">要保存的数据对象</param>
    UniTask SaveAsync<T>(string key, T data) where T : class;

    /// <summary>
    /// 异步加载数据；不存在时返回指定类型默认值。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>加载的数据对象</returns>
    UniTask<T> LoadAsync<T>(string key) where T : class, new();

    /// <summary>
    /// 检查指定键的数据是否存在。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>是否存在</returns>
    bool HasKey(string key);

    /// <summary>
    /// 删除指定键的数据文件。
    /// </summary>
    /// <param name="key">存储键</param>
    UniTask DeleteAsync(string key);
}
