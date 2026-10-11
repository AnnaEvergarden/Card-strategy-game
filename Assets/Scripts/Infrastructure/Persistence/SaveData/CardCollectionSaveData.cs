using System.Collections.Generic;

/// <summary>
/// 卡牌收藏存档 DTO。只用于序列化，与 Domain/Save/CardCollectionData.cs 结构一致。
/// </summary>
[System.Serializable]
public class CardCollectionSaveData
{
    public List<CardInstance> Cards = new List<CardInstance>();
}