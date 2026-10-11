/// <summary>
/// CardCollection 领域对象 ↔ SaveData DTO 映射器。纯函数，无状态。
/// </summary>
public static class CardCollectionMapper
{
    /// <summary>SaveData → Domain</summary>
    public static CardCollection ToDomain(CardCollectionSaveData dto)
    {
        var collection = new CardCollection();
        if (dto?.Cards == null) return collection;

        for (int i = 0; i < dto.Cards.Count; i++)
        {
            var card = dto.Cards[i];
            if (card != null && !string.IsNullOrWhiteSpace(card.InstanceId) && !string.IsNullOrWhiteSpace(card.CardId))
                collection.Add(card.InstanceId, card.CardId, card.Level, card.Experience);
        }

        return collection;
    }

    /// <summary>Domain → SaveData</summary>
    public static CardCollectionSaveData ToSaveData(CardCollection domain)
    {
        var dto = new CardCollectionSaveData();
        var cards = domain.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            dto.Cards.Add(new CardInstance
            {
                InstanceId = card.InstanceId,
                CardId = card.CardId,
                Level = card.Level,
                Experience = card.Experience
            });
        }
        return dto;
    }
}