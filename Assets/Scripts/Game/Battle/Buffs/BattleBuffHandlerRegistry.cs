using System.Collections.Generic;

/// <summary>
/// Buff 种类 → 处理器映射表；<see cref="SkillBuffKind"/> 扩展时在此注册。
/// </summary>
public static class BattleBuffHandlerRegistry
{
    #region Fields

    /// <summary>
    /// 已注册的处理器。
    /// </summary>
    private static readonly Dictionary<SkillBuffKind, IBattleBuffHandler> Handlers = new();

    /// <summary>
    /// 是否已完成默认注册。
    /// </summary>
    private static bool _defaultsRegistered;

    #endregion

    #region Public API

    /// <summary>
    /// 注册一种 Buff 处理器（重复注册同 Kind 会覆盖）。
    /// </summary>
    public static void Register(SkillBuffKind kind, IBattleBuffHandler handler)
    {
        if (kind == SkillBuffKind.None || handler == null)
        {
            return;
        }

        Handlers[kind] = handler;
    }

    /// <summary>
    /// 确保内置 Buff 处理器已注册。
    /// </summary>
    public static void EnsureDefaultsRegistered()
    {
        if (_defaultsRegistered)
        {
            return;
        }

        Register(SkillBuffKind.DefenseBuff, new DefenseBuffHandler());
        _defaultsRegistered = true;
    }

    /// <summary>
    /// Buff 施加时调用对应处理器。
    /// </summary>
    public static void OnApplied(string cardId, BattleBuffState.RuntimeBuff buff)
    {
        EnsureDefaultsRegistered();
        if (buff == null || !TryGetHandler(buff.Kind, out var handler))
        {
            return;
        }

        handler.OnApplied(cardId, buff);
    }

    /// <summary>
    /// Buff 到期移除时调用对应处理器。
    /// </summary>
    public static void OnExpired(string cardId, BattleBuffState.RuntimeBuff buff)
    {
        EnsureDefaultsRegistered();
        if (buff == null || !TryGetHandler(buff.Kind, out var handler))
        {
            return;
        }

        handler.OnExpired(cardId, buff);
    }

    /// <summary>
    /// 清空注册（仅用于测试或域重置；正常战斗依赖 EnsureDefaultsRegistered）。
    /// </summary>
    public static void ClearForTests()
    {
        Handlers.Clear();
        _defaultsRegistered = false;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 查找处理器。
    /// </summary>
    private static bool TryGetHandler(SkillBuffKind kind, out IBattleBuffHandler handler)
    {
        handler = null;
        return kind != SkillBuffKind.None && Handlers.TryGetValue(kind, out handler) && handler != null;
    }

    #endregion
}
