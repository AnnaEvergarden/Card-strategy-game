using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 音频服务实现（当前为占位实现，后续接入 Addressables 音频资源后扩展）。
/// 提供完整的接口方法签名，保证业务层可正常编译调用。
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private bool _initialized;
    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;

    /// <summary>
    /// 初始化音频系统。
    /// </summary>
    public UniTask InitializeAsync()
    {
        if (_initialized) return UniTask.CompletedTask;
        _initialized = true;
        Debug.Log("[AudioService] 音频系统初始化完成（占位实现）");
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 播放背景音乐。
    /// </summary>
    public void PlayBGM(string audioId)
    {
        if (!_initialized) return;
        Debug.Log($"[AudioService] PlayBGM: {audioId}");
    }

    /// <summary>
    /// 播放音效。
    /// </summary>
    public void PlaySFX(string audioId)
    {
        if (!_initialized) return;
        Debug.Log($"[AudioService] PlaySFX: {audioId}");
    }

    /// <summary>
    /// 停止当前背景音乐。
    /// </summary>
    public void StopBGM()
    {
        Debug.Log("[AudioService] StopBGM");
    }

    /// <summary>
    /// 暂停所有音频。
    /// </summary>
    public void PauseAll()
    {
        Debug.Log("[AudioService] PauseAll");
    }

    /// <summary>
    /// 恢复所有音频。
    /// </summary>
    public void ResumeAll()
    {
        Debug.Log("[AudioService] ResumeAll");
    }

    /// <summary>
    /// 设置主音量。
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        Debug.Log($"[AudioService] SetMasterVolume: {_masterVolume}");
    }

    /// <summary>
    /// 设置背景音乐音量。
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyVolume();
    }

    /// <summary>
    /// 设置音效音量。
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        ApplyVolume();
    }

    /// <summary>
    /// 释放资源。
    /// </summary>
    public void Dispose()
    {
        _initialized = false;
    }

    private void ApplyVolume()
    {
        // 占位：后续接入 AudioMixer 后实现实际音量控制
    }
}
