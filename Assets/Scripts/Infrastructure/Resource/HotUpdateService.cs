using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 热更新服务：封装 Addressables 目录检查、资源下载、超时容错与失败续传。
/// 失败时保留待更新目录并持久化，下次启动继续尝试。
/// </summary>
public class HotUpdateService : IHotUpdateService, IDisposable
{
    /// <summary>待更新目录持久化键（PlayerPrefs）。</summary>
    private const string PendingCatalogsKey = "HotUpdate.PendingCatalogs";

    /// <summary>目录检查超时（秒）。</summary>
    private const float CatalogCheckTimeoutSeconds = 5f;

    /// <summary>下载进度轮询间隔（秒）。</summary>
    private const float ProgressPollIntervalSeconds = 0.5f;

    /// <summary>下载零进展判定阈值（秒）：超过该时长进度没有变化，视为连接异常。</summary>
    private const float NoProgressStallSeconds = 30f;

    private bool _hasPendingUpdate;
    private List<string> _catalogsToUpdate = new List<string>();

    public bool HasPendingUpdate => _hasPendingUpdate;

    /// <summary>
    /// 检查 Addressables 内容更新。
    /// 成功时以服务端返回的目录集合为准并持久化；失败或超时时恢复上次未完成的目录。
    /// </summary>
    public async UniTask<IReadOnlyList<string>> CheckForCatalogUpdatesAsync()
    {
        Debug.Log("[HotUpdate] 检查 Addressables 内容更新...");
        _hasPendingUpdate = false;
        _catalogsToUpdate.Clear();

        if (!IsNetworkReachable())
        {
            Debug.LogWarning("[HotUpdate] 当前无网络连接，跳过目录检查");
            LoadPersistedCatalogs();
            return Array.Empty<string>();
        }

        try
        {
            var checkTask = Addressables.CheckForCatalogUpdates().Task.AsUniTask();
            var catalogs = await WithTimeoutAsync(checkTask, CatalogCheckTimeoutSeconds, "检查更新超时");

            if (catalogs != null && catalogs.Count > 0)
            {
                _catalogsToUpdate.AddRange(catalogs);
                _hasPendingUpdate = true;
                SavePersistedCatalogs(_catalogsToUpdate);
                Debug.Log($"[HotUpdate] 发现 {catalogs.Count} 个待更新目录");
            }
            else
            {
                // 服务端确认无需更新，清空上次失败的残留
                SavePersistedCatalogs(_catalogsToUpdate);
            }

            return _catalogsToUpdate;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HotUpdate] 检查更新失败: {ex.Message}");
            LoadPersistedCatalogs();
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// 下载并应用指定目录更新。
    /// 下载期间区分“断网/连接异常”与“慢速下载”：网络接口断开或长时间零进展判定为
    /// NetworkLost；只要进度持续推进就继续等待，不触发超时。
    /// 成功时清空待更新集合；失败时保留并持久化，供下次启动续传。
    /// </summary>
    public async UniTask<HotUpdateResult> DownloadUpdatesAsync(IReadOnlyList<string> catalogs, IProgress<float> progress)
    {
        if (catalogs == null || catalogs.Count == 0)
        {
            _hasPendingUpdate = false;
            SavePersistedCatalogs(new List<string>());
            progress?.Report(1f);
            return HotUpdateResult.Succeeded;
        }

        if (!IsNetworkReachable())
        {
            Debug.LogWarning("[HotUpdate] 当前无网络连接，暂停下载，保留待更新目录");
            KeepPendingCatalogs(catalogs);
            return HotUpdateResult.NetworkLost;
        }

        Debug.Log($"[HotUpdate] 开始下载 {catalogs.Count} 个目录更新...");
        progress?.Report(0f);

        try
        {
            var handle = Addressables.UpdateCatalogs(catalogs, autoReleaseHandle: true);
            var updateTask = handle.Task.AsUniTask();

            float lastProgress = handle.PercentComplete;
            float stallSeconds = 0f;

            while (!handle.IsDone)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Debug.LogWarning("[HotUpdate] 下载期间检测到网络断开");
                    KeepPendingCatalogs(catalogs);
                    return HotUpdateResult.NetworkLost;
                }

                float currentProgress = handle.PercentComplete;
                if (currentProgress > lastProgress + 0.001f)
                {
                    lastProgress = currentProgress;
                    stallSeconds = 0f;
                    progress?.Report(currentProgress);
                }
                else
                {
                    stallSeconds += ProgressPollIntervalSeconds;
                    if (stallSeconds >= NoProgressStallSeconds)
                    {
                        Debug.LogWarning($"[HotUpdate] 下载 {NoProgressStallSeconds:F0}s 无进展，判定连接异常");
                        KeepPendingCatalogs(catalogs);
                        return HotUpdateResult.NetworkLost;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(ProgressPollIntervalSeconds));
            }

            await updateTask;

            _catalogsToUpdate.Clear();
            _hasPendingUpdate = false;
            SavePersistedCatalogs(new List<string>());
            progress?.Report(1f);
            Debug.Log("[HotUpdate] 内容更新下载并应用完成");
            return HotUpdateResult.Succeeded;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HotUpdate] 下载更新失败: {ex.Message}");
            KeepPendingCatalogs(catalogs);
            return HotUpdateResult.Failed;
        }
    }

    public void Dispose()
    {
        _catalogsToUpdate.Clear();
        _hasPendingUpdate = false;
    }

    private static bool IsNetworkReachable()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void KeepPendingCatalogs(IReadOnlyList<string> catalogs)
    {
        _catalogsToUpdate.Clear();
        if (catalogs != null)
            _catalogsToUpdate.AddRange(catalogs);
        _hasPendingUpdate = _catalogsToUpdate.Count > 0;
        SavePersistedCatalogs(_catalogsToUpdate);
    }

    private void LoadPersistedCatalogs()
    {
        string raw = PlayerPrefs.GetString(PendingCatalogsKey, string.Empty);
        _catalogsToUpdate.Clear();
        if (string.IsNullOrWhiteSpace(raw))
        {
            _hasPendingUpdate = false;
            return;
        }

        string[] parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        _catalogsToUpdate.AddRange(parts);
        _hasPendingUpdate = _catalogsToUpdate.Count > 0;
    }

    private static void SavePersistedCatalogs(List<string> catalogs)
    {
        PlayerPrefs.SetString(PendingCatalogsKey, catalogs == null ? string.Empty : string.Join("|", catalogs));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 给任务增加整体超时：原任务与延迟任务竞争，延迟任务先完成则抛出 TimeoutException。
    /// 这是整体墙钟超时，不区分“断网无响应”与“下载过慢”，另由网络可达性预检查兜底。
    /// </summary>
    private static async UniTask<T> WithTimeoutAsync<T>(
        UniTask<T> task,
        float timeoutSeconds,
        string errorMessage)
    {
        var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds));
        var (hasResultLeft, result) = await UniTask.WhenAny(task, timeoutTask);
        if (!hasResultLeft)
            throw new TimeoutException(errorMessage);
        return result;
    }
}
