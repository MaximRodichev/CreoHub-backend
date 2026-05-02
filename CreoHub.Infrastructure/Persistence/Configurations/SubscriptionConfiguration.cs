using Creohub.Domain.Entities;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(x => x.Id);

        // Основной индекс для проверки активной подписки IsActiveAsync()
        builder.HasIndex(x => new { x.UserId, x.ProductType });

        // Для сортировки истории и поиска максимального ExpiresAt
        builder.HasIndex(x => new { x.UserId, x.ProductType, x.ExpiresAt });

        builder.Property(x => x.ProductType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.PlanType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Days)
            .IsRequired();

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

        // FK к User — без каскадного удаления
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK к UserTransaction — опциональный
        builder.HasOne<UserTransaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // FK к SubscriptionPromoCode — опциональный
        builder.HasOne<SubscriptionPromoCode>()
            .WithMany()
            .HasForeignKey(x => x.PromoCodeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
