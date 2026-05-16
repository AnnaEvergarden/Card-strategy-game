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
        // 账号数据在每次操作时已落盘，这里读取一次可触发文件校验流程。
        var currentUser = AccountStore.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
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

    /// <summary>
    /// 清空所有按账号隔离的进度缓存；登录账号变化时调用，避免旧账号数据被退出自动保存写入新账号目录。
    /// </summary>
    public static void ClearCachedProgressData()
    {
        CurrencyStore.ClearCache();
        InventoryStore.ClearCache();
        CardCollectionStore.ClearCache();
        FleetStore.ClearCache();
    }

    #endregion
}

