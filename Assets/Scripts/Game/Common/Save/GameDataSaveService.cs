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
        if (!UserDataPathService.HasCurrentUser())
        {
            Debug.Log("Game data save skipped: no logged-in user.");
            return;
        }

        // 账号数据在每次操作时已落盘，这里确认当前账号存在后再保存玩家进度。
        CurrencyStore.SaveCurrent();
        InventoryStore.SaveCurrent();
        CardCollectionStore.SaveCurrent();
        FleetStore.SaveCurrent();
        Debug.Log("Game data saved before quit.");
    }

    #endregion
}

