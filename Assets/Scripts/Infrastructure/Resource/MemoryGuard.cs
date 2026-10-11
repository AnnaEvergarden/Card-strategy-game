using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 内存压力等级。
/// </summary>
public enum MemoryPressureLevel
{
    /// <summary>内存充足，允许加载。</summary>
    Normal = 0,

    /// <summary>内存偏高，允许加载但需要关注。</summary>
    Warning = 1,

    /// <summary>内存危险，清理后仍超限，拒绝加载。</summary>
    Critical = 2,
}

/// <summary>
/// 加载前内存压力检测：基于托管堆已用内存与预估资源大小判断是否安全加载。
/// 危险等级下先卸载未使用资源并触发 GC，仍超限则返回 Critical。
/// </summary>
public sealed class MemoryGuard
{
    /// <summary>默认警告阈值：500MB。</summary>
    public const long DefaultWarningThresholdBytes = 500L * 1024L * 1024L;

    /// <summary>默认危险阈值：800MB。</summary>
    public const long DefaultCriticalThresholdBytes = 800L * 1024L * 1024L;

    private readonly long _warningThresholdBytes;
    private readonly long _criticalThresholdBytes;

    /// <summary>
    /// 构造内存压力检测器。
    /// </summary>
    /// <param name="warningThresholdBytes">警告阈值（字节）。</param>
    /// <param name="criticalThresholdBytes">危险阈值（字节），必须大于警告阈值。</param>
    public MemoryGuard(
        long warningThresholdBytes = DefaultWarningThresholdBytes,
        long criticalThresholdBytes = DefaultCriticalThresholdBytes)
    {
        if (warningThresholdBytes <= 0 || criticalThresholdBytes <= warningThresholdBytes)
        {
            Debug.LogError(
                $"[MemoryGuard] 阈值配置无效 warning={warningThresholdBytes}, " +
                $"critical={criticalThresholdBytes}，已回退默认值");
            _warningThresholdBytes = DefaultWarningThresholdBytes;
            _criticalThresholdBytes = DefaultCriticalThresholdBytes;
        }
        else
        {
            _warningThresholdBytes = warningThresholdBytes;
            _criticalThresholdBytes = criticalThresholdBytes;
        }
    }

    /// <summary>当前托管堆已用内存（不主动触发 GC）。</summary>
    public long CurrentManagedBytes => GC.GetTotalMemory(false);

    /// <summary>
    /// 异步执行内存压力检查。
    /// </summary>
    /// <param name="estimatedSizeBytes">本次待加载资源的预估大小（字节），未知时传 0。</param>
    /// <returns>当前内存压力等级。</returns>
    public async UniTask<MemoryPressureLevel> CheckAsync(long estimatedSizeBytes = 0)
    {
        var level = Evaluate(estimatedSizeBytes);
        if (level == MemoryPressureLevel.Critical)
        {
            Debug.LogWarning(
                $"[MemoryGuard] 内存达到危险等级（当前 {ToMb(CurrentManagedBytes)}MB + " +
                $"预估 {ToMb(estimatedSizeBytes)}MB），尝试清理后复检");
            await Resources.UnloadUnusedAssets().ToUniTask();
            GC.Collect();

            level = Evaluate(estimatedSizeBytes);
            if (level == MemoryPressureLevel.Critical)
            {
                Debug.LogError(
                    $"[MemoryGuard] 清理后内存仍危险（当前 {ToMb(CurrentManagedBytes)}MB + " +
                    $"预估 {ToMb(estimatedSizeBytes)}MB），拒绝加载");
            }
            else
            {
                Debug.LogWarning("[MemoryGuard] 清理后恢复至安全水平，允许加载");
            }
        }
        else if (level == MemoryPressureLevel.Warning)
        {
            Debug.LogWarning(
                $"[MemoryGuard] 内存偏高（当前 {ToMb(CurrentManagedBytes)}MB + " +
                $"预估 {ToMb(estimatedSizeBytes)}MB），允许加载");
        }

        return level;
    }

    private MemoryPressureLevel Evaluate(long estimatedSizeBytes)
    {
        long projected = CurrentManagedBytes + Math.Max(0L, estimatedSizeBytes);
        if (projected > _criticalThresholdBytes)
            return MemoryPressureLevel.Critical;
        if (projected > _warningThresholdBytes)
            return MemoryPressureLevel.Warning;
        return MemoryPressureLevel.Normal;
    }

    private static string ToMb(long bytes) => (bytes / (1024L * 1024L)).ToString();
}
