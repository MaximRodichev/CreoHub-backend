using Creohub.Domain.Entities;
using CreoHub.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class SubscriptionPromoCodeRepository(AppDbContext db) : ISubscriptionPromoCodeRepository
{
    public async Task<bool> WasMilestoneIssuedAsync(Guid userId, string milestoneTag) =>
        await db.SubscriptionPromoCodes
            .AnyAsync(p => p.IssuedToUserId == userId && p.MilestoneTag == milestoneTag);

    public async Task AddAsync(SubscriptionPromoCode code)
    {
        db.SubscriptionPromoCodes.Add(code);
        await Task.CompletedTask;
    }
}
