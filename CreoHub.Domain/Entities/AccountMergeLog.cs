namespace CreoHub.Domain.Entities;

/// <summary>
/// Аудит объединения аккаунтов: кто (админ) кого с кем смерджил и что переехало.
/// Append-only, пишется в той же транзакции, что и сам мердж.
/// Идентичность удалённого аккаунта дублируется в лог (сам аккаунт после мерджа удалён).
/// </summary>
public class AccountMergeLog
{
    public Guid     Id                     { get; private set; } = Guid.NewGuid();
    public Guid     KeepUserId             { get; private set; }   // остался
    public Guid     MergedUserId           { get; private set; }   // удалён
    public string?  MergedName             { get; private set; }
    public string?  MergedEmail            { get; private set; }
    public long?    MergedTelegramId       { get; private set; }
    public string?  MergedTelegramUsername { get; private set; }
    public Guid     AdminId                { get; private set; }
    public int      MovedContentAccess     { get; private set; }
    public int      MovedOrders            { get; private set; }
    public int      MovedTransactions      { get; private set; }
    public int      MovedSubscriptions     { get; private set; }
    public decimal  AddedBalance           { get; private set; }
    public decimal  AddedSpent             { get; private set; }
    public DateTime CreatedAt              { get; private set; } = DateTime.UtcNow;

    private AccountMergeLog() { }

    public AccountMergeLog(
        Guid keepUserId, Guid mergedUserId,
        string? mergedName, string? mergedEmail, long? mergedTelegramId, string? mergedTelegramUsername,
        Guid adminId,
        int movedContentAccess, int movedOrders, int movedTransactions, int movedSubscriptions,
        decimal addedBalance, decimal addedSpent)
    {
        KeepUserId             = keepUserId;
        MergedUserId           = mergedUserId;
        MergedName             = mergedName;
        MergedEmail            = mergedEmail;
        MergedTelegramId       = mergedTelegramId;
        MergedTelegramUsername = mergedTelegramUsername;
        AdminId                = adminId;
        MovedContentAccess     = movedContentAccess;
        MovedOrders            = movedOrders;
        MovedTransactions      = movedTransactions;
        MovedSubscriptions     = movedSubscriptions;
        AddedBalance           = addedBalance;
        AddedSpent             = addedSpent;
    }
}
