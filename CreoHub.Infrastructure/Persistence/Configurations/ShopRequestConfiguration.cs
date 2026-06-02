using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ShopRequestConfiguration : IEntityTypeConfiguration<ShopRequest>
{
    public void Configure(EntityTypeBuilder<ShopRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BuyerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.BuyerEmail)
            .HasMaxLength(255);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.SellerReply)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(x => x.RepliedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        // FK на магазин и покупателя (без навигаций), каскад как в спеке
        builder.HasOne<Shop>()
            .WithMany()
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.BuyerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Список продавца с фильтром по статусу + бейдж
        builder.HasIndex(x => new { x.ShopId, x.Status });
        // Список покупателя + анти-спам (open request, дневной лимит)
        builder.HasIndex(x => x.BuyerUserId);

        builder.ToTable("ShopRequests");
    }
}
