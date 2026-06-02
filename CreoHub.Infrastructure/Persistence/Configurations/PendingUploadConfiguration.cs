using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class PendingUploadConfiguration : IEntityTypeConfiguration<PendingUpload>
{
    public void Configure(EntityTypeBuilder<PendingUpload> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FileName)
            .HasMaxLength(300);

        builder.Property(x => x.MimeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ExpiresAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Поиск по key + shopId при consume
        builder.HasIndex(x => new { x.Key, x.ShopId }).IsUnique();

        // Очистка просроченных — индекс по времени
        builder.HasIndex(x => x.ExpiresAt);

        builder.ToTable("PendingUploads");
    }
}
