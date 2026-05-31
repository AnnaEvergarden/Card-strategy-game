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
        // 玩家进度必须归属于明确账号，未登录时不能写入共享默认档。
        if (string.IsNullOrWhiteSpace(AccountStore.GetCurrentUser()))
        {
            Debug.LogWarning("[GameDataSaveService] 当前没有登录账号，跳过玩家进度保存。");
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

