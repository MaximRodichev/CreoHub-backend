using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(x => x.Id);

        // Явно указываем связь
        builder.HasOne(x => x.Product)
            .WithMany(x => x.OrderItems) // Убедись, что в Product есть List<OrderItem> OrderItems
            .HasForeignKey(x => x.ProductId) // Указываем конкретное поле
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.PriceAtPurchase)
            .HasPrecision(18, 2);
    }
}