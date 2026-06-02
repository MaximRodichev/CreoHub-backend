using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class UserTransactionConfiguration : IEntityTypeConfiguration<UserTransaction>
{
    public void Configure(EntityTypeBuilder<UserTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        // Optimistic concurrency через системную колонку xmin (PostgreSQL).
        // Эквивалент UseXminAsConcurrencyToken(): два параллельных UPDATE одной строки →
        // у проигравшего DbUpdateConcurrencyException. DDL не создаётся (xmin системная).
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(t => t.UserId);
        // TrackId уникален по схеме (OxaPay invoice / "balance-{guid}") — unique-индекс
        // дополнительно защищает от дублей и гонок webhook'ов.
        builder.HasIndex(t => t.TrackId).IsUnique();

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
