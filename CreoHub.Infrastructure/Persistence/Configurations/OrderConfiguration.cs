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
        builder.Property(x => x.Subtotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PersonalDiscountPercent).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.CartDiscountPercent).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();

        builder.Property(x=> x.CustomerId).IsRequired();
        builder.Property(x=>x.OrderDate).IsRequired();
        builder.Property(x=>x.Status).IsRequired().HasConversion<string>();
        builder.Property(x=>x.Description).IsRequired().HasMaxLength(500);
        
        builder.Property(x => x.OrderDate)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        // UserTransaction (purchase) — Order хранит ссылку на оплату
        // builder.HasOne(x => x.Transaction)
        //     .WithOne()
        //     .HasForeignKey<Order>(x => x.TransactionId)
        //     .IsRequired(false);
        // builder.HasIndex(x => x.TransactionId)
        //     .IsUnique()
        //     .HasFilter("\"TransactionId\" IS NOT NULL");
        
        builder.HasOne(o => o.Transaction)
            .WithOne(t => t.Order)
            .HasForeignKey<UserTransaction>(t => t.OrderId) // FK переехал в транзакцию
            .IsRequired(false); // Заказ не обязан иметь транзакцию (пока не оплачен)

        // ShopTransaction тоже ссылается на Order (через навигацию Order в BaseTransaction)
        builder.HasMany<ShopTransaction>()
            .WithOne(x => x.Order)
            .HasForeignKey("OrderId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId);
    }
}