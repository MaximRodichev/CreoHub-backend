using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ContentFileReplacementConfiguration : IEntityTypeConfiguration<ContentFileReplacement>
{
    public void Configure(EntityTypeBuilder<ContentFileReplacement> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RejectReason).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // ContentFileId / NewStorageObjectId — мягкие ссылки БЕЗ FK:
        // staging-файл удаляется на аппруве/реджекте, и каскад не должен сносить аудит-строку.
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ContentFileId);

        builder.ToTable("ContentFileReplacements");
    }
}
