using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ContentFileReplacementRepository : IContentFileReplacementRepository
{
    private readonly AppDbContext _db;

    public ContentFileReplacementRepository(AppDbContext db) => _db = db;

    public async Task<ContentFileReplacement> AddAsync(ContentFileReplacement entity, CancellationToken ct = default)
        => (await _db.ContentFileReplacements.AddAsync(entity, ct)).Entity;

    public async Task<ContentFileReplacement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ContentFileReplacements.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> HasPendingAsync(Guid contentFileId, CancellationToken ct = default)
        => await _db.ContentFileReplacements
            .AnyAsync(x => x.ContentFileId == contentFileId && x.Status == ReplacementStatus.Pending, ct);

    public async Task<List<ContentFileReplacement>> GetPendingAsync(CancellationToken ct = default)
        => await _db.ContentFileReplacements
            .Where(x => x.Status == ReplacementStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
}
