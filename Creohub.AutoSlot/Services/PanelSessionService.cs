using Creohub.Domain.Entities;
using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Creohub.AutoSlot.Services;

// Creohub.AutoSlot/Services/PanelSessionService.cs
public class PanelSessionService(AppDbContext db)
{
    public async Task<PanelSession> CreateAsync()
    {
        var session = new PanelSession();
        db.PanelSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task<PanelSession?> GetByTokenAsync(string token) =>
        await db.PanelSessions.FirstOrDefaultAsync(s => s.Token == token);

    public async Task<bool> LinkAsync(string token, Guid userId)
    {
        var session = await GetByTokenAsync(token);
        if (session == null || session.IsExpired() || session.IsLinked) return false;
        session.Link(userId);
        await db.SaveChangesAsync();
        return true;
    }

    // Чистка старых сессий — вызывать периодически
    public async Task CleanupExpiredAsync()
    {
        var expired = db.PanelSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow && !s.IsLinked);
        db.PanelSessions.RemoveRange(expired);
        await db.SaveChangesAsync();
    }
}