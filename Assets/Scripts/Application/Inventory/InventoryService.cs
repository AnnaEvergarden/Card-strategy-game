using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IInventoryRepository repository, IEventBus eventBus, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _eventBus = eventBus;
        _unitOfWork = unitOfWork;
    }

    public async UniTask<IReadOnlyList<InventoryItem>> GetItemsAsync()
    {
        var inventory = await _repository.LoadAsync();
        return inventory.Items;
    }

    public async UniTask<bool> AddItemAsync(string itemId, int quantity)
    {
        var inventory = await _repository.LoadAsync();
        inventory.AddItem(itemId, quantity);
        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new InventoryChangedEvent(inventory.Items, itemId, quantity));
        return true;
    }

    public async UniTask<bool> RemoveItemAsync(string itemId, int quantity)
    {
        var inventory = await _repository.LoadAsync();
        if (!inventory.RemoveItem(itemId, quantity))
            return false;
        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new InventoryChangedEvent(inventory.Items, itemId, -quantity));
        return true;
    }

    public async UniTask<bool> HasItemAsync(string itemId, int quantity = 1)
    {
        var inventory = await _repository.LoadAsync();
        return inventory.HasItem(itemId, quantity);
    }

    public async UniTask<int> GetQuantityAsync(string itemId)
    {
        var inventory = await _repository.LoadAsync();
        return inventory.GetQuantity(itemId);
    }

    private async UniTask SaveWithTransactionAsync()
    {
        bool ownsTransaction = !_unitOfWork.IsInProgress;
        if (ownsTransaction) await _unitOfWork.BeginAsync();
        _unitOfWork.Register(() => _repository.SaveAsync(), () => _repository.InvalidateCache());
        if (ownsTransaction) await _unitOfWork.CommitAsync();
    }
}