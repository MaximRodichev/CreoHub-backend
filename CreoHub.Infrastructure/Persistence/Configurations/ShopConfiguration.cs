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
        
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
        
        builder.HasOne(x => x.Owner).WithOne(x => x.Shop).HasForeignKey<Shop>(x => x.OwnerId);
        builder.HasMany(x => x.Products).WithOne(x => x.Owner).HasForeignKey(x => x.OwnerId);

        builder.HasOne(x => x.Banner)
            .WithMany()
            .HasForeignKey(x => x.BannerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Logo)
            .WithMany()
            .HasForeignKey(x => x.LogoId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(x => x.Balance)
            .WithOne()
            .HasForeignKey<Shop>(x => x.BalanceId);

        builder.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(t => t.ShopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}