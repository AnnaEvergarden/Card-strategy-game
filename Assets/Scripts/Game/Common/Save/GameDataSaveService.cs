using Game.Common.Auth;
using UnityEngine;

/// <summary>
/// 游戏数据保存服务：统一处理退出时的自动保存与账号切换时的缓存清理。
/// </summary>
public static class GameDataSaveService
{
    #region Public API

    /// <summary>
    /// 保存当前登录账号的所有关键本地数据；未登录时跳过，避免写入无归属存档。
    /// </summary>
    public static void SaveAll()
    {
        var currentUser = AccountStore.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            Debug.LogWarning("Game data save skipped: 当前没有登录账号。");
            return;
        }

        CurrencyStore.SaveCurrent();
        InventoryStore.SaveCurrent();
        CardCollectionStore.SaveCurrent();
        FleetStore.SaveCurrent();
        Debug.Log($"Game data saved before quit. user={currentUser}");
    }

    /// <summary>
    /// 清理所有账号相关运行时缓存；登录切换或退出账号后调用，防止旧账号数据串写。
    /// </summary>
    public static void ClearCachedGameData()
    {
        CurrencyStore.ClearCache();
        InventoryStore.ClearCache();
        CardCollectionStore.ClearCache();
        FleetStore.ClearCache();
    }

    #endregion
}
