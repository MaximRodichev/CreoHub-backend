using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class OrderConfiguration :  IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(x=> x.CustomerId).IsRequired();
        builder.Property(x=>x.OrderDate).IsRequired();
        builder.Property(x=>x.Status).IsRequired().HasConversion<string>();
        builder.Property(x=>x.Description).IsRequired().HasMaxLength(500);
        
        builder.Property(x => x.OrderDate)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId);
    }
}