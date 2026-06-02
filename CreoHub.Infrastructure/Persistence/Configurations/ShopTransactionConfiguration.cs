using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ShopTransactionConfiguration : IEntityTypeConfiguration<ShopTransaction>
{
    public void Configure(EntityTypeBuilder<ShopTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        // Optimistic concurrency через системную колонку xmin (см. UserTransactionConfiguration).
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(t => t.ShopId);
        // TrackId магазинной продажи уникален ("sale-{orderId}-{shopId}") — unique-индекс
        // ловит повторную вставку при дублирующем webhook'е.
        builder.HasIndex(t => t.TrackId).IsUnique();

        builder.Property(x => x.TrackId)
            .IsRequired();

        builder.Property(x => x.FullAmount)
            .HasPrecision(18, 2)
            .HasField("_amount")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(x => x.PlatformFeeAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.NetAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PlatformFeePercent)
            .HasPrecision(5, 2)
            .HasField("_platformFeePercent")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(x => x.TransactionType)
            .HasConversion<string>();

        builder.Property(x => x.TransactionStatus)
            .HasConversion<string>()
            .HasField("_transactionStatus")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
    }
}
