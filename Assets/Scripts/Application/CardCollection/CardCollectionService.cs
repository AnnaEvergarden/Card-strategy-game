using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public sealed class CardCollectionService : ICardCollectionService
{
    private readonly ICardCollectionRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly IUnitOfWork _unitOfWork;

    public CardCollectionService(ICardCollectionRepository repository, IEventBus eventBus, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _eventBus = eventBus;
        _unitOfWork = unitOfWork;
    }

    public async UniTask<IReadOnlyList<CardInstance>> GetCardsAsync()
    {
        var collection = await _repository.LoadAsync();
        return collection.Cards;
    }

    public async UniTask AddCardAsync(string instanceId, string cardId)
    {
        var collection = await _repository.LoadAsync();
        collection.Add(instanceId, cardId);
        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new CardCollectionChangedEvent(
            collection.Cards, instanceId, CardCollectionChangedEvent.ChangeType.Added));
    }

    public async UniTask<bool> RemoveCardAsync(string instanceId)
    {
        var collection = await _repository.LoadAsync();
        if (!collection.Remove(instanceId))
            return false;
        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new CardCollectionChangedEvent(
            collection.Cards, instanceId, CardCollectionChangedEvent.ChangeType.Removed));
        return true;
    }

    public async UniTask<int> GetCountAsync()
    {
        var collection = await _repository.LoadAsync();
        return collection.Count;
    }

    public async UniTask<int> GetCountByCardIdAsync(string cardId)
    {
        var collection = await _repository.LoadAsync();
        return collection.GetCountByCardId(cardId);
    }

    private async UniTask SaveWithTransactionAsync()
    {
        bool ownsTransaction = !_unitOfWork.IsInProgress;
        if (ownsTransaction) await _unitOfWork.BeginAsync();
        _unitOfWork.Register(() => _repository.SaveAsync(), () => _repository.InvalidateCache());
        if (ownsTransaction) await _unitOfWork.CommitAsync();
    }
}