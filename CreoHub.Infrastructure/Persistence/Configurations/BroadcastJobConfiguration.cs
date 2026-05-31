using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class BroadcastJobConfiguration : IEntityTypeConfiguration<BroadcastJob>
{
    public void Configure(EntityTypeBuilder<BroadcastJob> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message)
            .HasMaxLength(4096)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ErrorLog)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
