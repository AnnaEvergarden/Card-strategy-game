using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 热更新结果。
/// </summary>
public enum HotUpdateResult
{
    /// <summary>下载并应用成功。</summary>
    Succeeded = 0,

    /// <summary>检测到网络断开或长时间无进展，视为连接异常。</summary>
    NetworkLost = 1,

    /// <summary>其他异常（服务器错误、下载失败等）。</summary>
    Failed = 2,
}

/// <summary>
/// 热更新服务接口：封装 Addressables 目录检查与资源下载逻辑。
/// </summary>
public interface IHotUpdateService
{
    /// <summary>检查是否有可用的内容更新，返回需要更新的目录标识列表。</summary>
    UniTask<IReadOnlyList<string>> CheckForCatalogUpdatesAsync();

    /// <summary>下载并应用指定目录列表的更新。</summary>
    /// <param name="catalogs">需要更新的目录标识列表</param>
    /// <param name="progress">下载进度（0.0～1.0）</param>
    /// <returns>下载结果；失败时保留待更新目录，供下次启动续传。</returns>
    UniTask<HotUpdateResult> DownloadUpdatesAsync(IReadOnlyList<string> catalogs, IProgress<float> progress);

    /// <summary>是否有待应用的更新。</summary>
    bool HasPendingUpdate { get; }
}
