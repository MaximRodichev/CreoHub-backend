using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ProductStatusLogConfiguration : IEntityTypeConfiguration<ProductStatusLog>
{
    public void Configure(EntityTypeBuilder<ProductStatusLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.OldStatus).HasConversion<string>().IsRequired();
        builder.Property(x => x.NewStatus).HasConversion<string>().IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.ChangedById);
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
