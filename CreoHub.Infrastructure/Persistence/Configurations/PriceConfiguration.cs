using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.Date });

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.Date).IsRequired();

        builder.Property(x => x.Value).HasPrecision(18, 2).IsRequired();
        
        builder.Property(x => x.Date)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Price_Value",
                "\"Value\" >= 0 AND \"Value\" <= 1000"
            ));

        
        builder.HasOne(x => x.Product).WithMany(x => x.Prices).HasForeignKey(x => x.ProductId);
    }
}