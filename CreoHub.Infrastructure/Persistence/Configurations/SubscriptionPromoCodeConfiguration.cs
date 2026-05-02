using Creohub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class SubscriptionPromoCodeConfiguration : IEntityTypeConfiguration<SubscriptionPromoCode>
{
    public void Configure(EntityTypeBuilder<SubscriptionPromoCode> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.ProductType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Days)
            .IsRequired();

        builder.Property(x => x.IsLifetime)
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .IsRequired();

        builder.Property(x => x.UsedByUserId)
            .IsRequired(false);

        builder.Property(x => x.IssuedToUserId)
            .IsRequired(false);

        builder.Property(x => x.MilestoneTag)
            .HasMaxLength(64)
            .IsRequired(false);

        // Индекс для быстрой проверки дедупликации milestone
        builder.HasIndex(x => new { x.IssuedToUserId, x.MilestoneTag });

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            )
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null
            );

        builder.Property(x => x.UsedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null
            );
    }
}
