using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ShopBalanceConfiguration : IEntityTypeConfiguration<ShopBalance>
{
    public void Configure(EntityTypeBuilder<ShopBalance> builder)
    {
        builder.HasKey(b => b.Id);

        // Optimistic concurrency через системную колонку xmin (PostgreSQL):
        // два параллельных withdraw/transfer не обновят баланс одновременно —
        // проигравший получит DbUpdateConcurrencyException. DDL не создаётся.
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(b => b.AvailableAmount)
            .HasPrecision(18, 2)
            .HasField("_availableAmount")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(b => b.PendingAmount)
            .HasPrecision(18, 2)
            .HasField("_pendingAmount")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.HasIndex(b => b.ShopId).IsUnique();
    }
}
