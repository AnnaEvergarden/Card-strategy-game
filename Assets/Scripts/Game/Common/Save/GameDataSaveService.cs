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
        var currentUser = AccountStore.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            Debug.Log("[GameDataSaveService] 当前没有登录账号，跳过进度自动保存。");
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

