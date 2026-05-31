using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class UserEventConfiguration : IEntityTypeConfiguration<UserEvent>
{
    public void Configure(EntityTypeBuilder<UserEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.SessionId)
            .HasMaxLength(64);

        builder.Property(x => x.Payload)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // ── Indexes ───────────────────────────────────────────────────────────
        // Funnel queries: by product + time
        builder.HasIndex(x => new { x.ProductId, x.CreatedAt })
            .HasFilter("\"ProductId\" IS NOT NULL");

        // Admin / shop time-range scans
        builder.HasIndex(x => new { x.EventType, x.CreatedAt });

        // General time-range scans
        builder.HasIndex(x => x.CreatedAt);
    }
}
