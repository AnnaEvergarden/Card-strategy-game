using System;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;

public sealed class AccountService : IAccountService
{
    private const string PasswordSalt = "CardGame_Auth_Salt_v2:";

    private readonly IAccountRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IAccountRepository repository, IEventBus eventBus, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _eventBus = eventBus;
        _unitOfWork = unitOfWork;
    }

    public async UniTask<bool> LoginAsync(string username, string password)
    {
        var account = await _repository.LoadAsync();
        var entry = account.FindUser(username);
        if (entry == null) return false;

        string hash = HashPassword(password);
        if (!string.Equals(entry.PasswordHash, hash, StringComparison.Ordinal))
            return false;

        account.SetCurrentUser(username);
        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new AccountChangedEvent(account, AccountChangedEvent.ChangeType.LoggedIn));
        return true;
    }

    public async UniTask<bool> RegisterAsync(string username, string password)
    {
        var account = await _repository.LoadAsync();
        string hash = HashPassword(password);
        if (!account.AddUser(username, hash))
            return false;

        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new AccountChangedEvent(account, AccountChangedEvent.ChangeType.Registered));
        return true;
    }

    public async UniTask<(string user, string pass)> LoadRememberedAsync()
    {
        var account = await _repository.LoadAsync();
        if (account.RememberEnabled && !string.IsNullOrWhiteSpace(account.RememberUser))
            return (account.RememberUser, account.RememberPass ?? string.Empty);
        return (string.Empty, string.Empty);
    }

    public async UniTask SaveRememberedAsync(string username, string password, bool enabled)
    {
        var account = await _repository.LoadAsync();
        account.SetRemembered(username, password, enabled);
        _repository.MarkDirty();
        await SaveWithTransactionAsync();
        _eventBus.Publish(new AccountChangedEvent(account, AccountChangedEvent.ChangeType.Remembered));
    }

    private async UniTask SaveWithTransactionAsync()
    {
        bool ownsTransaction = !_unitOfWork.IsInProgress;
        if (ownsTransaction) await _unitOfWork.BeginAsync();
        _unitOfWork.Register(() => _repository.SaveAsync(), () => _repository.InvalidateCache());
        if (ownsTransaction) await _unitOfWork.CommitAsync();
    }

    private static string HashPassword(string password)
    {
        string raw = PasswordSalt + (password ?? string.Empty);
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }
}