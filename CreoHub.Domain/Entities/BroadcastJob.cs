namespace CreoHub.Domain.Entities;

public static class BroadcastStatus
{
    public const string Pending  = "pending";
    public const string Running  = "running";
    public const string Done     = "done";
    public const string Failed   = "failed";
}

/// <summary>
/// Admin-created broadcast message to all (or filtered) users.
/// Processed by BroadcastBackgroundService at a rate-limited pace (~1-3s per message).
/// </summary>
public class BroadcastJob
{
    public int      Id           { get; private init; }
    public string   Message      { get; private set; }  = null!;
    public string   Status       { get; private set; }  = BroadcastStatus.Pending;
    public Guid     CreatedBy    { get; private init; }
    public DateTime CreatedAt    { get; private init; } = DateTime.UtcNow;
    public DateTime? StartedAt   { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int      TotalUsers   { get; private set; }
    public int      SentCount    { get; private set; }
    public int      FailedCount  { get; private set; }
    public string?  ErrorLog     { get; private set; }

    private BroadcastJob() {}

    public static BroadcastJob Create(string message, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        return new BroadcastJob { Message = message, CreatedBy = createdBy };
    }

    public void Start(int totalUsers)
    {
        Status    = BroadcastStatus.Running;
        StartedAt = DateTime.UtcNow;
        TotalUsers = totalUsers;
    }

    public void RecordSent()    => SentCount++;
    public void RecordFailed()  => FailedCount++;

    public void Complete()
    {
        Status      = BroadcastStatus.Done;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string error)
    {
        Status      = BroadcastStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorLog    = error;
    }
}
