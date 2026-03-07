using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 负责存档的读写
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "save.json";

    /// <summary> 当前运行时存档数据，Load后或默认数据 </summary>
    private SaveData _current;

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

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
                _current = JsonUtility.FromJson<SaveData>(json);
                if(_current == null)
                {
                    Debug.LogWarning($"当前路径没有存档文件 => {SavePath}");
                    _current = new SaveData();
                }
                if(_current.inventory == null)
                {
                    Debug.LogWarning($"当前存档中没有任何卡牌!");
                    _current.inventory = new System.Collections.Generic.List<CardInstance>();
                }
                if(_current.deckInstanceIds == null)
                {
                    Debug.LogWarning($"当前卡组没有任何卡牌!");
                    _current.deckInstanceIds = new System.Collections.Generic.List<string>();
                }
                Debug.Log($"[SaveManager] 加载数据来自{SavePath}");
            }
            catch(System.Exception e)
            {
                Debug.LogError($"加载数据失败: {e.Message}");
                _current = CreateDefaultSaveData();
                Save();
            }
        }
        else
        {
            _current = CreateDefaultSaveData();
            Save();
            Debug.LogWarning($"[SaveManager] 当前路径不存在，创建默认存档");
        }
    }

    /// <summary> 
    /// 将当前数据存入磁盘(以后修改-------------------)
    /// </summary>
    public void Save()
    {
        if(_current == null) _current = CreateDefaultSaveData();
        try
        {
            string json = JsonUtility.ToJson(_current, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"存档到路径 => {SavePath}");
        }catch(System.Exception e)
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
        if(_current.inventory == null)
        {
            Debug.LogWarning("[SaveManager] 该存档背包里当前没有任何卡牌，添加该卡牌");
            _current.inventory = new System.Collections.Generic.List<CardInstance>();
            _current.inventory.Add( _instance);
            Save();
        }
    }

    /// <summary> 
    /// 根据instanceId在背包中查找卡牌
    /// </summary>
    public CardInstance FindInstanceById(string _id)
    {
        if (_current?.inventory == null)
        {
            Debug.LogWarning($"改存档没有任何卡牌");
            return null;
        }
        foreach(var _instance in _current.inventory)
        {
            if(_instance.instanceId == _id)
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
        var deck = new System.Collections.Generic.List<CardInstance>();
        if(_current?.deckInstanceIds == null || _current.inventory == null)
        {
            Debug.LogWarning($"当前卡组为空或者背包里没有任何卡牌!");
            return deck;
        }
        foreach (var _id in _current.deckInstanceIds)
        {
            var instance = FindInstanceById(_id);
            if(instance !=null) deck.Add(instance);
        }
        return deck;
    }

    private static SaveData CreateDefaultSaveData()
    {
        var data = new SaveData();
        data.inventory = new System.Collections.Generic.List<CardInstance>();
        data.deckInstanceIds = new System.Collections.Generic.List<string>();
        return data;
    }
}
