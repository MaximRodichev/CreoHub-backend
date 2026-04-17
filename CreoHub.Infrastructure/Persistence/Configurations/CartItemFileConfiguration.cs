using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class CartItemFileConfiguration : IEntityTypeConfiguration<CartItemFile>
{
    public void Configure(EntityTypeBuilder<CartItemFile> builder)
    {
        builder.HasKey(x => new { x.CartItemId, x.ContentFileId });
        
    }
}