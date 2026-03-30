using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class UserBalanceConfiguration : IEntityTypeConfiguration<UserBalance>
{
    public void Configure(EntityTypeBuilder<UserBalance> builder)
    {
        builder.HasKey(b => b.Id);

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

        builder.HasIndex(b => b.UserId).IsUnique();
    }
}
