using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ProductBundleConfiguration :  IEntityTypeConfiguration<ProductBundle>
{
    public void Configure(EntityTypeBuilder<ProductBundle> builder)
    {
        builder.HasKey(x => new {x.ProductId, x.BundleId});
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.BundleId).IsRequired();
        builder.HasOne(x => x.Bundle).WithMany(x => x.BundleItems).HasForeignKey(x => x.BundleId);
    }
}