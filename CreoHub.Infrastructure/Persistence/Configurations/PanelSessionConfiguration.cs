using Creohub.Domain.Entities;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class PanelSessionConfiguration : IEntityTypeConfiguration<PanelSession>
{
    public void Configure(EntityTypeBuilder<PanelSession> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.Token)
            .IsUnique();

        // UserId без FK — сессия временная, не держим жёсткую связь
        builder.Property(x => x.UserId)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            )
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            )
            .IsRequired();
    }
}
