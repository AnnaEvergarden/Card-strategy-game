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
        // 账号数据在每次操作时已落盘；未登录时不能写入进度，避免生成匿名共享档案。
        var currentUser = AccountStore.GetCurrentUser();
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.Log("No logged-in account, skip game data save.");
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

