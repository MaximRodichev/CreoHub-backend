using Creohub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface ISubscriptionPromoCodeRepository
{
    Task<bool> WasMilestoneIssuedAsync(Guid userId, string milestoneTag);
    Task AddAsync(SubscriptionPromoCode code);
}
