using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class PaymentWebhookLogConfiguration : IEntityTypeConfiguration<PaymentWebhookLog>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TrackId).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(50);

        // Сырой JSON вебхука — jsonb для возможности запросов по полям из pgAdmin
        builder.Property(x => x.RawJson).HasColumnType("jsonb");

        builder.Property(x => x.ReceivedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Поиск истории вебхуков по конкретному платежу
        builder.HasIndex(x => x.TrackId);

        builder.ToTable("PaymentWebhookLogs");
    }
}
