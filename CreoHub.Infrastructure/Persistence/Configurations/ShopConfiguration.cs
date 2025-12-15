using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x=>x.Name).HasMaxLength(50).IsRequired();
        
        builder.HasIndex(x => x.Description).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.HasOne(x => x.Owner).WithOne(x => x.Shop).HasForeignKey<Shop>(x => x.OwnerId);
        builder.HasMany(x => x.Products).WithOne(x => x.Owner).HasForeignKey(x => x.OwnerId);
    }
}