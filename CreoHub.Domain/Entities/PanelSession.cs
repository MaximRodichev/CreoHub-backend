namespace Creohub.Domain.Entities;

public class PanelSession
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Token { get; private init; } = Guid.NewGuid().ToString("N")[..16];
    public Guid? UserId { get; private set; }
    public bool IsLinked { get; private set; } = false;
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private init; } = DateTime.UtcNow.AddMinutes(5);

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public void Link(Guid userId)
    {
        UserId = userId;
        IsLinked = true;
    }
}