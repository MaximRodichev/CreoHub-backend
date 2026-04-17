using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(x=>x.Id);
        builder.HasIndex(x => x.CartId);
        builder.HasKey(x => new { x.CartId, x.ProductId });
        builder.HasOne(x=>x.Cart).WithMany(x=>x.Items).HasForeignKey(x => x.CartId);
    }
}