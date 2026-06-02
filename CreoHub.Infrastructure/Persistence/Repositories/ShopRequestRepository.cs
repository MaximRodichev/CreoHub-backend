using CreoHub.Application.DTO.ShopRequestDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ShopRequestRepository : IShopRequestRepository
{
    private readonly AppDbContext _db;

    public ShopRequestRepository(AppDbContext db) => _db = db;

    public async Task<ShopRequest> AddAsync(ShopRequest entity, CancellationToken ct = default)
    {
        await _db.ShopRequests.AddAsync(entity, ct);
        return entity;
    }

    public Task<ShopRequest?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.ShopRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<ShopRequestDTO>> GetByShopIdAsync(
        Guid shopId, ShopRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ShopRequests.AsNoTracking().Where(r => r.ShopId == shopId);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        // Status enum хранится строкой (HasConversion) — материализуем, .ToString() уже в памяти
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { r.Id, r.BuyerName, r.Message, r.Status, r.SellerReply, r.CreatedAt, r.RepliedAt })
            .ToListAsync(ct);

        return rows.Select(r => new ShopRequestDTO
        {
            Id          = r.Id,
            BuyerName   = r.BuyerName,
            Message     = r.Message,
            Status      = r.Status.ToString(),
            SellerReply = r.SellerReply,
            CreatedAt   = r.CreatedAt,
            RepliedAt   = r.RepliedAt,
        }).ToList();
    }

    public async Task<List<MyShopRequestDTO>> GetByBuyerIdAsync(
        Guid buyerUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var rows = await (
            from r in _db.ShopRequests.AsNoTracking()
            join s in _db.Shops on r.ShopId equals s.Id
            where r.BuyerUserId == buyerUserId
            orderby r.CreatedAt descending
            select new { r.Id, r.ShopId, ShopName = s.Name, r.Message, r.Status, r.SellerReply, r.CreatedAt, r.RepliedAt })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return rows.Select(r => new MyShopRequestDTO
        {
            Id          = r.Id,
            ShopId      = r.ShopId,
            ShopName    = r.ShopName,
            Message     = r.Message,
            Status      = r.Status.ToString(),
            SellerReply = r.SellerReply,
            CreatedAt   = r.CreatedAt,
            RepliedAt   = r.RepliedAt,
        }).ToList();
    }

    public Task<int> CountNewByShopIdAsync(Guid shopId, CancellationToken ct = default) =>
        _db.ShopRequests.CountAsync(r => r.ShopId == shopId && r.Status == ShopRequestStatus.New, ct);

    public Task<bool> HasOpenRequestAsync(Guid buyerUserId, Guid shopId, CancellationToken ct = default) =>
        _db.ShopRequests.AnyAsync(
            r => r.BuyerUserId == buyerUserId && r.ShopId == shopId && r.Status == ShopRequestStatus.New, ct);

    public Task<int> CountByBuyerSinceAsync(Guid buyerUserId, DateTime since, CancellationToken ct = default) =>
        _db.ShopRequests.CountAsync(r => r.BuyerUserId == buyerUserId && r.CreatedAt >= since, ct);
}
