using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x=>x.User).WithOne(x=>x.Cart).HasForeignKey<Cart>(x => x.UserId);
    }
}