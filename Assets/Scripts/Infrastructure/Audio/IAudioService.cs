using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 音频服务接口：管理游戏中的所有音频播放（BGM / SFX / 音量等）。
/// 具体实现由 AudioService 提供，通过 ServiceContainer 注册为全局服务。
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// 初始化音频系统（加载音频配置、创建 AudioSource 等）。
    /// </summary>
    UniTask InitializeAsync();

    /// <summary>
    /// 播放背景音乐。
    /// </summary>
    /// <param name="audioId">音频资源标识</param>
    void PlayBGM(string audioId);

    /// <summary>
    /// 播放音效。
    /// </summary>
    /// <param name="audioId">音频资源标识</param>
    void PlaySFX(string audioId);

    /// <summary>
    /// 停止当前背景音乐。
    /// </summary>
    void StopBGM();

    /// <summary>
    /// 暂停所有音频播放。
    /// </summary>
    void PauseAll();

    /// <summary>
    /// 恢复所有音频播放。
    /// </summary>
    void ResumeAll();

    /// <summary>
    /// 设置主音量（0～1）。
    /// </summary>
    void SetMasterVolume(float volume);

    /// <summary>
    /// 设置背景音乐音量（0～1）。
    /// </summary>
    void SetBGMVolume(float volume);

    /// <summary>
    /// 设置音效音量（0～1）。
    /// </summary>
    void SetSFXVolume(float volume);
}
