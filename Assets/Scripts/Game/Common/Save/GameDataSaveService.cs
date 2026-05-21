using Game.Common.Auth;
using UnityEngine;

/// <summary>
/// 游戏数据保存服务：统一处理退出时的自动保存。
/// </summary>
public static class GameDataSaveService
{
    #region Public API

    /// <summary>
    /// 保存所有关键本地数据。
    /// </summary>
    public static void SaveAll()
    {
        // 账号数据在每次操作时已落盘；未登录时跳过玩家进度，避免旧缓存覆盖共享默认档。
        var currentUser = AccountStore.GetCurrentUser();
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.Log("Game data save skipped: 当前没有登录账号。");
            return;
        }

        CurrencyStore.SaveCurrent();
        InventoryStore.SaveCurrent();
        CardCollectionStore.SaveCurrent();
        FleetStore.SaveCurrent();
        Debug.Log("Game data saved before quit.");
    }

    #endregion
}

