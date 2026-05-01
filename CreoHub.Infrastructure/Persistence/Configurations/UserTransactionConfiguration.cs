using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class UserTransactionConfiguration : IEntityTypeConfiguration<UserTransaction>
{
    public void Configure(EntityTypeBuilder<UserTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.TrackId);

        builder.Property(x => x.TrackId)
            .IsRequired();

        builder.Property(x => x.FullAmount)
            .HasPrecision(18, 2)
            .HasField("_amount")
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
