using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ProductConfiguration :  IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x=>x.Name).IsUnique();
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(2000);
        
        builder.Property(x => x.ProductType)
            .HasConversion<string>();
        
        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Product_ProductType",
                "\"ProductType\" IN ('Single','Bundle')"
            ));
        
        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
        
        builder.HasMany(x=>x.Prices).WithOne(x=>x.Product).HasForeignKey(x=>x.ProductId);
        builder.HasOne(x => x.Owner).WithMany(x => x.Products).HasForeignKey(x => x.OwnerId);
        builder.HasMany(x => x.Orders).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
    }
}