using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 负责存档的读写
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    private string SaveFileName = "";

    // 依赖 AccountManager提供当前账号
    public const int SaveSlotCount = 3;
    private int currentSlotIndex = 0;

    private string GetSavePath(int? _slotIndex = null)
    {
        int slot = _slotIndex ?? currentSlotIndex;
        string accountId = "default";  // 未登录账号时用 default
        var accountManager = FindObjectOfType<AccountManager>();
        if (accountManager != null && accountManager.IsLoggedIn())
        {
            accountId = accountManager.GetCurrentAccountId();
        }

        SaveFileName = $"Save_{accountId}_{slot}.json";
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    /// <summary> 当前运行时存档数据，Load后或默认数据 </summary>
    private SaveData current;

    private string SavePath => GetSavePath(currentSlotIndex);

    private void Awake()
    {
        Load();
    }

    /// <summary> 
    /// 从磁盘读取数据(以后要改-------------),若无数据则创建默认数据并写入
    /// </summary>
    public void Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                current = JsonUtility.FromJson<SaveData>(json);
                if (current == null)
                {
                    Debug.LogWarning($"[SaveManager] 当前路径没有存档文件 => {SavePath}");
                    current = new SaveData();
                }
                if (current.inventory == null)
                {
                    Debug.LogWarning($"[SaveManager] 当前存档中没有任何卡牌!");
                    current.inventory = new System.Collections.Generic.List<CardInstance>();
                }
                if (current.decks == null)
                {
                    Debug.LogWarning($"[SaveManager] 当前没有任何卡组!");
                    current.decks = new System.Collections.Generic.List<DeckSlot>();
                }
                Debug.Log($"[SaveManager] 加载数据来自{SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 加载数据失败: {e.Message}");
                current = CreateDefaultSaveData();
                Save();
            }
        }
        else
        {
            current = CreateDefaultSaveData();
            Save();
            Debug.LogWarning($"[SaveManager] 当前路径不存在，创建默认存档");
        }
    }

    /// <summary> 
    /// 将当前数据存入磁盘(以后修改-------------------)
    /// </summary>
    public void Save()
    {
        if (current == null) current = CreateDefaultSaveData();
        try
        {
            string json = JsonUtility.ToJson(current, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] 存档到路径 => {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 存档失败 : {e.Message}");
        }
    }

    /// <summary> 
    /// 添加一张卡到背包并持久化
    /// </summary>
    public void AddCardToInventory(CardInstance _instance)
    {
        if (_instance == null)
        {
            Debug.LogWarning($"[SaveManager] 没有找到该卡牌，尝试加载数据");
            Load();
        }
        if (current.inventory == null)
        {
            Debug.LogWarning("[SaveManager] 该存档背包里当前没有任何卡牌，添加该卡牌");
            current.inventory = new System.Collections.Generic.List<CardInstance>();
            current.inventory.Add(_instance);
            Save();
        }
    }

    /// <summary> 
    /// 根据instanceId在背包中查找卡牌
    /// </summary>
    public CardInstance FindInstanceById(string _id)
    {
        if (current?.inventory == null)
        {
            Debug.LogWarning($"[SaveManager] 该存档没有任何卡牌");
            return null;
        }
        foreach (var _instance in current.inventory)
        {
            if (_instance.instanceId == _id)
            {
                return _instance;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取当前卡组，(按deckInstanceId 从 inventory 中获取 cardInstance 列表)
    /// </summary>
    public System.Collections.Generic.List<CardInstance> GetCurrentDeck()
    {
        var deckList = new List<CardInstance>();
        if (current?.inventory == null || current.decks == null)
        {
            Debug.LogWarning($"[SaveManager] 当前卡组为空或者背包里没有任何卡牌!");
            return deckList;
        }
        if (deckList.Count == 0)
        {
            current.decks.Add(new DeckSlot("默认卡组"));
            Save();
        }

        int idx = Mathf.Clamp(current.currentDeckIndex, 0, current.decks.Count - 1);
        var slot = current.decks[idx];
        if (slot?.instanceIds == null) return deckList;

        foreach (var _id in slot.instanceIds)
        {
            var instance = FindInstanceById(_id);
            if (instance != null) deckList.Add(instance);
        }
        return deckList;
    }

    /// <summary>
    /// 切换当前卡组,index 为 decks 下标
    /// </summary>
    public void SetCurrentDeckIndex(int index)
    {
        if (current?.decks == null || index < 0 || index >= current.decks.Count)
        {
            Debug.LogWarning($"[SaveManager] 当前序号 {index} 不符合卡组序号标准 或 所选卡组为空");
            return;
        }
        current.currentDeckIndex = index;
        Save();
    }

    /// <summary>
    /// 新增一套卡组
    /// </summary>
    public void AddDeck(string _deckName)
    {
        if (current.decks == null) current.decks = new List<DeckSlot>();
        current.decks.Add(new DeckSlot(_deckName));
        Save();
    }

    /// <summary>
    /// 删除卡组（若删除的是当前选中的 ，则设currentIndex 为0）
    /// </summary>
    public void RemoveDeckAt(int _index)
    {
        if (current?.decks == null || _index < 0 || _index >= current.decks.Count)
        {
            Debug.LogWarning($"[SaveManager] 当前序号不符合标准或者卡组为空");
            return;
        }
        current.decks.RemoveAt(_index);
        if (current.currentDeckIndex >= current.decks.Count)
        {
            current.currentDeckIndex = Mathf.Max(0, current.decks.Count - 1);
        }
        Save();
    }

    /// <summary>
    /// 设置某套卡组的卡牌 ID 列表（编辑卡组时用）
    /// </summary>
    public void SetDeckInstanceIds(int _deckIndex, List<string> _instanceIds)
    {
        if (current?.decks == null || _deckIndex < 0 || _deckIndex >= current.decks.Count)
        {
            Debug.LogWarning("[SaveManager] 当前没有任何卡组或者序号不符合标准!");
            return;
        }
        current.decks[_deckIndex].instanceIds = _instanceIds;
        Save();

    }

    private static SaveData CreateDefaultSaveData()
    {
        var data = new SaveData();
        data.inventory = new System.Collections.Generic.List<CardInstance>();
        data.decks = new System.Collections.Generic.List<DeckSlot>();
        return data;
    }

    /// <summary>
    /// 选择要读写的存档槽位然后加载数据, 0 - SaveSlotCount - 1
    /// </summary>
    public void SelectSlot(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= SaveSlotCount)
        {
            Debug.LogWarning($"[SaveManager] 当前序号 {_slotIndex} 不符合标准");
            return;
        }
        currentSlotIndex = _slotIndex;
        Load();
    }

    /// <summary>
    /// 检查指定的槽位有没有存档
    /// </summary>
    public bool HasSaveInSlot(int _index)
    {
        if (_index < 0 || _index >= SaveSlotCount)
        {
            Debug.LogWarning($"[SaveManager] 当前序号 {_index} 不符合标准!");
            return false;
        }
        string path = GetSavePath(_index);
        return File.Exists(path);
    }


    /// <summary>
    /// 当前账号下是否有存档
    /// </summary>
    public bool[] GetSlotOccupied()
    {
        var result = new bool[SaveSlotCount];
        for (int i = 0; i < SaveSlotCount; i++)
        {
            result[i] = HasSaveInSlot(i);
        }
        return result;
    }

    public int GetCurrentSlotIndex() => currentSlotIndex;

    /// <summary>
    /// 获取当前账号下的存档数量
    /// </summary>
    public int GetCurrentAccountSaveCount()
    {
        int count = 0;
        for (int i = 0; i < SaveSlotCount; i++)
        {
            if (HasSaveInSlot(i))
                count++;
        }
        return count;
    }

    /// <summary>
    /// 创建默认存档，（如果当前位置没有存档）
    /// </summary>
    public void CreateDefaultSaveInSlot(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= SaveSlotCount)
        {
            Debug.LogWarning($"{_slotIndex} 当前索引不符合标准!");
            return;
        }
        currentSlotIndex = _slotIndex;
        Load();
    }
}
